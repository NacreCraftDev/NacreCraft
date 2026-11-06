using NacreCraft.Game.World.Entity;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Effect;

//BadOmenMobEffect bad omen effect, maps to vanilla BadOmenMobEffect
//Vanilla converts the effect into raid omen when the player enters a village. The raid manager, village test and the
//player raid omen position are not ported yet (see RaidCommand), so the tick keeps the effect and the conversion is
//deferred until the raid subsystem lands; this matches the vanilla branch taken outside a village
public sealed class BadOmenMobEffect : MobEffect
{
    public BadOmenMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    public override bool ShouldApplyEffectTickThisTick(int remainingDuration, int amplification) => true;

    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification) => true;
}

//RaidOmenMobEffect raid omen effect, maps to vanilla RaidOmenMobEffect
//Vanilla starts or extends a raid from the stored raid omen position on the last tick. The raid manager is not ported,
//so the last tick keeps the effect, which is the vanilla branch taken when the player has no raid omen position
public sealed class RaidOmenMobEffect : MobEffect
{
    public RaidOmenMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    //Only the final tick matters, maps to vanilla shouldApplyEffectTickThisTick
    public override bool ShouldApplyEffectTickThisTick(int remainingDuration, int amplification)
        => remainingDuration == 1;

    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification) => true;
}
