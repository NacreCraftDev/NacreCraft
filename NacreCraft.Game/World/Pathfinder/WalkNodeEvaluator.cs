using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.Material;
using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Primitives.Phys;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Pathfinder;

//WalkNodeEvaluator ground walking rules shared by land, flying and amphibious mobs, maps to vanilla net.minecraft.world.level.pathfinder.WalkNodeEvaluator
//Direction.Plane.HORIZONTAL is expanded to an array in vanilla order NORTH EAST SOUTH WEST
public class WalkNodeEvaluator : NodeEvaluator
{
    public const double SpaceBetweenWallPosts = 0.5;
    private const double DefaultMobJumpHeight = 1.125;

    //HorizontalPlane the four horizontal directions in vanilla plane order, used by neighbor generation
    private static readonly Direction[] HorizontalPlane = { Direction.North, Direction.East, Direction.South, Direction.West };

    private readonly Dictionary<long, PathType> _pathTypesByPosCacheByMob = new();
    private readonly Dictionary<AABB, bool> _collisionCache = new();
    private readonly Node?[] _reusableNeighbors = new Node?[4];

    public override void Prepare(PathNavigationRegion level, Mob entity)
    {
        base.Prepare(level, entity);
        entity.OnPathfindingStart();
    }

    public override void Done()
    {
        Mob.OnPathfindingDone();
        _pathTypesByPosCacheByMob.Clear();
        _collisionCache.Clear();
        base.Done();
    }

    public override Node? GetStart()
    {
        var x = Mob.Pos.X;
        var z = Mob.Pos.Z;
        var startY = Mth.Floor(Mob.Pos.Y);
        var blockState = CurrentContext.GetBlockState(new BlockPos(Mth.Floor(x), startY, Mth.Floor(z)));
        if (Mob.CanStandOnFluid(blockState.FluidState))
        {
            while (Mob.CanStandOnFluid(blockState.FluidState))
            {
                startY++;
                blockState = CurrentContext.GetBlockState(new BlockPos(Mth.Floor(x), startY, Mth.Floor(z)));
            }
            startY--;
        }
        else if (CanFloat() && Mob.IsInWater)
        {
            while (blockState.Is(Blocks.WATER) || ReferenceEquals(blockState.FluidState, Fluids.Water.DefaultFluidState))
            {
                startY++;
                blockState = CurrentContext.GetBlockState(new BlockPos(Mth.Floor(x), startY, Mth.Floor(z)));
            }
            startY--;
        }
        else if (Mob.OnGround)
        {
            startY = Mth.Floor(Mob.Pos.Y + 0.5);
        }
        else
        {
            var y = Mth.Floor(Mob.Pos.Y + 1.0);
            while (y > CurrentContext.Level.MinBuildHeight)
            {
                startY = y;
                y--;
                var belowBlockState = CurrentContext.GetBlockState(new BlockPos(Mth.Floor(x), y, Mth.Floor(z)));
                if (!belowBlockState.IsAir && !belowBlockState.IsPathfindable(PathComputationType.LAND)) break;
            }
        }
        var startPos = Mob.BlockPosition();
        var startCell = new BlockPos(startPos.X, startY, startPos.Z);
        if (!CanStartAt(startCell))
        {
            var mobBb = Mob.BoundingBox;
            var minX = Mth.Floor(mobBb.Min.X);
            var maxX = Mth.Floor(mobBb.Max.X);
            var minZ = Mth.Floor(mobBb.Min.Z);
            var maxZ = Mth.Floor(mobBb.Max.Z);
            var candidates = new[]
            {
                new BlockPos(minX, startY, minZ),
                new BlockPos(minX, startY, maxZ),
                new BlockPos(maxX, startY, minZ),
                new BlockPos(maxX, startY, maxZ)
            };
            foreach (var candidate in candidates)
                if (CanStartAt(candidate)) return GetStartNode(candidate);
        }
        return GetStartNode(startCell);
    }

