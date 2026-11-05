using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Alchemy;

//PotionIds built-in potion keys, maps to vanilla net.minecraft.world.item.alchemy.PotionIds
public static class PotionIds
{
    public static readonly ResourceKey<Potion> Water = Key("water");
    public static readonly ResourceKey<Potion> Mundane = Key("mundane");
    public static readonly ResourceKey<Potion> Thick = Key("thick");
    public static readonly ResourceKey<Potion> Awkward = Key("awkward");
    public static readonly ResourceKey<Potion> NightVision = Key("night_vision");
    public static readonly ResourceKey<Potion> LongNightVision = Key("long_night_vision");
    public static readonly ResourceKey<Potion> Invisibility = Key("invisibility");
    public static readonly ResourceKey<Potion> LongInvisibility = Key("long_invisibility");
    public static readonly ResourceKey<Potion> Leaping = Key("leaping");
    public static readonly ResourceKey<Potion> LongLeaping = Key("long_leaping");
    public static readonly ResourceKey<Potion> StrongLeaping = Key("strong_leaping");
    public static readonly ResourceKey<Potion> FireResistance = Key("fire_resistance");
    public static readonly ResourceKey<Potion> LongFireResistance = Key("long_fire_resistance");
    public static readonly ResourceKey<Potion> Swiftness = Key("swiftness");
    public static readonly ResourceKey<Potion> LongSwiftness = Key("long_swiftness");
    public static readonly ResourceKey<Potion> StrongSwiftness = Key("strong_swiftness");
    public static readonly ResourceKey<Potion> Slowness = Key("slowness");
    public static readonly ResourceKey<Potion> LongSlowness = Key("long_slowness");
    public static readonly ResourceKey<Potion> StrongSlowness = Key("strong_slowness");
    public static readonly ResourceKey<Potion> TurtleMaster = Key("turtle_master");
    public static readonly ResourceKey<Potion> LongTurtleMaster = Key("long_turtle_master");
    public static readonly ResourceKey<Potion> StrongTurtleMaster = Key("strong_turtle_master");
    public static readonly ResourceKey<Potion> WaterBreathing = Key("water_breathing");
    public static readonly ResourceKey<Potion> LongWaterBreathing = Key("long_water_breathing");
    public static readonly ResourceKey<Potion> Healing = Key("healing");
    public static readonly ResourceKey<Potion> StrongHealing = Key("strong_healing");
    public static readonly ResourceKey<Potion> Harming = Key("harming");
    public static readonly ResourceKey<Potion> StrongHarming = Key("strong_harming");
    public static readonly ResourceKey<Potion> Poison = Key("poison");
    public static readonly ResourceKey<Potion> LongPoison = Key("long_poison");
    public static readonly ResourceKey<Potion> StrongPoison = Key("strong_poison");
    public static readonly ResourceKey<Potion> Regeneration = Key("regeneration");
    public static readonly ResourceKey<Potion> LongRegeneration = Key("long_regeneration");
    public static readonly ResourceKey<Potion> StrongRegeneration = Key("strong_regeneration");
    public static readonly ResourceKey<Potion> Strength = Key("strength");
    public static readonly ResourceKey<Potion> LongStrength = Key("long_strength");
    public static readonly ResourceKey<Potion> StrongStrength = Key("strong_strength");
    public static readonly ResourceKey<Potion> Weakness = Key("weakness");
    public static readonly ResourceKey<Potion> LongWeakness = Key("long_weakness");
    public static readonly ResourceKey<Potion> Luck = Key("luck");
    public static readonly ResourceKey<Potion> SlowFalling = Key("slow_falling");
    public static readonly ResourceKey<Potion> LongSlowFalling = Key("long_slow_falling");
    public static readonly ResourceKey<Potion> WindCharged = Key("wind_charged");
    public static readonly ResourceKey<Potion> Weaving = Key("weaving");
    public static readonly ResourceKey<Potion> Oozing = Key("oozing");
    public static readonly ResourceKey<Potion> Infested = Key("infested");

    private static ResourceKey<Potion> Key(string name)
        => ResourceKey<Potion>.Create(Registries.POTION, Identifier.WithDefaultNamespace(name));
}
