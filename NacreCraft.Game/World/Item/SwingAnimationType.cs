using NacreCraft.Codec;
using NacreCraft.Network;

namespace NacreCraft.Game.World.Items;

//SwingAnimationType which swing animation an item uses, maps to vanilla net.minecraft.world.item.SwingAnimationType
public enum SwingAnimationType
{
    None = 0,
    Whack = 1,
    Stab = 2,
}

//SwingAnimationTypeExtensions codecs, maps to vanilla CODEC/STREAM_CODEC
public static class SwingAnimationTypeExtensions
{
    public static readonly Codec<SwingAnimationType> Codec = Codecs.String.ComapFlatMap(
        name => name switch
        {
            "none" => DataResult<SwingAnimationType>.Success(SwingAnimationType.None),
            "whack" => DataResult<SwingAnimationType>.Success(SwingAnimationType.Whack),
            "stab" => DataResult<SwingAnimationType>.Success(SwingAnimationType.Stab),
            _ => DataResult<SwingAnimationType>.Error(() => $"Unknown swing animation '{name}'"),
        },
        GetSerializedName);

    public static readonly StreamCodec<FriendlyByteBuf, SwingAnimationType> StreamCodec =
        ByteBufCodecs.VarInt.Map(ById, value => (int)value);

    public static string GetSerializedName(this SwingAnimationType type) => type switch
    {
        SwingAnimationType.None => "none",
        SwingAnimationType.Whack => "whack",
        SwingAnimationType.Stab => "stab",
        _ => "whack",
    };

    public static SwingAnimationType ById(int id) => id is 0 or 1 or 2 ? (SwingAnimationType)id : SwingAnimationType.None;
}
