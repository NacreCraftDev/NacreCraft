using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Items.Context;

//UseOnContext one item use against a block, maps to vanilla net.minecraft.world.item.context.UseOnContext
public class UseOnContext
{
    private readonly Player? _player;
    private readonly InteractionHand _hand;
    private readonly BlockHitResult _hitResult;
    private readonly ServerLevel _level;
    private readonly ItemStack _itemStack;

    public UseOnContext(Player player, InteractionHand hand, BlockHitResult hitResult)
        : this(player.Level as ServerLevel
                   ?? throw new InvalidOperationException("Player has no server level"),
            player, hand, GetItemInHand(player, hand), hitResult)
    {
    }

    protected UseOnContext(ServerLevel level, Player? player, InteractionHand hand, ItemStack itemStack,
        BlockHitResult hitResult)
    {
        _player = player;
        _hand = hand;
        _hitResult = hitResult;
        _itemStack = itemStack;
        _level = level;
    }

    public BlockHitResult GetHitResult() => _hitResult;

    public virtual BlockPos GetClickedPos() => _hitResult.BlockPos;

    public Direction GetClickedFace() => _hitResult.Direction;

    public Vec3 GetClickLocation() => _hitResult.Location;

    public bool IsInside() => _hitResult.Inside;

    public ItemStack GetItemInHand() => _itemStack;

    public Player? GetPlayer() => _player;

    public InteractionHand GetHand() => _hand;

    public ServerLevel GetLevel() => _level;

    public virtual Direction GetHorizontalDirection()
        => _player is null ? Direction.North : Direction.FromYRot(_player.YRot);

    public virtual bool IsSecondaryUseActive() => _player is { IsCrouching: true };

    public virtual float GetRotation() => _player?.YRot ?? 0.0f;

    //GetItemInHand reads the stack in the given hand, maps to vanilla Player.getItemInHand
    private static ItemStack GetItemInHand(Player player, InteractionHand hand)
        => hand == InteractionHand.MainHand ? player.MainHandItem : player.OffhandItem;
}
