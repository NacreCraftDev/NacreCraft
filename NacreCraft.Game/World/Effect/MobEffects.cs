using NacreCraft.Registry;
using NacreCraft.Registry.EntityAttribute;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Effect;

//MobEffects built-in mob effects, maps to vanilla net.minecraft.world.effect.MobEffects
//Registration order strictly follows the vanilla static field declaration order; the registry id must equal the client registry id or the client shows the wrong effect
//Field initializers run in declaration order; first access to any member completes all registration, before the registry freezes
public static class MobEffects
{
    private static bool _bootstrapped;

    public static readonly Holder<NacreCraft.Registry.MobEffect> SPEED = Register("speed", new MobEffect(MobEffectCategory.Beneficial, 3402751)
        .AddAttributeModifier(Attributes.MovementSpeed, Identifier.WithDefaultNamespace("effect.speed"), 0.20000000298023224, AttributeOperation.AddMultipliedTotal));
    public static readonly Holder<NacreCraft.Registry.MobEffect> SLOWNESS = Register("slowness", new MobEffect(MobEffectCategory.Harmful, 9154528)
        .AddAttributeModifier(Attributes.MovementSpeed, Identifier.WithDefaultNamespace("effect.slowness"), -0.15000000596046448, AttributeOperation.AddMultipliedTotal));
    public static readonly Holder<NacreCraft.Registry.MobEffect> HASTE = Register("haste", new MobEffect(MobEffectCategory.Beneficial, 14270531)
        .AddAttributeModifier(Attributes.AttackSpeed, Identifier.WithDefaultNamespace("effect.haste"), 0.10000000149011612, AttributeOperation.AddMultipliedTotal));
    public static readonly Holder<NacreCraft.Registry.MobEffect> MINING_FATIGUE = Register("mining_fatigue", new MobEffect(MobEffectCategory.Harmful, 4866583)
        .AddAttributeModifier(Attributes.AttackSpeed, Identifier.WithDefaultNamespace("effect.mining_fatigue"), -0.10000000149011612, AttributeOperation.AddMultipliedTotal));
    public static readonly Holder<NacreCraft.Registry.MobEffect> STRENGTH = Register("strength", new MobEffect(MobEffectCategory.Beneficial, 16762624)
        .AddAttributeModifier(Attributes.AttackDamage, Identifier.WithDefaultNamespace("effect.strength"), 3.0, AttributeOperation.AddValue));
    public static readonly Holder<NacreCraft.Registry.MobEffect> INSTANT_HEALTH = Register("instant_health", new HealOrHarmMobEffect(MobEffectCategory.Beneficial, 16262179, false));
    public static readonly Holder<NacreCraft.Registry.MobEffect> INSTANT_DAMAGE = Register("instant_damage", new HealOrHarmMobEffect(MobEffectCategory.Harmful, 11101546, true));
    public static readonly Holder<NacreCraft.Registry.MobEffect> JUMP_BOOST = Register("jump_boost", new MobEffect(MobEffectCategory.Beneficial, 16646020)
        .AddAttributeModifier(Attributes.SafeFallDistance, Identifier.WithDefaultNamespace("effect.jump_boost"), 1.0, AttributeOperation.AddValue));
    public static readonly Holder<NacreCraft.Registry.MobEffect> NAUSEA = Register("nausea", new MobEffect(MobEffectCategory.Harmful, 5578058).SetBlendDuration(150, 20, 60));
    public static readonly Holder<NacreCraft.Registry.MobEffect> REGENERATION = Register("regeneration", new RegenerationMobEffect(MobEffectCategory.Beneficial, 13458603));
    public static readonly Holder<NacreCraft.Registry.MobEffect> RESISTANCE = Register("resistance", new MobEffect(MobEffectCategory.Beneficial, 9520880));
    public static readonly Holder<NacreCraft.Registry.MobEffect> FIRE_RESISTANCE = Register("fire_resistance", new MobEffect(MobEffectCategory.Beneficial, 0xFF9900));
    public static readonly Holder<NacreCraft.Registry.MobEffect> WATER_BREATHING = Register("water_breathing", new MobEffect(MobEffectCategory.Beneficial, 10017472));
    public static readonly Holder<NacreCraft.Registry.MobEffect> INVISIBILITY = Register("invisibility", new MobEffect(MobEffectCategory.Beneficial, 0xF6F6F6)
        .AddAttributeModifier(Attributes.WaypointTransmitRange, Identifier.WithDefaultNamespace("effect.waypoint_transmit_range_hide"), -1.0, AttributeOperation.AddMultipliedTotal));
    public static readonly Holder<NacreCraft.Registry.MobEffect> BLINDNESS = Register("blindness", new MobEffect(MobEffectCategory.Harmful, 2039587));
    public static readonly Holder<NacreCraft.Registry.MobEffect> NIGHT_VISION = Register("night_vision", new MobEffect(MobEffectCategory.Beneficial, 12779366));
    public static readonly Holder<NacreCraft.Registry.MobEffect> HUNGER = Register("hunger", new HungerMobEffect(MobEffectCategory.Harmful, 5797459));
    public static readonly Holder<NacreCraft.Registry.MobEffect> WEAKNESS = Register("weakness", new MobEffect(MobEffectCategory.Harmful, 0x484D48)
        .AddAttributeModifier(Attributes.AttackDamage, Identifier.WithDefaultNamespace("effect.weakness"), -4.0, AttributeOperation.AddValue));
    public static readonly Holder<NacreCraft.Registry.MobEffect> POISON = Register("poison", new PoisonMobEffect(MobEffectCategory.Harmful, 8889187));
    public static readonly Holder<NacreCraft.Registry.MobEffect> WITHER = Register("wither", new WitherMobEffect(MobEffectCategory.Harmful, 7561558));
    public static readonly Holder<NacreCraft.Registry.MobEffect> HEALTH_BOOST = Register("health_boost", new MobEffect(MobEffectCategory.Beneficial, 16284963)
        .AddAttributeModifier(Attributes.MaxHealth, Identifier.WithDefaultNamespace("effect.health_boost"), 4.0, AttributeOperation.AddValue));
    public static readonly Holder<NacreCraft.Registry.MobEffect> ABSORPTION = Register("absorption", new AbsorptionMobEffect(MobEffectCategory.Beneficial, 0x2552A5)
        .AddAttributeModifier(Attributes.MaxAbsorption, Identifier.WithDefaultNamespace("effect.absorption"), 4.0, AttributeOperation.AddValue));
    public static readonly Holder<NacreCraft.Registry.MobEffect> SATURATION = Register("saturation", new SaturationMobEffect(MobEffectCategory.Beneficial, 16262179));
    public static readonly Holder<NacreCraft.Registry.MobEffect> GLOWING = Register("glowing", new MobEffect(MobEffectCategory.Neutral, 9740385));
    public static readonly Holder<NacreCraft.Registry.MobEffect> LEVITATION = Register("levitation", new MobEffect(MobEffectCategory.Harmful, 0xCEFFFF));
    public static readonly Holder<NacreCraft.Registry.MobEffect> LUCK = Register("luck", new MobEffect(MobEffectCategory.Beneficial, 5882118)
        .AddAttributeModifier(Attributes.Luck, Identifier.WithDefaultNamespace("effect.luck"), 1.0, AttributeOperation.AddValue));
    public static readonly Holder<NacreCraft.Registry.MobEffect> UNLUCK = Register("unluck", new MobEffect(MobEffectCategory.Harmful, 12624973)
        .AddAttributeModifier(Attributes.Luck, Identifier.WithDefaultNamespace("effect.unluck"), -1.0, AttributeOperation.AddValue));
    public static readonly Holder<NacreCraft.Registry.MobEffect> SLOW_FALLING = Register("slow_falling", new MobEffect(MobEffectCategory.Beneficial, 15978425));
    public static readonly Holder<NacreCraft.Registry.MobEffect> CONDUIT_POWER = Register("conduit_power", new MobEffect(MobEffectCategory.Beneficial, 1950417));
    public static readonly Holder<NacreCraft.Registry.MobEffect> DOLPHINS_GRACE = Register("dolphins_grace", new MobEffect(MobEffectCategory.Beneficial, 8954814));
    public static readonly Holder<NacreCraft.Registry.MobEffect> BAD_OMEN = Register("bad_omen", new BadOmenMobEffect(MobEffectCategory.Neutral, 745784)
        .WithSoundOnAdded(SoundEvents.ApplyEffectBadOmen));
    public static readonly Holder<NacreCraft.Registry.MobEffect> HERO_OF_THE_VILLAGE = Register("hero_of_the_village", new MobEffect(MobEffectCategory.Beneficial, 0x44FF44));
    public static readonly Holder<NacreCraft.Registry.MobEffect> DARKNESS = Register("darkness", new MobEffect(MobEffectCategory.Harmful, 2696993).SetBlendDuration(22));
    public static readonly Holder<NacreCraft.Registry.MobEffect> TRIAL_OMEN = Register("trial_omen", new MobEffect(MobEffectCategory.Neutral, 0x16A6A6)
        .WithSoundOnAdded(SoundEvents.ApplyEffectTrialOmen));
    public static readonly Holder<NacreCraft.Registry.MobEffect> RAID_OMEN = Register("raid_omen", new RaidOmenMobEffect(MobEffectCategory.Neutral, 14565464)
        .WithSoundOnAdded(SoundEvents.ApplyEffectRaidOmen));
    public static readonly Holder<NacreCraft.Registry.MobEffect> WIND_CHARGED = Register("wind_charged", new WindChargedMobEffect(MobEffectCategory.Harmful, 12438015));
    public static readonly Holder<NacreCraft.Registry.MobEffect> WEAVING = Register("weaving", new WeavingMobEffect(MobEffectCategory.Harmful, 7891290,
        random => Mth.RandomBetweenInclusive(random, 2, 3)));
    public static readonly Holder<NacreCraft.Registry.MobEffect> OOZING = Register("oozing", new OozingMobEffect(MobEffectCategory.Harmful, 10092451,
        _ => 2));
    public static readonly Holder<NacreCraft.Registry.MobEffect> INFESTED = Register("infested", new InfestedMobEffect(MobEffectCategory.Harmful, 9214860, 0.1f,
        random => Mth.RandomBetweenInclusive(random, 1, 2)));
    public static readonly Holder<NacreCraft.Registry.MobEffect> BREATH_OF_THE_NAUTILUS = Register("breath_of_the_nautilus", new MobEffect(MobEffectCategory.Beneficial, 65518));

    //Bootstrap triggers built-in effect registration, idempotent
    //Registration is actually done by field initializers in declaration order on first type access; this only sets the idempotent flag
    public static void Bootstrap()
    {
        if (_bootstrapped) return;
        _bootstrapped = true;
    }

    //Find fetches the effect implementation by registry name; returns null when unregistered or not implemented by this subsystem
    public static MobEffect? Find(Identifier id)
        => BuiltInRegistries.MOB_EFFECT.GetValue(id) as MobEffect;

    //Register registered in call order; the registry id is decided by registration order
    private static Holder<NacreCraft.Registry.MobEffect> Register(string path, NacreCraft.Registry.MobEffect value)
    {
        var id = Identifier.WithDefaultNamespace(path);
        var key = ResourceKey<NacreCraft.Registry.MobEffect>.Create(Registries.MOB_EFFECT, id);
        return Registry<NacreCraft.Registry.MobEffect>.RegisterForHolder(BuiltInRegistries.MOB_EFFECT, key, value);
    }
}