    protected Node GetStartNode(BlockPos pos)
    {
        var node = GetNode(pos);
        node.Type = GetCachedPathType(node.X, node.Y, node.Z);
        node.CostMalus = Mob.GetPathfindingMalus(node.Type);
        return node;
    }

    protected virtual bool CanStartAt(BlockPos pos)
    {
        var blockPathType = GetCachedPathType(pos.X, pos.Y, pos.Z);
        return blockPathType != PathType.OPEN && Mob.GetPathfindingMalus(blockPathType) >= 0.0f;
    }

    public override Target GetTarget(double x, double y, double z) => GetTargetNodeAt(x, y, z);

    public override int GetNeighbors(Node[] neighbors, Node pos)
    {
        var p = 0;
        var jumpSize = 0;
        var blockPathTypeAbove = GetCachedPathType(pos.X, pos.Y + 1, pos.Z);
        var blockPathTypeCurrent = GetCachedPathType(pos.X, pos.Y, pos.Z);
        if (Mob.GetPathfindingMalus(blockPathTypeAbove) >= 0.0f && blockPathTypeCurrent != PathType.STICKY_HONEY)
            jumpSize = Mth.Floor(Math.Max(1.0f, (float)Mob.MaxUpStep));
        var posHeight = GetFloorLevel(new BlockPos(pos.X, pos.Y, pos.Z));
        foreach (var direction in HorizontalPlane)
        {
            var node = FindAcceptedNode(pos.X + direction.StepX, pos.Y, pos.Z + direction.StepZ,
                jumpSize, posHeight, direction, blockPathTypeCurrent);
            _reusableNeighbors[direction.Get2DDataValue] = node;
            if (IsNeighborValid(node, pos)) neighbors[p++] = node!;
        }
        foreach (var direction in HorizontalPlane)
        {
            var secondDirection = direction.ClockWise;
            if (IsDiagonalValid(pos, _reusableNeighbors[direction.Get2DDataValue],
                    _reusableNeighbors[secondDirection.Get2DDataValue]))
            {
                var diagonalNode = FindAcceptedNode(pos.X + direction.StepX + secondDirection.StepX, pos.Y,
                    pos.Z + direction.StepZ + secondDirection.StepZ, jumpSize, posHeight, direction, blockPathTypeCurrent);
                if (IsDiagonalValid(diagonalNode)) neighbors[p++] = diagonalNode!;
            }
        }
        return p;
    }

    protected bool IsNeighborValid(Node? neighbor, Node current)
        => neighbor is not null && !neighbor.Closed && (neighbor.CostMalus >= 0.0f || current.CostMalus < 0.0f);

    protected bool IsDiagonalValid(Node pos, Node? ew, Node? ns)
    {
        if (ns is null || ew is null || ns.Y > pos.Y || ew.Y > pos.Y
            || ew.Type == PathType.WALKABLE_DOOR || ns.Type == PathType.WALKABLE_DOOR)
            return false;
        if (Mob.Width > 1.0f && (ew.CostMalus > 0.0f || ns.CostMalus > 0.0f)) return false;
        var canPassBetweenPosts = ns.Type == PathType.FENCE && ew.Type == PathType.FENCE && Mob.Width < SpaceBetweenWallPosts;
        return (ns.Y < pos.Y || ns.CostMalus >= 0.0f || canPassBetweenPosts)
               && (ew.Y < pos.Y || ew.CostMalus >= 0.0f || canPassBetweenPosts);
    }

    protected bool IsDiagonalValid(Node? diagonal)
        => diagonal is not null && !diagonal.Closed && diagonal.Type != PathType.WALKABLE_DOOR && diagonal.CostMalus >= 0.0f;

    private static bool DoesBlockHavePartialCollision(PathType type)
        => type is PathType.FENCE or PathType.DOOR_WOOD_CLOSED or PathType.DOOR_IRON_CLOSED;

