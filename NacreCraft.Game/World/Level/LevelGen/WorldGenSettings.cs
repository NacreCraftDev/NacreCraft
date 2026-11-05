namespace NacreCraft.Game.World.Level.LevelGen;

//WorldGenSettings world generation settings, maps to vanilla net.minecraft.world.level.levelgen.WorldGenSettings
//Vanilla persists this as a SavedData; this port persists through WorldGenSettingsData, which reads and writes the
//same options/dimensions layout, so this class only carries the in-memory shape for callers
public sealed class WorldGenSettings
{
    public WorldGenSettings(WorldOptions options, WorldDimensions dimensions)
    {
        Options = options;
        Dimensions = dimensions;
    }

    public WorldOptions Options { get; }

    public WorldDimensions Dimensions { get; }

    //Of builds settings from options plus the dimension stems of a registry, maps to vanilla of
    //The registry overload is degraded: the level stem registry type is not generic yet, so callers pass the map directly
    public static WorldGenSettings Of(WorldOptions options, IReadOnlyDictionary<string, Dimension.LevelStem> stems)
        => new(options, new WorldDimensions(stems));

    public override string ToString() => $"WorldGenSettings[options={Options}, dimensions={Dimensions}]";
}
