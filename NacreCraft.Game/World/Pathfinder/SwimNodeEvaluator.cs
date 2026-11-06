using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Pathfinder;

//SwimNodeEvaluator swimming mob rules, maps to vanilla net.minecraft.world.level.pathfinder.SwimNodeEvaluator
//Only water cells are walkable; breaching lets a water mob surface into the air cell above a pathfindable surface
public class SwimNodeEvaluator : NodeEvaluator
{
    private readonly bool _allowBreaching;
    private readonly Dictionary<long, PathType> _pathTypesByPosCache = new();

    public SwimNodeEvaluator(bool allowBreaching) => _allowBreaching = allowBreaching;

    public override void Prepare(PathNavigationRegion level, Mob entity)
    {
        base.Prepare(level, entity);
        _pathTypesByPosCache.Clear();
    }

    public override void Done()
    {
        base.Done();
        _pathTypesByPosCache.Clear();
    }

    public override Node? GetStart()
    {
        var bb = Mob.BoundingBox;
        return GetNode(Mth.Floor(bb.Min.X), Mth.Floor(bb.Min.Y + 0.5), Mth.Floor(bb.Min.Z));
    }

    public override Target GetTarget(double x, double y, double z) => GetTargetNodeAt(x, y, z);

    public override int GetNeighbors(Node[] neighbors, Node pos)
    {
        var count = 0;
        var nodes = new Dictionary<Direction, Node?>();
        foreach (var direction in Direction.Values)
        {
            var node = FindAcceptedNode(pos.X + direction.StepX, pos.Y + direction.StepY, pos.Z + direction.StepZ);
            nodes[direction] = node;
            if (IsNodeValid(node)) neighbors[count++] = node!;
        }
        foreach (var direction in new[] { Direction.North, Direction.East, Direction.South, Direction.West })
        {
            var secondDirection = direction.ClockWise;
            if (HasMalus(nodes[direction]) && HasMalus(nodes[secondDirection]))
            {
                var diagonalNode = FindAcceptedNode(pos.X + direction.StepX + secondDirection.StepX, pos.Y,
                    pos.Z + direction.StepZ + secondDirection.StepZ);
                if (IsNodeValid(diagonalNode)) neighbors[count++] = diagonalNode!;
            }
        }
        return count;
    }

    protected bool IsNodeValid(Node? node) => node is not null && !node.Closed;

    private static bool HasMalus(Node? node) => node is not null && node.CostMalus >= 0.0f;

    protected Node? FindAcceptedNode(int x, int y, int z)
    {
        Node? best = null;
        var pathType = GetCachedBlockType(x, y, z);
        if ((_allowBreaching && pathType == PathType.BREACH) || pathType == PathType.WATER)
        {
            var pathCost = Mob.GetPathfindingMalus(pathType);
            if (pathCost >= 0.0f)
            {
                best = GetNode(x, y, z);
                best.Type = pathType;
                best.CostMalus = Math.Max(best.CostMalus, pathCost);
                if (CurrentContext.GetFluidState(new BlockPos(x, y, z)).IsEmpty) best.CostMalus += 8.0f;
            }
        }
        return best;
    }

    protected PathType GetCachedBlockType(int x, int y, int z)
    {
        var key = BlockPos.AsLong(x, y, z);
        if (_pathTypesByPosCache.TryGetValue(key, out var cached)) return cached;
        var computed = GetPathType(CurrentContext, x, y, z);
        _pathTypesByPosCache[key] = computed;
        return computed;
    }

    public override PathType GetPathType(PathfindingContext context, int x, int y, int z)
        => GetPathTypeOfMob(context, x, y, z, Mob);

    public override PathType GetPathTypeOfMob(PathfindingContext context, int x, int y, int z, Mob mob)
    {
        //Vanilla reuses one mutable position through the loops and checks the last cell afterwards, kept identical here
        var lastPos = new BlockPos(x, y, z);
        for (var xx = x; xx < x + EntityWidth; xx++)
        for (var yy = y; yy < y + EntityHeight; yy++)
        for (var zz = z; zz < z + EntityDepth; zz++)
        {
            lastPos = new BlockPos(xx, yy, zz);
            var blockState = context.GetBlockState(lastPos);
            var fluidState = blockState.FluidState;
            var belowState = context.GetBlockState(lastPos.Offset(0, -1, 0));
            if (fluidState.IsEmpty && belowState.IsPathfindable(PathComputationType.WATER) && blockState.IsAir)
                return PathType.BREACH;
            if (!fluidState.IsWater) return PathType.BLOCKED;
        }
        return context.GetBlockState(lastPos).IsPathfindable(PathComputationType.WATER)
            ? PathType.WATER
            : PathType.BLOCKED;
    }
}
