using NacreCraft.Codec;

namespace NacreCraft.Game.Server.JsonRpc;

//SchemaPresets the built-in OpenRPC schema components, maps to the static schema fields of vanilla Schema
//Only the components whose value types are already ported are registered; touching this type initializes them once
public static class SchemaPresets
{
    //An explicit static constructor removes beforefieldinit, so EnsureInitialized really runs the field initializers
    static SchemaPresets()
    {
    }

    private static readonly Codec<Guid> UuidCodec = Codecs.String.ComapFlatMap(
        text => Guid.TryParse(text, out var id)
            ? DataResult<Guid>.Success(id)
            : DataResult<Guid>.Error(() => $"Invalid UUID: {text}"),
        id => id.ToString());

    public static readonly Schema<bool> BOOL_SCHEMA = Schema<bool>.OfType("boolean", Codecs.Bool);
    public static readonly Schema<int> INT_SCHEMA = Schema<int>.OfType("integer", Codecs.Int);
    public static readonly Schema<float> NUMBER_SCHEMA = Schema<float>.OfType("number", Codecs.Float);
    public static readonly Schema<string> STRING_SCHEMA = Schema<string>.OfType("string", Codecs.String);
    public static readonly Schema<Guid> UUID_SCHEMA = Schema<Guid>.OfType("string", UuidCodec);

    public static readonly SchemaComponent PLAYER_SCHEMA = Schema<PlayerDto>.RegisterSchema("player",
        Schema<PlayerDto>.Record(Codecs.AsCodec(PlayerDto.CODEC))
            .WithField("id", UUID_SCHEMA)
            .WithField("name", STRING_SCHEMA));

    public static readonly SchemaComponent VERSION_SCHEMA = Schema<DiscoveryService.DiscoverInfo>.RegisterSchema("version",
        Schema<DiscoveryService.DiscoverInfo>.Record(DiscoveryService.DiscoverInfo.Codec)
            .WithField("name", STRING_SCHEMA)
            .WithField("protocol", INT_SCHEMA));

    public static readonly SchemaComponent MESSAGE_SCHEMA = Schema<Message>.RegisterSchema("message",
        Schema<Message>.Record(Message.CODEC)
            .WithField("literal", STRING_SCHEMA)
            .WithField("translatable", STRING_SCHEMA)
            .WithField("translatableParams", STRING_SCHEMA.AsArray()));

    //EnsureInitialized forces the static field initializers to run before the discovery document is built
    public static void EnsureInitialized()
    {
    }
}
