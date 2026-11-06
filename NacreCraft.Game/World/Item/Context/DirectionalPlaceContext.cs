using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.Context;

//DirectionalPlaceContext a placement driven by a dispenser direction, maps to vanilla net.minecraft.world.item.context.DirectionalPlaceContext
public sealed class DirectionalPlaceContext : BlockPlaceContext
{
    private readonly Direction _direction;

    public DirectionalPlaceContext(ServerLevel level, BlockPos pos, Direction direction, ItemStack dispensed,
        Direction clickedFace)
        : base(level, null, InteractionHand.MainHand, dispensed,
            new BlockHitResult(pos, clickedFace, Vec3.AtBottomCenterOf(pos), false))
    {
        _direction = direction;
    }

    public override BlockPos GetClickedPos() => GetHitResult().BlockPos;

    public override bool CanPlace()
        => GetLevel().GetBlockState(GetHitResult().BlockPos) is not { } state
            || state.Owner is BlockBehaviour behaviour && behaviour.CanBeReplaced;

    public override bool ReplacingClickedOnBlock() => CanPlace();

    public override Direction GetNearestLookingDirection() => Direction.Down;

    public override Direction[] GetNearestLookingDirections() => _direction.Id3D switch
    {
        Direction.NorthId => new[] { Direction.Down, Direction.North, Direction.East, Direction.West, Direction.Up, Direction.South },
        Direction.SouthId => new[] { Direction.Down, Direction.South, Direction.East, Direction.West, Direction.Up, Direction.North },
        Direction.WestId => new[] { Direction.Down, Direction.West, Direction.South, Direction.Up, Direction.North, Direction.East },
        Direction.EastId => new[] { Direction.Down, Direction.East, Direction.South, Direction.Up, Direction.North, Direction.West },
        Direction.UpId => new[] { Direction.Down, Direction.Up, Direction.North, Direction.East, Direction.South, Direction.West },
        _ => new[] { Direction.Down, Direction.North, Direction.East, Direction.South, Direction.West, Direction.Up }
    };

    public override Direction GetHorizontalDirection()
        => _direction.AxisValue == Direction.Axis.Y ? Direction.North : _direction;

    public override bool IsSecondaryUseActive() => false;

    public override float GetRotation() => _direction.Get2DDataValue * 90f;
}