    private bool CanReachWithoutCollision(Node posTo)
    {
        var bb = Mob.BoundingBox;
        var delta = new Vec3(
            posTo.X - Mob.Pos.X + bb.Size.X / 2.0,
            posTo.Y - Mob.Pos.Y + bb.Size.Y / 2.0,
            posTo.Z - Mob.Pos.Z + bb.Size.Z / 2.0);
        var steps = Mth.Ceil(delta.Length() / bb.Size.Length());
        var delta2 = delta.Multiply(1.0f / steps);
        for (var i = 1; i <= steps; i++)
        {
            bb = bb.Move(delta2);
            if (HasCollisions(bb)) return false;
        }
        return true;
    }

    //GetFloorLevel floor level seen by this evaluator; floating and amphibious mobs treat water as half a block up
    protected double GetFloorLevel(BlockPos pos)
    {
        if ((CanFloat() || IsAmphibious())
            && CurrentContext.GetBlockState(pos).FluidState.IsWater)
            return pos.Y + 0.5;
        return GetFloorLevel(CurrentContext.Level, pos);
    }

    //GetFloorLevel top of the collision shape below the position, maps to vanilla getFloorLevel(BlockGetter, BlockPos)
    public static double GetFloorLevel(BlockGetter level, BlockPos pos)
    {
        var target = pos.Offset(0, -1, 0);
        var state = level.GetBlockState(target.X, target.Y, target.Z);
        var shape = state.Owner is BlockBehaviour behaviour
            ? behaviour.GetCollisionShape(state, level, target, CollisionContext.Empty)
            : Shapes.Empty();
        return target.Y + (shape.IsEmpty ? 0.0 : shape.Max(Direction.Axis.Y));
    }

    protected virtual bool IsAmphibious() => false;

    protected Node? FindAcceptedNode(int x, int y, int z, int jumpSize, double nodeHeight,
        Direction travelDirection, PathType blockPathTypeCurrent)
    {
        Node? best = null;
        var maxYTarget = GetFloorLevel(new BlockPos(x, y, z));
        if (maxYTarget - nodeHeight > GetMobJumpHeight()) return null;
        var pathType = GetCachedPathType(x, y, z);
        var pathCost = Mob.GetPathfindingMalus(pathType);
        if (pathCost >= 0.0f) best = GetNodeAndUpdateCostToMax(x, y, z, pathType, pathCost);
        if (DoesBlockHavePartialCollision(blockPathTypeCurrent) && best is not null && best.CostMalus >= 0.0f
            && !CanReachWithoutCollision(best))
            best = null;
        if (pathType == PathType.WALKABLE || (IsAmphibious() && pathType == PathType.WATER)) return best;
        if ((best is null || best.CostMalus < 0.0f) && jumpSize > 0
            && (pathType != PathType.FENCE || CanWalkOverFences())
            && pathType != PathType.UNPASSABLE_RAIL && pathType != PathType.TRAPDOOR && pathType != PathType.POWDER_SNOW)
            best = TryJumpOn(x, y, z, jumpSize, nodeHeight, travelDirection, blockPathTypeCurrent);
        else if (!IsAmphibious() && pathType == PathType.WATER && !CanFloat())
            best = TryFindFirstNonWaterBelow(x, y, z, best);
        else if (pathType == PathType.OPEN)
            best = TryFindFirstGroundNodeBelow(x, y, z);
        else if (DoesBlockHavePartialCollision(pathType) && best is null)
            best = GetClosedNode(x, y, z, pathType);
        return best;
    }

    private double GetMobJumpHeight() => Math.Max(DefaultMobJumpHeight, Mob.MaxUpStep);

    private Node GetNodeAndUpdateCostToMax(int x, int y, int z, PathType pathType, float cost)
    {
        var node = GetNode(x, y, z);
        node.Type = pathType;
        node.CostMalus = Math.Max(node.CostMalus, cost);
        return node;
    }

