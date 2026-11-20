using NacreCraft.Codec;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Updates;
using NacreCraft.Util;
//The block state axis property uses the registry enum (lowercase x/y/z), not the direction axis enum
using Axis = NacreCraft.Registry.Enums.Axis;

namespace NacreCraft.Game.World.Level.Portal;

//PortalForcer finds or builds a nether portal on the destination side, maps to vanilla net.minecraft.world.level.portal.PortalForcer
//NC has no point-of-interest square query, so the closest-portal lookup degrades to empty and the placement search scans a
//16-block square ordered by distance instead of the vanilla spiral; the frame geometry and block flags match vanilla
public sealed class PortalForcer
{
    public const int TicketRadius = 3;
    private const int NetherPortalRadius = 16;
    private const int OverworldPortalRadius = 128;
    private const int FrameHeight = 5;
    private const int FrameWidth = 4;

    private readonly ServerLevel _level;

    public PortalForcer(ServerLevel level) => _level = level;

    //FindClosestPortalPosition looks for an existing portal in the destination area, maps to vanilla findClosestPortalPosition
    //Degraded: the POI manager has no square/tag query yet, so an existing portal is never found and the caller builds a new one
    public Optional<BlockPos> FindClosestPortalPosition(BlockPos approximateExitPos, bool toNether, WorldBorder worldBorder)
    {
        _ = toNether ? NetherPortalRadius : OverworldPortalRadius;
        _ = approximateExitPos;
        _ = worldBorder;
        return Optional<BlockPos>.Empty();
    }

    //CreatePortal finds a free spot near the origin, builds the obsidian frame and fills the portal, maps to vanilla createPortal
    public Optional<BlockUtil.FoundRectangle> CreatePortal(BlockPos origin, Axis portalAxis)
    {
        var direction = portalAxis == Axis.x ? Direction.East : Direction.South;
        var worldBorder = _level.WorldBorder;
        var maxPlaceableY = _level.MaxBuildHeight - 1;
        var minY = _level.MinBuildHeight;

        double closestFullDistanceSqr = -1.0;
        BlockPos? closestFullPosition = null;
        double closestPartialDistanceSqr = -1.0;
        BlockPos? closestPartialPosition = null;

        foreach (var columnPos in SquareAround(origin, NetherPortalRadius))
        {
            if (!worldBorder.IsWithinBounds(columnPos) || !worldBorder.IsWithinBounds(columnPos.Relative(direction, 1)))
                continue;
            var y = Math.Clamp(origin.Y, minY + 1, maxPlaceableY);
            while (y >= minY)
            {
                if (CanPortalReplaceBlock(new BlockPos(columnPos.X, y, columnPos.Z)))
                {
                    var firstEmptyY = y;
                    while (y > minY && CanPortalReplaceBlock(new BlockPos(columnPos.X, y - 1, columnPos.Z))) y--;
                    var deltaY = firstEmptyY - y;
                    if (y + 4 <= maxPlaceableY && (deltaY <= 0 || deltaY >= 3))
                    {
                        var basePos = new BlockPos(columnPos.X, y, columnPos.Z);
                        if (CanHostFrame(basePos, direction, 0))
                        {
                            var distance = origin.AsVec3i().DistSqr(basePos.AsVec3i());
                            if (CanHostFrame(basePos, direction, -1) && CanHostFrame(basePos, direction, 1)
                                && (closestFullDistanceSqr == -1.0 || closestFullDistanceSqr > distance))
                            {
                                closestFullDistanceSqr = distance;
                                closestFullPosition = basePos;
                            }
                            if (closestFullDistanceSqr == -1.0
                                && (closestPartialDistanceSqr == -1.0 || closestPartialDistanceSqr > distance))
                            {
                                closestPartialDistanceSqr = distance;
                                closestPartialPosition = basePos;
                            }
                        }
                    }
                }
                y--;
            }
        }

        if (closestFullDistanceSqr == -1.0 && closestPartialDistanceSqr != -1.0)
        {
            closestFullPosition = closestPartialPosition;
            closestFullDistanceSqr = closestPartialDistanceSqr;
        }

        if (closestFullDistanceSqr == -1.0)
        {
            //No natural spot: build the vanilla emergency platform near the origin
            var minStartY = Math.Max(minY + 1, 70);
            var maxStartY = maxPlaceableY - 9;
            if (maxStartY < minStartY) return Optional<BlockUtil.FoundRectangle>.Empty();
            var platformPos = worldBorder.ClampToBounds(new BlockPos(
                origin.X - direction.StepX, Math.Clamp(origin.Y, minStartY, maxStartY), origin.Z - direction.StepZ));
            closestFullPosition = platformPos;
            var clockWise = direction.ClockWise;
            for (var box = -1; box < 2; box++)
            for (var width = 0; width < 2; width++)
            for (var height = -1; height < 3; height++)
            {
                var pos = platformPos.Offset(width * direction.StepX + box * clockWise.StepX, height,
                    width * direction.StepZ + box * clockWise.StepZ);
                _level.SetBlock(pos, height < 0 ? Blocks.Obsidian.DefaultBlockState : Blocks.AIR.DefaultBlockState);
            }
        }

        var frameOrigin = closestFullPosition!.Value;
        //Obsidian frame: the two side columns plus the top and bottom rows
        for (var width = -1; width < 3; width++)
        for (var height = -1; height < 4; height++)
        {
            if (width != -1 && width != 2 && height != -1 && height != 3) continue;
            var pos = frameOrigin.Offset(width * direction.StepX, height, width * direction.StepZ);
            _level.SetBlock(pos, Blocks.Obsidian.DefaultBlockState, BlockUpdateFlags.Clients | BlockUpdateFlags.Neighbours);
        }
        var portalBlockState = Blocks.NetherPortal.DefaultBlockState
            .SetValue(BlockStateProperties.AxisProperty, portalAxis);
        for (var width = 0; width < 2; width++)
        for (var height = 0; height < 3; height++)
        {
            var pos = frameOrigin.Offset(width * direction.StepX, height, width * direction.StepZ);
            _level.SetBlock(pos, portalBlockState, BlockUpdateFlags.Clients | BlockUpdateFlags.KnownShape);
        }
        return Optional<BlockUtil.FoundRectangle>.Of(new BlockUtil.FoundRectangle(frameOrigin, 2, 3));
    }

