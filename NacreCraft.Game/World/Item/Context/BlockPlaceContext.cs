using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.Context;

//BlockPlaceContext one block placement attempt, maps to vanilla net.minecraft.world.item.context.BlockPlaceContext
public class BlockPlaceContext : UseOnContext
{
    private readonly BlockPos _relativePos;
    protected bool ReplaceClicked = true;

    public BlockPlaceContext(Player player, InteractionHand hand, ItemStack itemInHand, BlockHitResult hitResult)
        : this(player.Level as ServerLevel
                   ?? throw new InvalidOperationException("Player has no server level"),
            player, hand, itemInHand, hitResult)
    {
    }

    public BlockPlaceContext(UseOnContext context)
        : this(context.GetLevel(), context.GetPlayer(), context.GetHand(), context.GetItemInHand(),
            context.GetHitResult())
    {
    }

    protected BlockPlaceContext(ServerLevel level, Player? player, InteractionHand hand, ItemStack itemStackInHand,
        BlockHitResult hitResult)
        : base(level, player, hand, itemStackInHand, hitResult)
    {
        _relativePos = hitResult.BlockPos.Offset(hitResult.Direction);
        ReplaceClicked = CanBeReplaced(level, hitResult.BlockPos);
    }

    //At builds a context aimed at one block and direction, maps to vanilla at
    public static BlockPlaceContext At(BlockPlaceContext context, BlockPos pos, Direction direction)
        => new(context.GetLevel(), context.GetPlayer(), context.GetHand(), context.GetItemInHand(),
            new BlockHitResult(pos, direction,
                new Vec3(pos.X + 0.5 + direction.StepX * 0.5, pos.Y + 0.5 + direction.StepY * 0.5,
                    pos.Z + 0.5 + direction.StepZ * 0.5), false));

    public override BlockPos GetClickedPos() => ReplaceClicked ? base.GetClickedPos() : _relativePos;

    public virtual bool CanPlace()
        => ReplaceClicked || GetLevel().GetBlockState(GetClickedPos()) is not { } state
            || CanBeReplaced(GetLevel(), GetClickedPos());

    public virtual bool ReplacingClickedOnBlock() => ReplaceClicked;

    public virtual Direction GetNearestLookingDirection()
        => GetPlayer() is { } player
            ? Direction.OrderedByNearest(player.XRot, player.YRot)[0]
            : Direction.Up;

    public virtual Direction GetNearestLookingVerticalDirection()
        => GetPlayer() is { } player
            ? Direction.GetFacingAxis(player.XRot, player.YRot, Direction.Axis.Y)
            : Direction.Up;

    public virtual Direction[] GetNearestLookingDirections()
    {
        var directions = GetPlayer() is { } player
            ? Direction.OrderedByNearest(player.XRot, player.YRot)
            : Direction.Values.ToArray();
        if (ReplaceClicked) return directions;
        var clickedFace = GetClickedFace();
        var index = 0;
        while (index < directions.Length && directions[index] != clickedFace.Opposite) index++;
        if (index > 0)
        {
            Array.Copy(directions, 0, directions, 1, index);
            directions[0] = clickedFace.Opposite;
        }
        return directions;
    }

    //CanBeReplaced whether the clicked block can be replaced by the held item
    //The vanilla overload takes the context; NC block behavior only exposes the context-free flag
    private static bool CanBeReplaced(ServerLevel level, BlockPos pos)
        => level.GetBlockState(pos) is not { } state
            || state.Owner is BlockBehaviour behaviour && behaviour.CanBeReplaced;
}