    private Node GetBlockedNode(int x, int y, int z)
    {
        var node = GetNode(x, y, z);
        node.Type = PathType.BLOCKED;
        node.CostMalus = -1.0f;
        return node;
    }

    private Node GetClosedNode(int x, int y, int z, PathType pathType)
    {
        var node = GetNode(x, y, z);
        node.Closed = true;
        node.Type = pathType;
        node.CostMalus = pathType.GetMalus();
        return node;
    }

    private Node? TryJumpOn(int x, int y, int z, int jumpSize, double nodeHeight, Direction travelDirection,
        PathType blockPathTypeCurrent)
    {
        var nodeAbove = FindAcceptedNode(x, y + 1, z, jumpSize - 1, nodeHeight, travelDirection, blockPathTypeCurrent);
        if (nodeAbove is null) return null;
        if (Mob.Width >= 1.0f) return nodeAbove;
        if (nodeAbove.Type != PathType.OPEN && nodeAbove.Type != PathType.WALKABLE) return nodeAbove;
        var centerX = x - travelDirection.StepX + 0.5;
        var centerZ = z - travelDirection.StepZ + 0.5;
        var halfWidth = Mob.Width / 2.0;
        var grow = new AABB(
            centerX - halfWidth,
            GetFloorLevel(new BlockPos(Mth.Floor(centerX), y + 1, Mth.Floor(centerZ))) + 0.001,
            centerZ - halfWidth,
            centerX + halfWidth,
            Mob.Height + GetFloorLevel(new BlockPos(nodeAbove.X, nodeAbove.Y, nodeAbove.Z)) - 0.002,
            centerZ + halfWidth);
        return HasCollisions(grow) ? null : nodeAbove;
    }

    private Node? TryFindFirstNonWaterBelow(int x, int y, int z, Node? best)
    {
        while (true)
        {
            y--;
            if (y <= CurrentContext.Level.MinBuildHeight) return best;
            var pathTypeLocal = GetCachedPathType(x, y, z);
            if (pathTypeLocal != PathType.WATER) return best;
            best = GetNodeAndUpdateCostToMax(x, y, z, pathTypeLocal, Mob.GetPathfindingMalus(pathTypeLocal));
        }
    }

    private Node TryFindFirstGroundNodeBelow(int x, int y, int z)
    {
        for (var currentY = y - 1; currentY >= CurrentContext.Level.MinBuildHeight; currentY--)
        {
            if (y - currentY > Mob.GetMaxFallDistance()) return GetBlockedNode(x, currentY, z);
            var pathType = GetCachedPathType(x, currentY, z);
            var pathCost = Mob.GetPathfindingMalus(pathType);
            if (pathType != PathType.OPEN)
                return pathCost >= 0.0f
                    ? GetNodeAndUpdateCostToMax(x, currentY, z, pathType, pathCost)
                    : GetBlockedNode(x, currentY, z);
        }
        return GetBlockedNode(x, y, z);
    }

    private bool HasCollisions(AABB aabb)
    {
        if (_collisionCache.TryGetValue(aabb, out var cached)) return cached;
        var result = !CurrentContext.Level.NoCollision(Mob, aabb);
        _collisionCache[aabb] = result;
        return result;
    }

    protected virtual PathType GetCachedPathType(int x, int y, int z)
    {
        var key = BlockPos.AsLong(x, y, z);
        if (_pathTypesByPosCacheByMob.TryGetValue(key, out var cached)) return cached;
        var computed = GetPathTypeOfMob(CurrentContext, x, y, z, Mob);
        _pathTypesByPosCacheByMob[key] = computed;
        return computed;
    }

