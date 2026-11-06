using NacreCraft.Game.World.Entity;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Effect;

//AbsorptionMobEffect absorption effect, maps to vanilla AbsorptionMobEffect
//The effect only keeps the absorption shield alive; the shield value itself comes from the MAX_ABSORPTION modifier
//and the amount granted by OnEffectStarted
public sealed class AbsorptionMobEffect : MobEffect
{
    public AbsorptionMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    //The effect drops as soon as the shield is used up, maps to vanilla applyEffectTick
    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification)
        => mob.AbsorptionAmount > 0.0f;

    public override bool ShouldApplyEffectTickThisTick(int tickCount, int amplification) => true;

    //Grants four shield points per level, never lowering an existing shield, maps to vanilla onEffectStarted
    public override void OnEffectStarted(LivingEntity mob, int amplifier)
    {
        base.OnEffectStarted(mob, amplifier);
        mob.SetAbsorptionAmount(Math.Max(mob.AbsorptionAmount, 4 * (1 + amplifier)));
    }
}

//RegenerationMobEffect regeneration effect, maps to vanilla RegenerationMobEffect
public sealed class RegenerationMobEffect : MobEffect
{
    //DamageInterval base ticks between heals, maps to vanilla 50
    private const int HealInterval = 50;

    public RegenerationMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification)
    {
        if (mob.Health < mob.MaxHealth) mob.Heal(1.0f);
        return true;
    }

    //Higher levels heal faster; an interval that shifts to zero heals every tick, maps to vanilla shouldApplyEffectTickThisTick
    public override bool ShouldApplyEffectTickThisTick(int tickCount, int amplification)
    {
        var interval = HealInterval >> amplification;
        return interval <= 0 || tickCount % interval == 0;
    }
}

//PoisonMobEffect poison effect, maps to vanilla PoisonMobEffect
//Poison never kills: it stops once health would drop to one
public sealed class PoisonMobEffect : MobEffect
{
    //DamageInterval base ticks between damage ticks, maps to vanilla DAMAGE_INTERVAL
    public const int DamageInterval = 25;

    public PoisonMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification)
    {
        if (mob.Health > 1.0f) mob.HurtServer(level, mob.DamageSources().Magic(), 1.0f);
        return true;
    }

    public override bool ShouldApplyEffectTickThisTick(int tickCount, int amplification)
    {
        var interval = DamageInterval >> amplification;
        return interval <= 0 || tickCount % interval == 0;
    }
}

//WitherMobEffect wither effect, maps to vanilla WitherMobEffect
//Unlike poison, wither can kill
public sealed class WitherMobEffect : MobEffect
{
    //DamageInterval base ticks between damage ticks, maps to vanilla DAMAGE_INTERVAL
    public const int DamageInterval = 40;

    public WitherMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification)
    {
        mob.HurtServer(level, mob.DamageSources().Wither(), 1.0f);
        return true;
    }

    public override bool ShouldApplyEffectTickThisTick(int tickCount, int amplification)
    {
        var interval = DamageInterval >> amplification;
        return interval <= 0 || tickCount % interval == 0;
    }
}

//HungerMobEffect hunger effect, maps to vanilla HungerMobEffect
public sealed class HungerMobEffect : MobEffect
{
    public HungerMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification)
    {
        if (mob is Player player) player.CauseFoodExhaustion(0.005f * (amplification + 1));
        return true;
    }

    public override bool ShouldApplyEffectTickThisTick(int tickCount, int amplification) => true;
}

//SaturationMobEffect saturation effect, maps to vanilla SaturationMobEffect
public sealed class SaturationMobEffect : InstantaneousMobEffect
{
    public SaturationMobEffect(MobEffectCategory category, int color) : base(category, color) { }

    public override bool ApplyEffectTick(ServerLevel level, LivingEntity mob, int amplification)
    {
        if (mob is Player player) player.Eat(amplification + 1, 1.0f);
        return true;
    }
}
