using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Effect;

//Potion potion content, a name plus the effects it applies, maps to net.minecraft.world.item.alchemy.Potion
public sealed class Potion(string name, params MobEffectInstance[] effects) : NacreCraft.Registry.Potion
{
    public string Name { get; } = name;
    public IReadOnlyList<MobEffectInstance> Effects { get; } = effects;
}

//Potions built-in potion table, mirrors net.minecraft.world.item.alchemy.Potions entry by entry
public static class Potions
{
    public static readonly Holder<NacreCraft.Registry.Potion> WATER = Register("water", "water");
    public static readonly Holder<NacreCraft.Registry.Potion> MUNDANE = Register("mundane", "mundane");
    public static readonly Holder<NacreCraft.Registry.Potion> THICK = Register("thick", "thick");
    public static readonly Holder<NacreCraft.Registry.Potion> AWKWARD = Register("awkward", "awkward");
    public static readonly Holder<NacreCraft.Registry.Potion> NIGHT_VISION = Register("night_vision", "night_vision", new MobEffectInstance(MobEffects.NIGHT_VISION, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_NIGHT_VISION = Register("long_night_vision", "night_vision", new MobEffectInstance(MobEffects.NIGHT_VISION, 9600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> INVISIBILITY = Register("invisibility", "invisibility", new MobEffectInstance(MobEffects.INVISIBILITY, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_INVISIBILITY = Register("long_invisibility", "invisibility", new MobEffectInstance(MobEffects.INVISIBILITY, 9600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LEAPING = Register("leaping", "leaping", new MobEffectInstance(MobEffects.JUMP_BOOST, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_LEAPING = Register("long_leaping", "leaping", new MobEffectInstance(MobEffects.JUMP_BOOST, 9600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_LEAPING = Register("strong_leaping", "leaping", new MobEffectInstance(MobEffects.JUMP_BOOST, 1800, 1, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> FIRE_RESISTANCE = Register("fire_resistance", "fire_resistance", new MobEffectInstance(MobEffects.FIRE_RESISTANCE, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_FIRE_RESISTANCE = Register("long_fire_resistance", "fire_resistance", new MobEffectInstance(MobEffects.FIRE_RESISTANCE, 9600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> SWIFTNESS = Register("swiftness", "swiftness", new MobEffectInstance(MobEffects.SPEED, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_SWIFTNESS = Register("long_swiftness", "swiftness", new MobEffectInstance(MobEffects.SPEED, 9600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_SWIFTNESS = Register("strong_swiftness", "swiftness", new MobEffectInstance(MobEffects.SPEED, 1800, 1, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> SLOWNESS = Register("slowness", "slowness", new MobEffectInstance(MobEffects.SLOWNESS, 1800, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_SLOWNESS = Register("long_slowness", "slowness", new MobEffectInstance(MobEffects.SLOWNESS, 4800, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_SLOWNESS = Register("strong_slowness", "slowness", new MobEffectInstance(MobEffects.SLOWNESS, 400, 3, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> TURTLE_MASTER = Register("turtle_master", "turtle_master", new MobEffectInstance(MobEffects.SLOWNESS, 400, 3, false, true), new MobEffectInstance(MobEffects.RESISTANCE, 400, 2, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_TURTLE_MASTER = Register("long_turtle_master", "turtle_master", new MobEffectInstance(MobEffects.SLOWNESS, 800, 3, false, true), new MobEffectInstance(MobEffects.RESISTANCE, 800, 2, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_TURTLE_MASTER = Register("strong_turtle_master", "turtle_master", new MobEffectInstance(MobEffects.SLOWNESS, 400, 5, false, true), new MobEffectInstance(MobEffects.RESISTANCE, 400, 3, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> WATER_BREATHING = Register("water_breathing", "water_breathing", new MobEffectInstance(MobEffects.WATER_BREATHING, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_WATER_BREATHING = Register("long_water_breathing", "water_breathing", new MobEffectInstance(MobEffects.WATER_BREATHING, 9600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> HEALING = Register("healing", "healing", new MobEffectInstance(MobEffects.INSTANT_HEALTH, 1, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_HEALING = Register("strong_healing", "healing", new MobEffectInstance(MobEffects.INSTANT_HEALTH, 1, 1, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> HARMING = Register("harming", "harming", new MobEffectInstance(MobEffects.INSTANT_DAMAGE, 1, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_HARMING = Register("strong_harming", "harming", new MobEffectInstance(MobEffects.INSTANT_DAMAGE, 1, 1, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> POISON = Register("poison", "poison", new MobEffectInstance(MobEffects.POISON, 900, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_POISON = Register("long_poison", "poison", new MobEffectInstance(MobEffects.POISON, 1800, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_POISON = Register("strong_poison", "poison", new MobEffectInstance(MobEffects.POISON, 432, 1, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> REGENERATION = Register("regeneration", "regeneration", new MobEffectInstance(MobEffects.REGENERATION, 900, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_REGENERATION = Register("long_regeneration", "regeneration", new MobEffectInstance(MobEffects.REGENERATION, 1800, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_REGENERATION = Register("strong_regeneration", "regeneration", new MobEffectInstance(MobEffects.REGENERATION, 450, 1, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRENGTH = Register("strength", "strength", new MobEffectInstance(MobEffects.STRENGTH, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_STRENGTH = Register("long_strength", "strength", new MobEffectInstance(MobEffects.STRENGTH, 9600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> STRONG_STRENGTH = Register("strong_strength", "strength", new MobEffectInstance(MobEffects.STRENGTH, 1800, 1, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> WEAKNESS = Register("weakness", "weakness", new MobEffectInstance(MobEffects.WEAKNESS, 1800, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_WEAKNESS = Register("long_weakness", "weakness", new MobEffectInstance(MobEffects.WEAKNESS, 4800, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LUCK = Register("luck", "luck", new MobEffectInstance(MobEffects.LUCK, 6000, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> SLOW_FALLING = Register("slow_falling", "slow_falling", new MobEffectInstance(MobEffects.SLOW_FALLING, 1800, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> LONG_SLOW_FALLING = Register("long_slow_falling", "slow_falling", new MobEffectInstance(MobEffects.SLOW_FALLING, 4800, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> WIND_CHARGED = Register("wind_charged", "wind_charged", new MobEffectInstance(MobEffects.WIND_CHARGED, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> WEAVING = Register("weaving", "weaving", new MobEffectInstance(MobEffects.WEAVING, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> OOZING = Register("oozing", "oozing", new MobEffectInstance(MobEffects.OOZING, 3600, 0, false, true));
    public static readonly Holder<NacreCraft.Registry.Potion> INFESTED = Register("infested", "infested", new MobEffectInstance(MobEffects.INFESTED, 3600, 0, false, true));

    //Register files one potion and keeps the holder for diagnostics
    private static Holder<NacreCraft.Registry.Potion> Register(string id, string name, params MobEffectInstance[] effects)
        => Registry<NacreCraft.Registry.Potion>.RegisterForHolder(BuiltInRegistries.POTION,
            ResourceKey<NacreCraft.Registry.Potion>.Create(Registries.POTION, Identifier.WithDefaultNamespace(id)),
            new Potion(name, effects));

    private static bool _registered;

    //Bootstrap registers every built-in potion before the registry freezes
    public static void Bootstrap()
    {
        if (_registered) return;
        _registered = true;
        //Touch one field so the type initializer runs even when the caller never reads a potion
        _ = WATER;
    }
}

