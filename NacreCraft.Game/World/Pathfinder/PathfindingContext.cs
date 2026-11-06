using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Pathfinder;

//PathfindingContext the read view shared by one pathfinding run, maps to vanilla net.minecraft.world.level.pathfinder.PathfindingContext
//Holds the mob position and the path type cache; the ServerLevel-level cache is not wired up, so every query computes from the block state
public sealed class PathfindingContext
{
    private readonly CollisionGetter _level;
    private readonly PathTypeCache? _cache;
    private readonly BlockPos _mobPosition;

    public PathfindingContext(CollisionGetter level, Mob mob)
    {
        _level = level;
        //Vanilla takes ServerLevel#getPathTypeCache when the mob is in a server level; the cache is not wired into ServerLevel yet
        //Computing every query is slower but never stale, behavior is unchanged
        _cache = null;
        _mobPosition = mob.BlockPosition();
    }

    //GetPathTypeFromState path type of one block position, maps to vanilla getPathTypeFromState
    public PathType GetPathTypeFromState(int x, int y, int z)
    {
        var pos = new BlockPos(x, y, z);
        return _cache is null
            ? WalkNodeEvaluator.GetPathTypeFromState(_level, pos)
            : _cache.GetOrCompute(_level, pos);
    }

    public BlockState GetBlockState(BlockPos pos) => _level.GetBlockState(pos.X, pos.Y, pos.Z);

    //GetFluidState fluid state at the position, taken from the block state because the block view exposes no fluid read
    public FluidState GetFluidState(BlockPos pos) => GetBlockState(pos).FluidState;

    public CollisionGetter Level => _level;

    public BlockPos MobPosition => _mobPosition;
}
