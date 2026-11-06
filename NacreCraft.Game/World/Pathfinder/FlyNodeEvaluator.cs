using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Primitives.Phys;
using NacreCraft.Registry.State;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Pathfinder;

//FlyNodeEvaluator flying mob rules, maps to vanilla net.minecraft.world.level.pathfinder.FlyNodeEvaluator
//Neighbors cover all 26 surrounding cells, with corners gated on their two edge neighbors
public class FlyNodeEvaluator : WalkNodeEvaluator
{
    private const float SmallMobSize = 1.0f;
    private const float SmallMobInflatedStartNodeBoundingBox = 1.1f;
    private const int MaxStartNodeCandidates = 10;
    private readonly Dictionary<long, PathType> _pathTypeByPosCache = new();

    public override void Prepare(PathNavigationRegion level, Mob entity)
    {
        base.Prepare(level, entity);
        _pathTypeByPosCache.Clear();
        //Vanilla calls onPathfindingStart again here after the walk prepare already did; kept so overrides see the same call count
        entity.OnPathfindingStart();
    }

    public override void Done()
    {
        Mob.OnPathfindingDone();
        _pathTypeByPosCache.Clear();
        base.Done();
    }

    public override Node? GetStart()
    {
        int startY;
        if (CanFloat() && Mob.IsInWater)
        {
            startY = Mth.Floor(Mob.Pos.Y);
            var state = CurrentContext.GetBlockState(new BlockPos(Mth.Floor(Mob.Pos.X), startY, Mth.Floor(Mob.Pos.Z)));
            while (state.Is(Blocks.WATER))
            {
                startY++;
                state = CurrentContext.GetBlockState(new BlockPos(Mth.Floor(Mob.Pos.X), startY, Mth.Floor(Mob.Pos.Z)));
            }
        }
        else
        {
            startY = Mth.Floor(Mob.Pos.Y + 0.5);
        }
        var startPos = new BlockPos(Mth.Floor(Mob.Pos.X), startY, Mth.Floor(Mob.Pos.Z));
        if (!CanStartAt(startPos))
            foreach (var candidate in IteratePathfindingStartNodeCandidatePositions(Mob))
                if (CanStartAt(candidate))
                    return base.GetStartNode(candidate);
        return base.GetStartNode(startPos);
    }

    protected override bool CanStartAt(BlockPos pos)
    {
        var blockPathType = GetCachedPathType(pos.X, pos.Y, pos.Z);
        return Mob.GetPathfindingMalus(blockPathType) >= 0.0f;
    }

    public override Target GetTarget(double x, double y, double z) => GetTargetNodeAt(x, y, z);