    //CanPortalReplaceBlock whether a cell may be replaced by the portal, maps to vanilla canPortalReplaceBlock
    private bool CanPortalReplaceBlock(BlockPos pos)
    {
        var state = _level.GetBlockState(pos);
        if (state is not { } blockState) return false;
        return blockState.Owner is BlockBehaviour behaviour && behaviour.CanBeReplaced && blockState.FluidState.IsEmpty;
    }

    //CanHostFrame whether a 4x5 frame can be placed with the given sideways offset, maps to vanilla canHostFrame
    //The solid floor check uses "not air" as the NC stand-in for the vanilla motion-blocking test
    private bool CanHostFrame(BlockPos origin, Direction direction, int offset)
    {
        var clockWise = direction.ClockWise;
        for (var width = -1; width < 3; width++)
        for (var height = -1; height < 4; height++)
        {
            var pos = origin.Offset(direction.StepX * width + clockWise.StepX * offset, height,
                direction.StepZ * width + clockWise.StepZ * offset);
            var state = _level.GetBlockState(pos);
            if (height < 0)
            {
                if (state is null || state.Value.IsAir) return false;
            }
            else if (!CanPortalReplaceBlock(pos))
            {
                return false;
            }
        }
        return true;
    }

    //SquareAround yields positions in a growing square around the origin ordered by squared distance
    //The vanilla spiral order is not preserved; NC degrades to a distance-ordered scan and records that in the leftovers
    private static IEnumerable<BlockPos> SquareAround(BlockPos origin, int radius)
    {
        var positions = new List<BlockPos>((radius * 2 + 1) * (radius * 2 + 1));
        for (var dx = -radius; dx <= radius; dx++)
        for (var dz = -radius; dz <= radius; dz++)
            positions.Add(origin.Offset(dx, 0, dz));
        positions.Sort((a, b) => origin.AsVec3i().DistSqr(a.AsVec3i()).CompareTo(origin.AsVec3i().DistSqr(b.AsVec3i())));
        return positions;
    }
}
