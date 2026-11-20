using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Updates;
using NacreCraft.Util;
//The block state axis property uses the registry enum (lowercase x/y/z), not the direction axis enum
using Axis = NacreCraft.Registry.Enums.Axis;

namespace NacreCraft.Game.World.Level.Portal;

//PortalShape a detected or to-be-created nether portal frame, maps to vanilla net.minecraft.world.level.portal.PortalShape
//The shape scans outward from a position along one horizontal axis, verifies the obsidian frame and counts the portal blocks.
//NC has no MutableBlockPos, so every scan step builds a new BlockPos; arithmetic matches the vanilla offsets
public sealed class PortalShape
{
    private const int MinWidth = 2;
    public const int MaxWidth = 21;
    private const int MinHeight = 3;
    public const int MaxHeight = 21;
    private const float SafeTravelMaxEntityXY = 4f;

    private readonly Axis _axis;
    private readonly Direction _rightDir;
    private readonly int _numPortalBlocks;
    private readonly BlockPos _bottomLeft;
    private readonly int _height;
    private readonly int _width;

    private PortalShape(Axis axis, int portalBlockCount, Direction rightDir, BlockPos bottomLeft, int width, int height)
    {
        _axis = axis;
        _numPortalBlocks = portalBlockCount;
        _rightDir = rightDir;
        _bottomLeft = bottomLeft;
        _width = width;
        _height = height;
    }

    public Axis Axis => _axis;
    public Direction RightDir => _rightDir;
    public int NumPortalBlocks => _numPortalBlocks;
    public BlockPos BottomLeft => _bottomLeft;
    public int Height => _height;
    public int Width => _width;

    //FindEmptyPortalShape finds a valid frame with no portal blocks yet, maps to vanilla findEmptyPortalShape
    public static PortalShape? FindEmptyPortalShape(ServerLevel level, BlockPos pos, Axis preferredAxis)
        => FindPortalShape(level, pos, shape => shape.IsValid() && shape.NumPortalBlocks == 0, preferredAxis);

    //FindPortalShape tries the preferred axis first and then the other horizontal axis, maps to vanilla findPortalShape
    public static PortalShape? FindPortalShape(ServerLevel level, BlockPos pos, Func<PortalShape, bool> isValid,
        Axis preferredAxis)
    {
        var first = FindAnyShape(level, pos, preferredAxis);
        if (isValid(first)) return first;
        var otherAxis = preferredAxis == Axis.x ? Axis.z : Axis.x;
        var second = FindAnyShape(level, pos, otherAxis);
        return isValid(second) ? second : null;
    }

    //FindAnyShape scans the frame without validity filtering, maps to vanilla findAnyShape
    public static PortalShape FindAnyShape(ServerLevel level, BlockPos pos, Axis axis)
    {
        var rightDir = axis == Axis.x ? Direction.West : Direction.South;
        var bottomLeft = CalculateBottomLeft(level, rightDir, pos);
        if (bottomLeft is null) return new PortalShape(axis, 0, rightDir, pos, 0, 0);
        var width = CalculateWidth(level, bottomLeft.Value, rightDir);
        if (width == 0) return new PortalShape(axis, 0, rightDir, bottomLeft.Value, 0, 0);
        var portalBlockCount = 0;
        var height = CalculateHeight(level, bottomLeft.Value, rightDir, width, ref portalBlockCount);
        return new PortalShape(axis, portalBlockCount, rightDir, bottomLeft.Value, width, height);
    }

    //CalculateBottomLeft walks down to the frame floor and then left to the frame corner, maps to vanilla calculateBottomLeft
    private static BlockPos? CalculateBottomLeft(ServerLevel level, Direction rightDir, BlockPos pos)
    {
        var minY = Math.Max(level.MinBuildHeight, pos.Y - 21);
        while (pos.Y > minY && IsEmpty(GetState(level, pos.Offset(0, -1, 0)))) pos = pos.Offset(0, -1, 0);
        var leftDir = rightDir.Opposite;
        var edge = GetDistanceUntilEdgeAboveFrame(level, pos, leftDir) - 1;
        return edge < 0 ? null : pos.Relative(leftDir, edge);
    }

