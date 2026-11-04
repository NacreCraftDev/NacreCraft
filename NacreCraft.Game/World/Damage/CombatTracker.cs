using NacreCraft.Game.World.Entity;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.World.Damage;

//CombatTracker records the hits a living entity took and decides when combat starts and ends
//maps to vanilla net.minecraft.world.damagesource.CombatTracker
public sealed class CombatTracker
{
    public const int ResetDamageStatusTime = 100;

    public const int ResetCombatStatusTime = 300;

    private readonly List<CombatEntry> _entries = new();
    private readonly LivingEntity _mob;
    private int _lastDamageTime;
    private int _combatStartTime;
    private int _combatEndTime;
    private bool _inCombat;
    private bool _takingDamage;

    public CombatTracker(LivingEntity mob) => _mob = mob;

    //Entries the recorded hits, oldest first
    public IReadOnlyList<CombatEntry> Entries => _entries;

    //InCombat whether the entity is currently in combat
    public bool InCombat => _inCombat;

    //RecordDamage records one hit and enters combat when a living entity caused it, maps to vanilla recordDamage
    public void RecordDamage(DamageSource source, float damage)
    {
        RecheckStatus();
        var fallLocation = FallLocation.GetCurrentFallLocation(_mob);
        _entries.Add(new CombatEntry(source, damage, fallLocation, (float)_mob.FallDistance));
        _lastDamageTime = _mob.TickCount;
        _takingDamage = true;
        if (_inCombat || _mob.IsDeadOrDying || !ShouldEnterCombat(source)) return;
        _inCombat = true;
        _combatEndTime = _combatStartTime = _mob.TickCount;
    }

    //GetCombatDuration how long the current or last combat lasted in ticks
    public int GetCombatDuration()
        => _inCombat ? _mob.TickCount - _combatStartTime : _combatEndTime - _combatStartTime;

    //RecheckStatus ends the combat window once the reset time passed, maps to vanilla recheckStatus
    public void RecheckStatus()
    {
        var reset = _inCombat ? ResetCombatStatusTime : ResetDamageStatusTime;
        if (!_takingDamage || (!_mob.IsDeadOrDying && _mob.TickCount - _lastDamageTime <= reset)) return;
        _takingDamage = false;
        _inCombat = false;
        _combatEndTime = _mob.TickCount;
        _entries.Clear();
    }

    //GetDeathMessage the death message of the last blow
    //NC has no localized death message or display names yet, so only the message key is produced
    public Component GetDeathMessage()
    {
        if (_entries.Count == 0) return Component.Translatable("death.attack.generic");
        return Component.Translatable("death.attack." + _entries[^1].Source.GetMsgId());
    }

    //ShouldEnterCombat a living entity as the cause starts combat, maps to vanilla shouldEnterCombat
    private static bool ShouldEnterCombat(DamageSource source) => source.CausingEntity is LivingEntity;
}
