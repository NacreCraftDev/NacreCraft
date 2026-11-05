using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.Server.JsonRpc;

//ParamInfo one RPC parameter, maps to vanilla net.minecraft.server.jsonrpc.api.ParamInfo
public sealed record ParamInfo(string Name, SchemaInfo Schema, bool Required)
{
    public static readonly MapCodec<ParamInfo> MapCodec = RecordCodecBuilder.Of3(
        Codecs.String.FieldOf("name").ForGetter((ParamInfo info) => info.Name),
        SchemaInfo.Codec.FieldOf("schema").ForGetter((ParamInfo info) => info.Schema),
        Codecs.Bool.FieldOf("required").ForGetter((ParamInfo info) => info.Required),
        (name, schema, required) => new ParamInfo(name, schema, required));
}

//ResultInfo the RPC result description, maps to vanilla net.minecraft.server.jsonrpc.api.ResultInfo
public sealed record ResultInfo(string Name, SchemaInfo Schema)
{
    public static readonly MapCodec<ResultInfo> MapCodec = RecordCodecBuilder.Of2(
        Codecs.String.FieldOf("name").ForGetter((ResultInfo info) => info.Name),
        SchemaInfo.Codec.FieldOf("schema").ForGetter((ResultInfo info) => info.Schema),
        (name, schema) => new ResultInfo(name, schema));
}

//MethodInfo one RPC method description, maps to vanilla net.minecraft.server.jsonrpc.api.MethodInfo
public sealed record MethodInfo(string Description, Optional<ParamInfo> Params, Optional<ResultInfo> Result)
{
    public static readonly MapCodec<MethodInfo> MapCodec = RecordCodecBuilder.Of3(
        Codecs.String.FieldOf("description").ForGetter((MethodInfo info) => info.Description),
        Codecs.AsCodec(ParamInfo.MapCodec).OptionalFieldOf("params").ForGetter((MethodInfo info) => info.Params),
        Codecs.AsCodec(ResultInfo.MapCodec).OptionalFieldOf("result").ForGetter((MethodInfo info) => info.Result),
        (description, parameters, result) => new MethodInfo(description, parameters, result));

    public NamedMethodInfo Named(Identifier name) => new(name, this);
}

//NamedMethodInfo a method with its registry name, maps to vanilla MethodInfo.Named
public sealed record NamedMethodInfo(Identifier Name, MethodInfo Contents)
{
    public static readonly Codec<NamedMethodInfo> Codec = RecordCodecBuilder.Of2(
        IdentifierCodec.Instance.FieldOf("name").ForGetter((NamedMethodInfo info) => info.Name),
        MethodInfo.MapCodec.ForGetter((NamedMethodInfo info) => info.Contents),
        (name, contents) => new NamedMethodInfo(name, contents));
}