    public override int GetNeighbors(Node[] neighbors, Node pos)
    {
        var count = 0;
        var south = FindAcceptedNode(pos.X, pos.Y, pos.Z + 1);
        if (IsOpen(south)) neighbors[count++] = south!;
        var west = FindAcceptedNode(pos.X - 1, pos.Y, pos.Z);
        if (IsOpen(west)) neighbors[count++] = west!;
        var east = FindAcceptedNode(pos.X + 1, pos.Y, pos.Z);
        if (IsOpen(east)) neighbors[count++] = east!;
        var north = FindAcceptedNode(pos.X, pos.Y, pos.Z - 1);
        if (IsOpen(north)) neighbors[count++] = north!;
        var up = FindAcceptedNode(pos.X, pos.Y + 1, pos.Z);
        if (IsOpen(up)) neighbors[count++] = up!;
        var down = FindAcceptedNode(pos.X, pos.Y - 1, pos.Z);
        if (IsOpen(down)) neighbors[count++] = down!;
        var southUp = FindAcceptedNode(pos.X, pos.Y + 1, pos.Z + 1);
        if (IsOpen(southUp) && HasMalus(south) && HasMalus(up)) neighbors[count++] = southUp!;
        var westUp = FindAcceptedNode(pos.X - 1, pos.Y + 1, pos.Z);
        if (IsOpen(westUp) && HasMalus(west) && HasMalus(up)) neighbors[count++] = westUp!;
        var eastUp = FindAcceptedNode(pos.X + 1, pos.Y + 1, pos.Z);
        if (IsOpen(eastUp) && HasMalus(east) && HasMalus(up)) neighbors[count++] = eastUp!;
        var northUp = FindAcceptedNode(pos.X, pos.Y + 1, pos.Z - 1);
        if (IsOpen(northUp) && HasMalus(north) && HasMalus(up)) neighbors[count++] = northUp!;
        var southDown = FindAcceptedNode(pos.X, pos.Y - 1, pos.Z + 1);
        if (IsOpen(southDown) && HasMalus(south) && HasMalus(down)) neighbors[count++] = southDown!;
        var westDown = FindAcceptedNode(pos.X - 1, pos.Y - 1, pos.Z);
        if (IsOpen(westDown) && HasMalus(west) && HasMalus(down)) neighbors[count++] = westDown!;
        var eastDown = FindAcceptedNode(pos.X + 1, pos.Y - 1, pos.Z);
        if (IsOpen(eastDown) && HasMalus(east) && HasMalus(down)) neighbors[count++] = eastDown!;
        var northDown = FindAcceptedNode(pos.X, pos.Y - 1, pos.Z - 1);
        if (IsOpen(northDown) && HasMalus(north) && HasMalus(down)) neighbors[count++] = northDown!;
        var northEast = FindAcceptedNode(pos.X + 1, pos.Y, pos.Z - 1);
        if (IsOpen(northEast) && HasMalus(north) && HasMalus(east)) neighbors[count++] = northEast!;
        var southEast = FindAcceptedNode(pos.X + 1, pos.Y, pos.Z + 1);
        if (IsOpen(southEast) && HasMalus(south) && HasMalus(east)) neighbors[count++] = southEast!;
        var northWest = FindAcceptedNode(pos.X - 1, pos.Y, pos.Z - 1);
        if (IsOpen(northWest) && HasMalus(north) && HasMalus(west)) neighbors[count++] = northWest!;
        var southWest = FindAcceptedNode(pos.X - 1, pos.Y, pos.Z + 1);
        if (IsOpen(southWest) && HasMalus(south) && HasMalus(west)) neighbors[count++] = southWest!;
        var northEastUp = FindAcceptedNode(pos.X + 1, pos.Y + 1, pos.Z - 1);
        if (IsOpen(northEastUp) && HasMalus(northEast) && HasMalus(north) && HasMalus(east) && HasMalus(up)
            && HasMalus(northUp) && HasMalus(eastUp))
            neighbors[count++] = northEastUp!;
        var southEastUp = FindAcceptedNode(pos.X + 1, pos.Y + 1, pos.Z + 1);
        if (IsOpen(southEastUp) && HasMalus(southEast) && HasMalus(south) && HasMalus(east) && HasMalus(up)
            && HasMalus(southUp) && HasMalus(eastUp))
            neighbors[count++] = southEastUp!;
        var northWestUp = FindAcceptedNode(pos.X - 1, pos.Y + 1, pos.Z - 1);
        if (IsOpen(northWestUp) && HasMalus(northWest) && HasMalus(north) && HasMalus(west) && HasMalus(up)
            && HasMalus(northUp) && HasMalus(westUp))
            neighbors[count++] = northWestUp!;
        var southWestUp = FindAcceptedNode(pos.X - 1, pos.Y + 1, pos.Z + 1);
        if (IsOpen(southWestUp) && HasMalus(southWest) && HasMalus(south) && HasMalus(west) && HasMalus(up)
            && HasMalus(southUp) && HasMalus(westUp))
            neighbors[count++] = southWestUp!;
        var northEastDown = FindAcceptedNode(pos.X + 1, pos.Y - 1, pos.Z - 1);
        if (IsOpen(northEastDown) && HasMalus(northEast) && HasMalus(north) && HasMalus(east) && HasMalus(down)
            && HasMalus(northDown) && HasMalus(eastDown))
            neighbors[count++] = northEastDown!;
        var southEastDown = FindAcceptedNode(pos.X + 1, pos.Y - 1, pos.Z + 1);
        if (IsOpen(southEastDown) && HasMalus(southEast) && HasMalus(south) && HasMalus(east) && HasMalus(down)
            && HasMalus(southDown) && HasMalus(eastDown))
            neighbors[count++] = southEastDown!;
        var northWestDown = FindAcceptedNode(pos.X - 1, pos.Y - 1, pos.Z - 1);
        if (IsOpen(northWestDown) && HasMalus(northWest) && HasMalus(north) && HasMalus(west) && HasMalus(down)
            && HasMalus(northDown) && HasMalus(westDown))
            neighbors[count++] = northWestDown!;
        var southWestDown = FindAcceptedNode(pos.X - 1, pos.Y - 1, pos.Z + 1);
        if (IsOpen(southWestDown) && HasMalus(southWest) && HasMalus(south) && HasMalus(west) && HasMalus(down)
            && HasMalus(southDown) && HasMalus(westDown))
            neighbors[count++] = southWestDown!;
        return count;
    }

    private static bool HasMalus(Node? node) => node is not null && node.CostMalus >= 0.0f;

    private static bool IsOpen(Node? node) => node is not null && !node.Closed;

