using NacreCraft.Codec;

namespace NacreCraft.Game.Server.JsonRpc;

//SchemaInfo the non-generic JSON schema shape written by the discovery document, maps to the info() view of vanilla Schema
public sealed record SchemaInfo(Optional<Uri> Reference, IReadOnlyList<string> Types, Optional<SchemaInfo> Items,
    IReadOnlyDictionary<string, SchemaInfo> Properties, IReadOnlyList<string> EnumValues)
{
    public static readonly Codec<SchemaInfo> Codec = new SchemaInfoCodec();

    //Encode into the ops element, used by the parent property map
    public DataResult<T> Encode<T>(DynamicOps<T> ops) => Codec.EncodeStart(ops, this);
}

//SchemaInfoCodec writes the OpenRPC schema shape; the document is generated, never read back
internal sealed class SchemaInfoCodec : AbstractMapCodec<SchemaInfo>
{
    public override DataResult<SchemaInfo> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
        => DataResult<SchemaInfo>.Error(() => "schema is write-only");

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, SchemaInfo value, RecordBuilder<U> builder)
    {
        if (value.Reference.IsPresent) builder.Add("$ref", ops.CreateString(value.Reference.Get().ToString()));
        if (value.Types.Count > 0) builder.Add("type", ops.CreateList(value.Types.Select(ops.CreateString)));
        if (value.Items.IsPresent) builder.Add("items", value.Items.Get().Encode(ops).GetOrThrow());
        if (value.Properties.Count > 0)
            builder.Add("properties", ops.CreateMap(value.Properties.Select(pair =>
                new Pair<U, U>(ops.CreateString(pair.Key), pair.Value.Encode(ops).GetOrThrow()))));
        if (value.EnumValues.Count > 0) builder.Add("enum", ops.CreateList(value.EnumValues.Select(ops.CreateString)));
        return builder;
    }
}
