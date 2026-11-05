using NacreCraft.Primitives;

namespace NacreCraft.Storage;

//PlayerSpawnFinder searches a safe spawn position, maps to vanilla net.minecraft.server.level.PlayerSpawnFinder
//The vanilla search samples columns near the world spawn and checks block and fluid safety; terrain checks are not ported
public static class PlayerSpawnFinder
{
    //FindSpawnPositions candidate spawn positions around a centre, nearest first
    //Returns an empty list until the terrain safety checks land, so callers fall back to the world spawn
    public static IReadOnlyList<BlockPos> FindSpawnPositions(BlockPos center, int radius)
        => Array.Empty<BlockPos>();
}
