using NacreCraft.Codec;
using NacreCraft.Network;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Component;

//ItemComponentCodecs shared codecs for item components, standing in for the vanilla Identifier.CODEC/STREAM_CODEC
internal static class ItemComponentCodecs
{
    //IdentifierCodec encodes namespace:path, maps to vanilla Identifier.CODEC
    public static readonly Codec<Identifier> IdentifierCodec = Codecs.String.ComapFlatMap(
        value => Identifier.TryParse(value) is { } identifier
            ? DataResult<Identifier>.Success(identifier)
            : DataResult<Identifier>.Error(() => $"Invalid identifier '{value}'"),
        identifier => identifier.ToString());

    //IdentifierStreamCodec writes namespace:path, maps to vanilla Identifier.STREAM_CODEC
    public static readonly StreamCodec<FriendlyByteBuf, Identifier> IdentifierStreamCodec =
        StreamCodecs.String<FriendlyByteBuf>(buf => buf.ReadIdentifier().ToString(),
                (buf, value) => buf.WriteIdentifier(Identifier.Parse(value)))
            .Map(Identifier.Parse, identifier => identifier.ToString());
}