    //CalculateWidth measures the frame width along the right direction, maps to vanilla calculateWidth
    private static int CalculateWidth(ServerLevel level, BlockPos bottomLeft, Direction rightDir)
    {
        var width = GetDistanceUntilEdgeAboveFrame(level, bottomLeft, rightDir);
        return width is < MinWidth or > MaxWidth ? 0 : width;
    }

    //GetDistanceUntilEdgeAboveFrame measures to the frame edge above the floor, maps to the vanilla method
    private static int GetDistanceUntilEdgeAboveFrame(ServerLevel level, BlockPos pos, Direction direction)
    {
        for (var width = 0; width <= MaxWidth; width++)
        {
            var columnPos = pos.Relative(direction, width);
            var state = GetState(level, columnPos);
            if (!IsEmpty(state))
                return IsFrame(state) ? width : 0;
            if (!IsFrame(GetState(level, columnPos.Offset(0, -1, 0)))) return 0;
        }
        return 0;
    }

    //CalculateHeight measures the frame height and counts existing portal blocks, maps to vanilla calculateHeight
    private static int CalculateHeight(ServerLevel level, BlockPos bottomLeft, Direction rightDir, int width,
        ref int portalBlockCount)
    {
        var height = GetDistanceUntilTop(level, bottomLeft, rightDir, width, ref portalBlockCount);
        if (height is < MinHeight or > MaxHeight || !HasTopFrame(level, bottomLeft, rightDir, width, height))
            return 0;
        return height;
    }

    //HasTopFrame verifies the frame row above the opening, maps to vanilla hasTopFrame
    private static bool HasTopFrame(ServerLevel level, BlockPos bottomLeft, Direction rightDir, int width, int height)
    {
        for (var i = 0; i < width; i++)
        {
            var framePos = bottomLeft.Offset(0, height, 0).Relative(rightDir, i);
            if (!IsFrame(GetState(level, framePos))) return false;
        }
        return true;
    }

    //GetDistanceUntilTop walks upward while the two side columns and the inner cells stay valid, maps to vanilla getDistanceUntilTop
    private static int GetDistanceUntilTop(ServerLevel level, BlockPos bottomLeft, Direction rightDir, int width,
        ref int portalBlockCount)
    {
        for (var height = 0; height < MaxHeight; height++)
        {
            var leftPos = bottomLeft.Offset(0, height, 0).Relative(rightDir, -1);
            if (!IsFrame(GetState(level, leftPos))) return height;
            var rightPos = bottomLeft.Offset(0, height, 0).Relative(rightDir, width);
            if (!IsFrame(GetState(level, rightPos))) return height;
            for (var i = 0; i < width; i++)
            {
                var state = GetState(level, bottomLeft.Offset(0, height, 0).Relative(rightDir, i));
                if (!IsEmpty(state)) return height;
                if (IsPortal(state)) portalBlockCount++;
            }
        }
        return MaxHeight;
    }

    //IsEmpty whether a block may be part of the portal opening, maps to vanilla isEmpty
    private static bool IsEmpty(BlockState state)
        => state.IsAir || state.Is(BlockTags.Fire) || IsPortal(state);

    //IsFrame whether the state is the obsidian frame material, maps to vanilla FRAME
    private static bool IsFrame(BlockState state)
        => state.Owner.Id.Path == "obsidian";

    //IsPortal whether the state is a nether portal block
    private static bool IsPortal(BlockState state) => state.Owner.Id.Path == "nether_portal";

    //IsValid whether the measured opening is in the vanilla size range, maps to vanilla isValid
    public bool IsValid() => Width is >= MinWidth and <= MaxWidth && Height is >= MinHeight and <= MaxHeight;

