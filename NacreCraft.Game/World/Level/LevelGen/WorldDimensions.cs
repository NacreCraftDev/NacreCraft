using NacreCraft.Registry;
using LevelStem = NacreCraft.Game.World.Level.LevelGen.Dimension.LevelStem;

namespace NacreCraft.Game.World.Level.LevelGen;

//WorldDimensions the dimension stem map of a world, maps to vanilla net.minecraft.world.level.levelgen.WorldDimensions
//Vanilla keys by ResourceKey<LevelStem>; this port keys by registry name because the LevelStem registry key type is not generic yet
public sealed class WorldDimensions
{
    private const string OverworldName = "minecraft:overworld";
    private const string NetherName = "minecraft:the_nether";
    private const string EndName = "minecraft:the_end";

    //BuiltinOrder built-in dimension order, maps to vanilla BUILTIN_ORDER
    private static readonly string[] BuiltinOrder = { OverworldName, NetherName, EndName };

    private readonly Dictionary<string, LevelStem> _dimensions;

    public WorldDimensions(IReadOnlyDictionary<string, LevelStem> dimensions)
    {
        if (!dimensions.ContainsKey(OverworldName))
            throw new InvalidOperationException("Overworld settings missing");
        _dimensions = new Dictionary<string, LevelStem>(dimensions, StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, LevelStem> Dimensions => _dimensions;

    //KeysInOrder built-in dimensions first, then the remaining keys, maps to vanilla keysInOrder
    public static IEnumerable<string> KeysInOrder(IReadOnlyCollection<string> knownKeys)
    {
        foreach (var key in BuiltinOrder)
            if (knownKeys.Contains(key))
                yield return key;
        foreach (var key in knownKeys)
            if (!BuiltinOrder.Contains(key))
                yield return key;
    }

    //Overworld overworld generator, maps to vanilla overworld
    public ChunkGenerator Overworld
        => _dimensions.TryGetValue(OverworldName, out var stem)
            ? stem.Generator
            : throw new InvalidOperationException("Overworld settings missing");

    public LevelStem? Get(string key) => _dimensions.GetValueOrDefault(key);

    //IsDebug whether the overworld uses the debug generator, maps to vanilla isDebug
    public bool IsDebug() => Overworld is DebugLevelSource;

    //ReplaceOverworldGenerator swaps the overworld generator, maps to vanilla replaceOverworldGenerator
    public WorldDimensions ReplaceOverworldGenerator(ChunkGenerator generator)
    {
        var copy = new Dictionary<string, LevelStem>(_dimensions, StringComparer.Ordinal);
        var typeId = copy.TryGetValue(OverworldName, out var stem) ? stem.TypeId : Identifier.Parse(OverworldName);
        copy[OverworldName] = new LevelStem(typeId, generator);
        return new WorldDimensions(copy);
    }
}
