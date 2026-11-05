using NacreCraft.Registry;

namespace NacreCraft.Storage;

//LevelDataAndDimensions maps to vanilla net.minecraft.world.level.storage.LevelDataAndDimensions
//The level metadata plus the world gen settings and the dimension table, produced while loading level.dat
//Degraded: WorldDimensions.Complete lives in the Game levelgen layer, so the dimension table degrades to the level keys
//already known to storage; the gen settings use the existing WorldGenSettingsData SavedData
public sealed record LevelDataAndDimensions(
    LevelDataAndDimensions.WorldDataAndGenSettings DataAndGenSettings,
    IReadOnlyList<ResourceKey<Level>> Dimensions)
{
    public static LevelDataAndDimensions Create(
        WorldData data,
        WorldGenSettingsData genSettings,
        IReadOnlyList<ResourceKey<Level>> dimensions)
        => new(new WorldDataAndGenSettings(data, genSettings), dimensions);

    //WorldDataAndGenSettings maps to the vanilla nested record LevelDataAndDimensions.WorldDataAndGenSettings
    public sealed record WorldDataAndGenSettings(WorldData Data, WorldGenSettingsData GenSettings);
}
