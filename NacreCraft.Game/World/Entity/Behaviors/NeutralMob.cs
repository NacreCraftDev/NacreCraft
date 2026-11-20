using NacreCraft.Game.World.Level;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//NeutralMob a mob that becomes angry at a player for a limited time, maps to vanilla net.minecraft.world.entity.NeutralMob
//The anger end time lives on the implementor; the default helpers read the game rules and the target reference like vanilla
public interface NeutralMob
{
    const string TagAngerEndTime = "anger_end_time";
    const string TagAngryAt = "angry_at";
    const long NoAngerEndTime = -1;

    long GetPersistentAngerEndTime();

    void SetPersistentAngerEndTime(long endTime);

    EntityReference<LivingEntity>? GetPersistentAngerTarget();

    void SetPersistentAngerTarget(EntityReference<LivingEntity>? persistentAngerTarget);

    void StartPersistentAngerTimer();

    ServerLevel Level { get; }

    LivingEntity? GetLastHurtByMob();

    void SetLastHurtByMob(LivingEntity? hurtBy);

    void SetTarget(LivingEntity? target);

    bool CanAttack(LivingEntity target);

    LivingEntity? GetTarget();

    LivingEntity? GetTargetUnchecked();

    void SetTimeToRemainAngry(long remainingTime)
        => SetPersistentAngerEndTime(Level.GameTime + remainingTime);

    //UpdatePersistentAnger refreshes the anger timer and clears anger at dead, creative or peaceful targets
    //Maps to vanilla updatePersistentAnger; the difficulty check uses the level's gamerule-aware data when available
    void UpdatePersistentAnger(ServerLevel level, bool stayAngryIfTargetPresent)
    {
        var previousTarget = GetTargetUnchecked();
        var persistentAngerTarget = GetPersistentAngerTarget();
        if (previousTarget is not null && previousTarget.IsDeadOrDying && persistentAngerTarget is not null
            && persistentAngerTarget.Matches(previousTarget) && previousTarget is Mob)
        {
            StopBeingAngry();
            return;
        }
        var target = GetTarget();
        if (target is not null)
        {
            var newTarget = persistentAngerTarget is null || !persistentAngerTarget.Matches(target);
            if (newTarget) SetPersistentAngerTarget(EntityReference<LivingEntity>.Of(target));
            if (newTarget || stayAngryIfTargetPresent) StartPersistentAngerTimer();
        }
        if (persistentAngerTarget is not null && !IsAngry()
            && (target is null || !IsValidPlayerTarget(target) || !stayAngryIfTargetPresent))
        {
            StopBeingAngry();
        }
        var persistentTarget = persistentAngerTarget?.GetEntity(level);
        if (persistentTarget is Player player && (IsCreativeOrSpectator(player) || IsPeaceful(level)))
            StopBeingAngry();
    }

    //IsAngryAt whether the anger applies to the entity, maps to vanilla isAngryAt
    bool IsAngryAt(LivingEntity entity, ServerLevel level)
    {
        if (!CanAttack(entity)) return false;
        if (IsValidPlayerTarget(entity) && IsAngryAtAllPlayers(level)) return true;
        var persistentAngerTarget = GetPersistentAngerTarget();
        return persistentAngerTarget is not null && persistentAngerTarget.Matches(entity);
    }

    bool IsAngryAtAllPlayers(ServerLevel level)
        => (level.GameRules?.GetBool(GameRules.UniversalAnger) ?? false) && IsAngry() && GetPersistentAngerTarget() is null;

    bool IsAngry()
    {
        var endTime = GetPersistentAngerEndTime();
        return endTime > 0 && endTime - Level.GameTime > 0;
    }

    void PlayerDied(ServerLevel level, Player player)
    {
        if (!(level.GameRules?.GetBool(GameRules.ForgiveDeadPlayers) ?? true)) return;
        var persistentAngerTarget = GetPersistentAngerTarget();
        if (persistentAngerTarget is not null && persistentAngerTarget.Matches(player)) StopBeingAngry();
    }

    void ForgetCurrentTargetAndRefreshUniversalAnger()
    {
        StopBeingAngry();
        StartPersistentAngerTimer();
    }

    void StopBeingAngry()
    {
        SetLastHurtByMob(null);
        SetPersistentAngerTarget(null);
        SetTarget(null);
        SetPersistentAngerEndTime(NoAngerEndTime);
    }

    static bool IsValidPlayerTarget(LivingEntity target)
        => target is Player player && !IsCreativeOrSpectator(player);

    static bool IsCreativeOrSpectator(Player player)
        => player.GameMode == GameType.Creative.Id || player.IsSpectator();

    static bool IsPeaceful(ServerLevel level)
        => false;
}