    public override PathType GetPathTypeOfMob(PathfindingContext context, int x, int y, int z, Mob mob)
    {
        var blockTypes = GetPathTypeWithinMobBB(context, x, y, z);
        if (blockTypes.Count == 1) return blockTypes.First();
        if (blockTypes.Contains(PathType.FENCE)) return PathType.FENCE;
        if (blockTypes.Contains(PathType.UNPASSABLE_RAIL)) return PathType.UNPASSABLE_RAIL;
        var highestMalusPathTypeWithinBb = PathType.BLOCKED;
        var highestMalusWithinBb = mob.GetPathfindingMalus(highestMalusPathTypeWithinBb);
        //EnumSet iterates in declaration order in vanilla, so the tie breaking below must follow the enum order too
        foreach (var pathType in Enum.GetValues<PathType>())
        {
            if (!blockTypes.Contains(pathType)) continue;
            var malusForPathType = mob.GetPathfindingMalus(pathType);
            if (malusForPathType < 0.0f) return pathType;
            if (malusForPathType >= highestMalusWithinBb)
            {
                highestMalusWithinBb = malusForPathType;
                highestMalusPathTypeWithinBb = pathType;
            }
        }
        var currentNodePathType = GetPathType(context, x, y, z);
        var isLargeMob = EntityWidth > 1;
        if (isLargeMob)
        {
            var isCurrentNodeCheaper = mob.GetPathfindingMalus(currentNodePathType) < highestMalusWithinBb;
            var capMalusDueToCheapNode = isCurrentNodeCheaper
                                          && mob.GetPathfindingMalus(PathType.BIG_MOBS_CLOSE_TO_DANGER) < highestMalusWithinBb;
            return capMalusDueToCheapNode ? PathType.BIG_MOBS_CLOSE_TO_DANGER : highestMalusPathTypeWithinBb;
        }
        if (currentNodePathType == PathType.OPEN && highestMalusPathTypeWithinBb != PathType.OPEN && highestMalusWithinBb == 0.0f)
            return PathType.OPEN;
        return highestMalusPathTypeWithinBb;
    }

    public HashSet<PathType> GetPathTypeWithinMobBB(PathfindingContext context, int x, int y, int z)
    {
        var blockTypes = new HashSet<PathType>();
        var mobPosition = Mob.BlockPosition();
        var canPassDoors = CanPassDoors();
        for (var dx = 0; dx < EntityWidth; dx++)
        for (var dy = 0; dy < EntityHeight; dy++)
        for (var dz = 0; dz < EntityDepth; dz++)
        {
            var xx = dx + x;
            var yy = dy + y;
            var zz = dz + z;
            var blockType = GetPathType(context, xx, yy, zz);
            if (blockType == PathType.DOOR_WOOD_CLOSED && CanOpenDoors() && canPassDoors)
                blockType = PathType.WALKABLE_DOOR;
            if (blockType == PathType.DOOR_OPEN && !canPassDoors) blockType = PathType.BLOCKED;
            if (blockType == PathType.RAIL
                && GetPathType(context, mobPosition.X, mobPosition.Y, mobPosition.Z) != PathType.RAIL
                && GetPathType(context, mobPosition.X, mobPosition.Y - 1, mobPosition.Z) != PathType.RAIL)
                blockType = PathType.UNPASSABLE_RAIL;
            blockTypes.Add(blockType);
        }
        return blockTypes;
    }

    public override PathType GetPathType(PathfindingContext context, int x, int y, int z)
        => GetPathTypeStatic(context, new BlockPos(x, y, z));

    public static PathType GetPathTypeStatic(Mob mob, BlockPos pos)
        => mob.Level is Game.World.Phys.Collision.CollisionGetter level
            ? GetPathTypeStatic(new PathfindingContext(level, mob), pos)
            : PathType.BLOCKED;

