using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Pathfinder;

//PathTypeCache fixed-size direct-mapped cache of computed path types, maps to vanilla net.minecraft.world.level.pathfinder.PathTypeCache
//Vanilla hangs it on ServerLevel and invalidates on block updates; ServerLevel is not wired up yet, so it is kept for the level to adopt later
public sealed class PathTypeCache
{
    private const int Size = 4096;
    private const int Mask = Size - 1;
    private readonly long[] _positions = new long[Size];
    private readonly PathType?[] _pathTypes = new PathType?[Size];

    public PathType GetOrCompute(BlockGetter level, BlockPos pos)
    {
        var key = pos.AsLong();
        var index = Index(key);
        var cached = Get(index, key);
        return cached ?? Compute(level, pos, index, key);
    }

    private PathType? Get(int index, long key) => _positions[index] == key ? _pathTypes[index] : null;

    private PathType Compute(BlockGetter level, BlockPos pos, int index, long key)
    {
        var pathType = WalkNodeEvaluator.GetPathTypeFromState(level, pos);
        _positions[index] = key;
        _pathTypes[index] = pathType;
        return pathType;
    }

    public void Invalidate(BlockPos pos)
    {
        var key = pos.AsLong();
        var index = Index(key);
        if (_positions[index] == key) _pathTypes[index] = null;
    }

    //Index mixes the packed position the same way fastutil HashCommon.mix does, maps to vanilla index
    private static int Index(long pos)
    {
        var mixed = unchecked(pos * (long)0x9E3779B97F4A7C15UL);
        return (int)((mixed ^ (long)((ulong)mixed >> 32)) & Mask);
    }
}