    protected Node? FindAcceptedNode(int x, int y, int z)
    {
        Node? best = null;
        var pathType = GetCachedPathType(x, y, z);
        var pathCost = Mob.GetPathfindingMalus(pathType);
        if (pathCost >= 0.0f)
        {
            best = GetNode(x, y, z);
            best.Type = pathType;
            best.CostMalus = Math.Max(best.CostMalus, pathCost);
            if (pathType == PathType.WALKABLE) best.CostMalus += 1.0f;
        }
        return best;
    }

    protected override PathType GetCachedPathType(int x, int y, int z)
    {
        var key = BlockPos.AsLong(x, y, z);
        if (_pathTypeByPosCache.TryGetValue(key, out var cached)) return cached;
        var computed = GetPathTypeOfMob(CurrentContext, x, y, z, Mob);
        _pathTypeByPosCache[key] = computed;
        return computed;
    }

    public override PathType GetPathType(PathfindingContext context, int x, int y, int z)
    {
        var blockPathType = context.GetPathTypeFromState(x, y, z);
        if (blockPathType == PathType.OPEN && y >= context.Level.MinBuildHeight + 1)
        {
            var belowPos = new BlockPos(x, y - 1, z);
            var belowType = context.GetPathTypeFromState(belowPos.X, belowPos.Y, belowPos.Z);
            if (belowType is PathType.FIRE or PathType.LAVA) blockPathType = PathType.FIRE;
            else if (belowType == PathType.DAMAGING) blockPathType = PathType.DAMAGING;
            else if (belowType == PathType.COCOA) blockPathType = PathType.COCOA;
            else if (belowType == PathType.FENCE)
            {
                if (!belowPos.Equals(context.MobPosition)) blockPathType = PathType.FENCE;
            }
            else
                blockPathType = belowType is PathType.WALKABLE or PathType.OPEN or PathType.WATER
                    ? PathType.OPEN
                    : PathType.WALKABLE;
        }
        if (blockPathType is PathType.WALKABLE or PathType.OPEN)
            blockPathType = CheckNeighbourBlocks(context, x, y, z, blockPathType);
        return blockPathType;
    }

    //IteratePathfindingStartNodeCandidatePositions candidate start cells around the mob box, maps to vanilla method of the same name
    //A small mob probes random cells inside the inflated box, a larger one probes the four box corners
    private static IEnumerable<BlockPos> IteratePathfindingStartNodeCandidatePositions(Mob mob)
    {
        var boundingBox = mob.BoundingBox;
        var isSmallMob = boundingBox.Size.Length() / 3.0 < SmallMobSize;
        var blockY = Mth.Floor(mob.Pos.Y);
        if (!isSmallMob)
        {
            return new[]
            {
                new BlockPos(Mth.Floor(boundingBox.Min.X), blockY, Mth.Floor(boundingBox.Min.Z)),
                new BlockPos(Mth.Floor(boundingBox.Min.X), blockY, Mth.Floor(boundingBox.Max.Z)),
                new BlockPos(Mth.Floor(boundingBox.Max.X), blockY, Mth.Floor(boundingBox.Min.Z)),
                new BlockPos(Mth.Floor(boundingBox.Max.X), blockY, Mth.Floor(boundingBox.Max.Z))
            };
        }
        var zPadding = Math.Max(0.0, SmallMobInflatedStartNodeBoundingBox - boundingBox.Size.Z);
        var xPadding = Math.Max(0.0, SmallMobInflatedStartNodeBoundingBox - boundingBox.Size.X);
        var yPadding = Math.Max(0.0, SmallMobInflatedStartNodeBoundingBox - boundingBox.Size.Y);
        var inflated = boundingBox.Inflate(xPadding, yPadding, zPadding);
        return RandomBetweenClosed(mob.GetRandom(), MaxStartNodeCandidates,
            Mth.Floor(inflated.Min.X), Mth.Floor(inflated.Min.Y), Mth.Floor(inflated.Min.Z),
            Mth.Floor(inflated.Max.X), Mth.Floor(inflated.Max.Y), Mth.Floor(inflated.Max.Z));
    }

    //RandomBetweenClosed draws count random cells inside the closed box, maps to vanilla BlockPos.randomBetweenClosed
    private static List<BlockPos> RandomBetweenClosed(NacreCraft.Util.Random.RandomSource random, int count,
        int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        var dx = maxX - minX + 1;
        var dy = maxY - minY + 1;
        var dz = maxZ - minZ + 1;
        var result = new List<BlockPos>(count);
        for (var i = 0; i < count; i++)
            result.Add(new BlockPos(minX + random.NextInt(dx), minY + random.NextInt(dy), minZ + random.NextInt(dz)));
        return result;
    }
}
