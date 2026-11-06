using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Pathfinder;

//AmphibiousNodeEvaluator rules for mobs that walk and swim, maps to vanilla net.minecraft.world.level.pathfinder.AmphibiousNodeEvaluator
//Water becomes cheap and walkable cells become expensive, then the vertical neighbors that the walk rules skip are added back
public class AmphibiousNodeEvaluator : WalkNodeEvaluator
{
    private readonly bool _prefersShallowSwimming;
    private float _oldWalkableCost;
    private float _oldWaterBorderCost;

    public AmphibiousNodeEvaluator(bool prefersShallowSwimming) => _prefersShallowSwimming = prefersShallowSwimming;

    public override void Prepare(PathNavigationRegion level, Mob entity)
    {
        base.Prepare(level, entity);
        entity.SetPathfindingMalus(PathType.WATER, 0.0f);
        _oldWalkableCost = entity.GetPathfindingMalus(PathType.WALKABLE);
        entity.SetPathfindingMalus(PathType.WALKABLE, 6.0f);
        _oldWaterBorderCost = entity.GetPathfindingMalus(PathType.WATER_BORDER);
        entity.SetPathfindingMalus(PathType.WATER_BORDER, 4.0f);
    }

    public override void Done()
    {
        Mob.SetPathfindingMalus(PathType.WALKABLE, _oldWalkableCost);
        Mob.SetPathfindingMalus(PathType.WATER_BORDER, _oldWaterBorderCost);
        base.Done();
    }

    public override Node? GetStart()
    {
        if (!Mob.IsInWater) return base.GetStart();
        var bb = Mob.BoundingBox;
        return GetStartNode(new BlockPos(Mth.Floor(bb.Min.X), Mth.Floor(bb.Min.Y + 0.5), Mth.Floor(bb.Min.Z)));
    }

    public override Target GetTarget(double x, double y, double z) => GetTargetNodeAt(x, y + 0.5, z);

    public override int GetNeighbors(Node[] neighbors, Node pos)
    {
        var numValidNeighbors = base.GetNeighbors(neighbors, pos);
        var blockPathTypeAbove = GetCachedPathType(pos.X, pos.Y + 1, pos.Z);
        var blockPathTypeCurrent = GetCachedPathType(pos.X, pos.Y, pos.Z);
        var jumpSize = Mob.GetPathfindingMalus(blockPathTypeAbove) >= 0.0f && blockPathTypeCurrent != PathType.STICKY_HONEY
            ? Mth.Floor(Math.Max(1.0f, (float)Mob.MaxUpStep))
            : 0;
        var posHeight = GetFloorLevel(new BlockPos(pos.X, pos.Y, pos.Z));
        var upNode = FindAcceptedNode(pos.X, pos.Y + 1, pos.Z, Math.Max(0, jumpSize - 1), posHeight,
            Direction.Up, blockPathTypeCurrent);
        var downNode = FindAcceptedNode(pos.X, pos.Y - 1, pos.Z, jumpSize, posHeight,
            Direction.Down, blockPathTypeCurrent);
        if (IsVerticalNeighborValid(upNode, pos)) neighbors[numValidNeighbors++] = upNode!;
        if (IsVerticalNeighborValid(downNode, pos) && blockPathTypeCurrent != PathType.TRAPDOOR)
            neighbors[numValidNeighbors++] = downNode!;
        for (var i = 0; i < numValidNeighbors; i++)
        {
            var neighbor = neighbors[i];
            if (neighbor.Type != PathType.WATER || !_prefersShallowSwimming || neighbor.Y >= GetSeaLevel() - 10) continue;
            neighbor.CostMalus += 1.0f;
        }
        return numValidNeighbors;
    }

    //GetSeaLevel the level sea level; the path region is not a level reader, so the vanilla overworld value is the fallback
    private int GetSeaLevel() => CurrentContext.Level is LevelReader reader ? reader.GetSeaLevel() : 63;

    private bool IsVerticalNeighborValid(Node? verticalNode, Node pos)
        => IsNeighborValid(verticalNode, pos) && verticalNode!.Type == PathType.WATER;

    protected override bool IsAmphibious() => true;

    public override PathType GetPathType(PathfindingContext context, int x, int y, int z)
    {
        var blockPathType = context.GetPathTypeFromState(x, y, z);
        if (blockPathType == PathType.WATER)
        {
            foreach (var direction in Direction.Values)
            {
                var neighbor = new BlockPos(x + direction.StepX, y + direction.StepY, z + direction.StepZ);
                var pathType = context.GetPathTypeFromState(neighbor.X, neighbor.Y, neighbor.Z);
                if (pathType == PathType.BLOCKED) return PathType.WATER_BORDER;
            }
            return PathType.WATER;
        }
        return base.GetPathType(context, x, y, z);
    }
}
