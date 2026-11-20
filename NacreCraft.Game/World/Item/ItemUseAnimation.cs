using NacreCraft.Codec;
using NacreCraft.Network;

namespace NacreCraft.Game.World.Items;

//ItemUseAnimation the third-person/first-person animation played while an item is used, maps to vanilla net.minecraft.world.item.ItemUseAnimation
public enum ItemUseAnimation
{
    None = 0,
    Eat = 1,
    Drink = 2,
    Block = 3,
    Bow = 4,
    Trident = 5,
    Crossbow = 6,
    Spyglass = 7,
    TootHorn = 8,
    Brush = 9,
    Bundle = 10,
    Spear = 11,
}

//ItemUseAnimationExtensions codecs and metadata, maps to vanilla CODEC/STREAM_CODEC/hasCustomArmTransform
public static class ItemUseAnimationExtensions
{
    public static readonly Codec<ItemUseAnimation> Codec = Codecs.String.ComapFlatMap(
        name => TryByName(name) is { } animation
            ? DataResult<ItemUseAnimation>.Success(animation)
            : DataResult<ItemUseAnimation>.Error(() => $"Unknown item use animation '{name}'"),
        GetSerializedName);

    public static readonly StreamCodec<FriendlyByteBuf, ItemUseAnimation> StreamCodec =
        ByteBufCodecs.VarInt.Map(ById, value => (int)value);

    public static int GetId(this ItemUseAnimation animation) => (int)animation;

    public static string GetSerializedName(this ItemUseAnimation animation) => animation switch
    {
        ItemUseAnimation.None => "none",
        ItemUseAnimation.Eat => "eat",
        ItemUseAnimation.Drink => "drink",
        ItemUseAnimation.Block => "block",
        ItemUseAnimation.Bow => "bow",
        ItemUseAnimation.Trident => "trident",
        ItemUseAnimation.Crossbow => "crossbow",
        ItemUseAnimation.Spyglass => "spyglass",
        ItemUseAnimation.TootHorn => "toot_horn",
        ItemUseAnimation.Brush => "brush",
        ItemUseAnimation.Bundle => "bundle",
        ItemUseAnimation.Spear => "spear",
        _ => "none",
    };

    //HasCustomArmTransform whether the animation uses a special arm transform, maps to vanilla hasCustomArmTransform
    public static bool HasCustomArmTransform(this ItemUseAnimation animation)
        => animation is ItemUseAnimation.Eat or ItemUseAnimation.Drink or ItemUseAnimation.Spear;

    public static ItemUseAnimation ById(int id) => id is >= 0 and <= 11 ? (ItemUseAnimation)id : ItemUseAnimation.None;

    private static ItemUseAnimation? TryByName(string name) => name switch
    {
        "none" => ItemUseAnimation.None,
        "eat" => ItemUseAnimation.Eat,
        "drink" => ItemUseAnimation.Drink,
        "block" => ItemUseAnimation.Block,
        "bow" => ItemUseAnimation.Bow,
        "trident" => ItemUseAnimation.Trident,
        _ when name == "crossbow" => ItemUseAnimation.Crossbow,
        "spyglass" => ItemUseAnimation.Spyglass,
        "toot_horn" => ItemUseAnimation.TootHorn,
        "brush" => ItemUseAnimation.Brush,
        "bundle" => ItemUseAnimation.Bundle,
        "spear" => ItemUseAnimation.Spear,
        _ => null,
    };
}