    //CreatePortalBlocks fills the opening with portal blocks on this shape's axis, maps to vanilla createPortalBlocks
    public void CreatePortalBlocks(ServerLevel level)
    {
        var portalState = Blocks.NetherPortal.DefaultBlockState
            .SetValue(BlockStateProperties.AxisProperty, _axis);
        for (var width = 0; width < Width; width++)
        for (var height = 0; height < Height; height++)
        {
            var pos = _bottomLeft.Offset(0, height, 0).Relative(_rightDir, width);
            level.SetBlock(pos, portalState, BlockUpdateFlags.Clients | BlockUpdateFlags.KnownShape);
        }
    }

    //IsComplete whether the opening is valid and every cell already holds a portal block, maps to vanilla isComplete
    public bool IsComplete() => IsValid() && NumPortalBlocks == Width * Height;

    //GetRelativePosition maps a world position into the found rectangle, maps to vanilla getRelativePosition
    //The result is (along axis1, up, forward) each in 0..1; entity dimensions are passed as width/height because NC has no EntityDimensions type
    public static Vec3 GetRelativePosition(BlockUtil.FoundRectangle largestRectangleAround, Axis axis, Vec3 position,
        double entityWidth, double entityHeight)
    {
        var width = largestRectangleAround.Axis1Size - entityWidth;
        var height = largestRectangleAround.Axis2Size - entityHeight;
        var bottomMin = largestRectangleAround.MinCorner;
        double relativeRight;
        if (width > 0)
        {
            var bottomStart = GetComponent(bottomMin, axis) + entityWidth / 2.0;
            relativeRight = Mth.Clamp(Mth.InverseLerp(GetComponent(position, axis) - bottomStart, 0.0, width), 0.0, 1.0);
        }
        else
        {
            relativeRight = 0.5;
        }
        double relativeUp;
        if (height > 0)
        {
            relativeUp = Mth.Clamp(
                Mth.InverseLerp(GetComponent(position, Axis.y) - GetComponent(bottomMin, Axis.y), 0.0, height), 0.0, 1.0);
        }
        else
        {
            relativeUp = 0.0;
        }
        var forwardAxis = axis == Axis.x ? Axis.z : Axis.x;
        var relativeForward = GetComponent(position, forwardAxis) - (GetComponent(bottomMin, forwardAxis) + 0.5);
        return new Vec3(relativeRight, relativeUp, relativeForward);
    }

    //FindCollisionFreePosition nudges the landing position out of blocks, maps to vanilla findCollisionFreePosition
    //NC has no free-spot search on the level, so only the exact landing position is tested; when blocked the original position is kept
    public static Vec3 FindCollisionFreePosition(Vec3 bottomCenter, ServerLevel level, NacreCraft.Registry.Entity entity,
        double width, double height)
    {
        if (width > SafeTravelMaxEntityXY || height > SafeTravelMaxEntityXY) return bottomCenter;
        CollisionGetter collision = new LevelCollisionGetter(level, level.MinBuildHeight >> 4,
            (level.MaxBuildHeight - level.MinBuildHeight) >> 4);
        var box = new AABB(bottomCenter.X - width / 2.0, bottomCenter.Y, bottomCenter.Z - width / 2.0,
            bottomCenter.X + width / 2.0, bottomCenter.Y + height, bottomCenter.Z + width / 2.0);
        return collision.NoCollision(entity, box) ? bottomCenter : bottomCenter;
    }

    private static BlockState GetState(ServerLevel level, BlockPos pos)
        => level.GetBlockState(pos) ?? Blocks.AIR.DefaultBlockState;

    private static double GetComponent(BlockPos pos, Axis axis)
        => axis == Axis.x ? pos.X : axis == Axis.y ? pos.Y : pos.Z;

    private static double GetComponent(Vec3 pos, Axis axis)
        => axis == Axis.x ? pos.X : axis == Axis.y ? pos.Y : pos.Z;
}
