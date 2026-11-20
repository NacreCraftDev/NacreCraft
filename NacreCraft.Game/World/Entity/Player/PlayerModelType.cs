using NacreCraft.Codec;
using NacreCraft.Network;

namespace NacreCraft.Game.World.Entity;

//PlayerModelType player skin model, maps to vanilla net.minecraft.world.entity.player.PlayerModelType
//SLIM is the 3-pixel-arm model, WIDE the classic 4-pixel-arm model; the wire form is a single boolean
public enum PlayerModelType
{
    SLIM,
    WIDE
}

//PlayerModelTypeExtensions codecs and lookups, maps to the vanilla CODEC/STREAM_CODEC/byLegacyServicesName
public static class PlayerModelTypeExtensions
{
    private const string DefaultLegacyServicesId = "wide";

    //Codec the serialized name is "slim"/"wide", maps to vanilla CODEC
    public static readonly Codec<PlayerModelType> Codec = Codecs.String.ComapFlatMap(
        name => TryByName(name) is { } model
            ? DataResult<PlayerModelType>.Success(model)
            : DataResult<PlayerModelType>.Error(() => $"Unknown player model '{name}'"),
        GetSerializedName);

    //StreamCodec one boolean, true is SLIM, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<FriendlyByteBuf, PlayerModelType> StreamCodec =
        ByteBufCodecs.Bool.Map(slim => slim ? PlayerModelType.SLIM : PlayerModelType.WIDE,
            model => model == PlayerModelType.SLIM);

    public static string GetSerializedName(this PlayerModelType model)
        => model == PlayerModelType.SLIM ? "slim" : "wide";

    //ByLegacyServicesName looks up by the services id, unknown names fall back to WIDE, maps to vanilla byLegacyServicesName
    public static PlayerModelType ByLegacyServicesName(string? name) => name switch
    {
        "slim" => PlayerModelType.SLIM,
        "wide" or DefaultLegacyServicesId => PlayerModelType.WIDE,
        _ => PlayerModelType.WIDE
    };

    private static PlayerModelType? TryByName(string name) => name switch
    {
        "slim" => PlayerModelType.SLIM,
        "wide" => PlayerModelType.WIDE,
        _ => null
    };
}
