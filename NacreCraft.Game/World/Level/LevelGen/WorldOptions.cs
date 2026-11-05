using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen;

//WorldOptions world creation options, maps to vanilla net.minecraft.world.level.levelgen.WorldOptions
//Carries the seed, structure and bonus chest switches plus the legacy customised options marker
public sealed class WorldOptions
{
    //DemoOptions demo world options, maps to vanilla DEMO_OPTIONS
    //Vanilla uses "North Carolina".hashCode(); reproduced here so a demo world has the same seed
    public static readonly WorldOptions DemoOptions = new(JavaStringHash("North Carolina"), true, true);

    public WorldOptions(long seed, bool generateStructures, bool generateBonusChest)
        : this(seed, generateStructures, generateBonusChest, null)
    {
    }

    private WorldOptions(long seed, bool generateStructures, bool generateBonusChest, string? legacyCustomOptions)
    {
        Seed = seed;
        GenerateStructures = generateStructures;
        GenerateBonusChest = generateBonusChest;
        LegacyCustomOptions = legacyCustomOptions;
    }

    public long Seed { get; }

    public bool GenerateStructures { get; }

    public bool GenerateBonusChest { get; }

    //LegacyCustomOptions present for worlds created with the removed customised generator
    public string? LegacyCustomOptions { get; }

    //DefaultWithRandomSeed default options for a new world, maps to vanilla defaultWithRandomSeed
    public static WorldOptions DefaultWithRandomSeed() => new(RandomSeed(), true, false);

    //TestWorldWithRandomSeed options for a test world, no structures, maps to vanilla testWorldWithRandomSeed
    public static WorldOptions TestWorldWithRandomSeed() => new(RandomSeed(), false, false);

    public bool IsOldCustomizedWorld() => LegacyCustomOptions is not null;

    public WorldOptions WithBonusChest(bool generateBonusChest)
        => new(Seed, GenerateStructures, generateBonusChest, LegacyCustomOptions);

    public WorldOptions WithStructures(bool generateStructures)
        => new(Seed, generateStructures, GenerateBonusChest, LegacyCustomOptions);

    //WithSeed replaces the seed, an empty value draws a random one, maps to vanilla withSeed
    public WorldOptions WithSeed(long? seed)
        => new(seed ?? RandomSeed(), GenerateStructures, GenerateBonusChest, LegacyCustomOptions);

    //ParseSeed parses a seed string; a non-numeric value falls back to its string hash, maps to vanilla parseSeed
    //Java's String.hashCode is reproduced here because C#'s GetHashCode is randomised per process
    public static long? ParseSeed(string? seedString)
    {
        var trimmed = seedString?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        return long.TryParse(trimmed, out var seed) ? seed : JavaStringHash(trimmed);
    }

    //RandomSeed draws a random seed, maps to vanilla randomSeed
    public static long RandomSeed() => RandomSource.Create().NextLong();

    //JavaStringHash reproduces java.lang.String.hashCode's base-31 rolling hash
    private static int JavaStringHash(string value)
    {
        var hash = 0;
        foreach (var c in value) hash = unchecked(hash * 31 + c);
        return hash;
    }
}