    public static PathType GetPathTypeStatic(PathfindingContext context, BlockPos pos)
    {
        var x = pos.X;
        var y = pos.Y;
        var z = pos.Z;
        var blockPathType = context.GetPathTypeFromState(x, y, z);
        if (blockPathType != PathType.OPEN || y < context.Level.MinBuildHeight + 1) return blockPathType;
        return context.GetPathTypeFromState(x, y - 1, z) switch
        {
            PathType.OPEN or PathType.WATER or PathType.LAVA or PathType.WALKABLE => PathType.OPEN,
            PathType.FIRE => PathType.FIRE,
            PathType.DAMAGING => PathType.DAMAGING,
            PathType.STICKY_HONEY => PathType.STICKY_HONEY,
            PathType.POWDER_SNOW => PathType.ON_TOP_OF_POWDER_SNOW,
            PathType.DAMAGE_CAUTIOUS => PathType.DAMAGE_CAUTIOUS,
            PathType.TRAPDOOR => PathType.ON_TOP_OF_TRAPDOOR,
            _ => CheckNeighbourBlocks(context, x, y, z, PathType.WALKABLE)
        };
    }

    public static PathType CheckNeighbourBlocks(PathfindingContext context, int x, int y, int z, PathType blockPathType)
    {
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        for (var dz = -1; dz <= 1; dz++)
        {
            if (dx == 0 && dz == 0) continue;
            var pathType = context.GetPathTypeFromState(x + dx, y + dy, z + dz);
            if (pathType == PathType.DAMAGING) return PathType.DAMAGING_IN_NEIGHBOR;
            if (pathType is PathType.FIRE or PathType.LAVA) return PathType.FIRE_IN_NEIGHBOR;
            if (pathType == PathType.WATER) return PathType.WATER_BORDER;
            if (pathType == PathType.DAMAGE_CAUTIOUS) return PathType.DAMAGE_CAUTIOUS;
        }
        return blockPathType;
    }

    //GetPathTypeFromState base path type of one cell from its block state, maps to vanilla getPathTypeFromState
    //Blocks not registered in this project yet are matched by registry name, see PathfinderBlocks
    internal static PathType GetPathTypeFromState(BlockGetter level, BlockPos pos)
    {
        var blockState = level.GetBlockState(pos.X, pos.Y, pos.Z);
        var block = blockState.Owner;
        if (blockState.IsAir) return PathType.OPEN;
        if (blockState.Is(BlockTags.Trapdoors) || blockState.Is(Blocks.LILY_PAD)
            || PathfinderBlocks.IsNamed(blockState, "big_dripleaf"))
            return PathType.TRAPDOOR;
        if (PathfinderBlocks.IsNamed(blockState, "powder_snow")) return PathType.POWDER_SNOW;
        if (blockState.Is(Blocks.CACTUS) || blockState.Is(Blocks.SWEET_BERRY_BUSH)) return PathType.DAMAGING;
        if (blockState.Is(Blocks.HONEY_BLOCK)) return PathType.STICKY_HONEY;
        if (PathfinderBlocks.IsNamed(blockState, "cocoa")) return PathType.COCOA;
        if (PathfinderBlocks.IsNamed(blockState, "wither_rose") || blockState.Is(BlockTags.Speleothems))
            return PathType.DAMAGE_CAUTIOUS;
        var fluidState = blockState.FluidState;
        if (fluidState.IsLava) return PathType.LAVA;
        if (IsBurningBlock(blockState)) return PathType.FIRE;
        if (block is Blocks.DoorBlock door)
            return blockState.GetValue(BlockStateProperties.Open)
                ? PathType.DOOR_OPEN
                : door.CanOpenByHand ? PathType.DOOR_WOOD_CLOSED : PathType.DOOR_IRON_CLOSED;
        if (block is BaseRailBlock) return PathType.RAIL;
        if (block is LeavesBlock) return PathType.LEAVES;
        if (blockState.Is(BlockTags.Fences) || blockState.Is(BlockTags.Walls)
            || (block is Blocks.FenceGateBlock && !blockState.GetValue(BlockStateProperties.Open)))
            return PathType.FENCE;
        if (!blockState.IsPathfindable(PathComputationType.LAND)) return PathType.BLOCKED;
        return fluidState.IsWater ? PathType.WATER : PathType.OPEN;
    }
}
