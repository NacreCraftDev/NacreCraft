using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Entity;

//SpawnPlacements the per-entity spawn placement table, maps to vanilla net.minecraft.world.entity.SpawnPlacements
//The table is keyed by entity type; NC's entity registry only holds a handful of types, so no vanilla rows are registered yet.
//The lookup/check API is complete so mob registrations can add rows as the mob classes land
public static class SpawnPlacements
{
    public delegate bool SpawnPredicate(EntityType<object> type, ServerLevel level, EntitySpawnReason spawnReason,
        BlockPos pos, RandomSource random);

    private sealed record Data(Heightmap.Types HeightMap, SpawnPlacementType Placement, SpawnPredicate Predicate);

    private static readonly Dictionary<EntityType<object>, Data> DataByType = new();

    //Register adds or replaces an entity type's spawn data, maps to vanilla register
    public static void Register(EntityType<object> type, SpawnPlacementType placement, Heightmap.Types heightMap,
        SpawnPredicate predicate)
        => DataByType[type] = new Data(heightMap, placement, predicate);

    //CheckSpawnRules runs the registered predicate, maps to vanilla checkSpawnRules
    public static bool CheckSpawnRules(EntityType<object> type, ServerLevel level, EntitySpawnReason spawnReason,
        BlockPos pos, RandomSource random)
        => DataByType.TryGetValue(type, out var data) && data.Predicate(type, level, spawnReason, pos, random);

    //GetPlacementType the placement rule of a type, null when unregistered, maps to vanilla getPlacementType
    public static SpawnPlacementType? GetPlacementType(EntityType<object> type)
        => DataByType.TryGetValue(type, out var data) ? data.Placement : null;

    //GetHeightmap the heightmap a type spawns against, maps to vanilla getHeightmapType
    public static Heightmap.Types? GetHeightmap(EntityType<object> type)
        => DataByType.TryGetValue(type, out var data) ? data.HeightMap : null;

    //IsSpawnPositionOk whether the placement rule accepts the position, maps to vanilla isSpawnPositionOk
    public static bool IsSpawnPositionOk(EntityType<object> type, ServerLevel level, BlockPos pos)
        => GetPlacementType(type) is { } placement && placement.IsSpawnPositionOk(level, pos, type);

    //AdjustSpawnPosition adjusts a candidate position for the type, maps to vanilla adjustSpawnPosition
    public static BlockPos AdjustSpawnPosition(EntityType<object> type, ServerLevel level, BlockPos candidate)
        => GetPlacementType(type) is { } placement ? placement.AdjustSpawnPosition(level, candidate) : candidate;
}
