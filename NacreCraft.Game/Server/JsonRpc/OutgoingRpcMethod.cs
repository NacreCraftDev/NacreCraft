using System.Text.Json.Nodes;
using NacreCraft.Codec;

namespace NacreCraft.Game.Server.JsonRpc;

//OutgoingRpcMethod an RPC method the client calls on us, maps to vanilla net.minecraft.server.jsonrpc.OutgoingRpcMethod
//encodeParams writes the parameters into the request and decodeResult reads a response; notifications decode nothing
public interface OutgoingRpcMethod<Params, Result> : IOutgoingRpcMethod, IOutgoingRpcResultDecoder<Result>
{
    //NotificationPrefix method name prefix shared by every server-to-client notification
    public const string NotificationPrefix = "notification/";

    //EncodeParams builds the params array element, maps to vanilla encodeParams
    JsonNode? EncodeParams(Params parameters);
}

//IOutgoingRpcResultDecoder decodes a response into the typed result, splitting the variance off so pending requests can hold it
public interface IOutgoingRpcResultDecoder<out Result>
{
    Result DecodeResult(JsonNode? result);
}

//OutgoingRpcMethodHolder the common typed shape of a resolved outgoing method plus its registry name
public interface IOutgoingRpcMethodHolder<out Result>
{
    NacreCraft.Registry.Identifier Name { get; }

    IOutgoingRpcResultDecoder<Result> Method { get; }
}

//OutgoingRpcMethodAttributes attributes of an outgoing method, maps to vanilla OutgoingRpcMethod.Attributes
public sealed record OutgoingRpcMethodAttributes(bool Discoverable, bool AllowPreServerInit)
{
    public static readonly OutgoingRpcMethodAttributes Default = new(true, false);
}

//ParameterlessNotification a notification with no parameters, maps to vanilla ParmeterlessNotification
public sealed class ParameterlessNotification(MethodInfo info, OutgoingRpcMethodAttributes attributes)
    : OutgoingRpcMethod<object, object?>
{
    public MethodInfo Info() => info;

    public RpcMethodAttributes Attributes { get; } = new(attributes.Discoverable, false, attributes.AllowPreServerInit);

    public JsonNode? EncodeParams(object parameters) => null;

    public object? DecodeResult(JsonNode? result) => null;
}

//Notification a notification with parameters, maps to vanilla Notification
public sealed class Notification<Params>(MethodInfo info, OutgoingRpcMethodAttributes attributes,
    Schema<Params> paramSchema) : OutgoingRpcMethod<Params, object?>
{
    public MethodInfo Info() => info;

    public RpcMethodAttributes Attributes { get; } = new(attributes.Discoverable, false, attributes.AllowPreServerInit);

    //EncodeParams encodes the parameter value with the schema codec, maps to vanilla encodeParams
    public JsonNode? EncodeParams(Params parameters)
        => paramSchema.ValueCodec.EncodeStart(JsonOps.Instance, parameters)
            .GetOrThrow(reason => new EncodeJsonRpcException(reason));

    public object? DecodeResult(JsonNode? result) => null;
}

//ParameterlessMethod a request without parameters, maps to vanilla ParameterlessMethod
public sealed class ParameterlessOutgoingMethod<Result>(MethodInfo info, OutgoingRpcMethodAttributes attributes,
    Schema<Result> resultSchema) : OutgoingRpcMethod<object, Result>
{
    public MethodInfo Info() => info;

    public RpcMethodAttributes Attributes { get; } = new(attributes.Discoverable, false, attributes.AllowPreServerInit);

    public JsonNode? EncodeParams(object parameters) => null;

    //DecodeResult decodes the response with the schema codec, maps to vanilla decodeResult
    public Result DecodeResult(JsonNode? result)
        => resultSchema.ValueCodec.Parse(JsonOps.Instance, result)
            .GetOrThrow(reason => new InvalidParameterJsonRpcException(reason));
}

//OutgoingMethod a request with parameters and a result, maps to vanilla Method
public sealed class OutgoingMethod<Params, Result>(MethodInfo info, OutgoingRpcMethodAttributes attributes,
    Schema<Params> paramSchema, Schema<Result> resultSchema) : OutgoingRpcMethod<Params, Result>
{
    public MethodInfo Info() => info;

    public RpcMethodAttributes Attributes { get; } = new(attributes.Discoverable, false, attributes.AllowPreServerInit);

    public JsonNode? EncodeParams(Params parameters)
        => paramSchema.ValueCodec.EncodeStart(JsonOps.Instance, parameters)
            .GetOrThrow(reason => new EncodeJsonRpcException(reason));

    public Result DecodeResult(JsonNode? result)
        => resultSchema.ValueCodec.Parse(JsonOps.Instance, result)
            .GetOrThrow(reason => new InvalidParameterJsonRpcException(reason));
}

//OutgoingRpcMethodBuilder builder for outgoing methods, maps to vanilla OutgoingRpcMethodBuilder
public sealed class OutgoingRpcMethodBuilder<Params, Result>
{
    //Factory builds the concrete method once the schemas are known
    public delegate OutgoingRpcMethod<Params, Result> Factory(MethodInfo info, OutgoingRpcMethodAttributes attributes,
        Schema<Params>? paramSchema, Schema<Result>? resultSchema);

    private readonly Factory _factory;
    private string _description = "";
    private Schema<Params>? _paramSchema;
    private string _paramName = "params";
    private Schema<Result>? _resultSchema;
    private string _resultName = "result";
    private bool _discoverable = true;
    private bool _allowPreServerInit;

    public OutgoingRpcMethodBuilder(Factory factory) => _factory = factory;

    public OutgoingRpcMethodBuilder<Params, Result> Description(string description)
    {
        _description = description;
        return this;
    }

    public OutgoingRpcMethodBuilder<Params, Result> Param(string name, Schema<Params> schema)
    {
        _paramName = name;
        _paramSchema = schema;
        return this;
    }

    public OutgoingRpcMethodBuilder<Params, Result> Response(string name, Schema<Result> schema)
    {
        _resultName = name;
        _resultSchema = schema;
        return this;
    }

    public OutgoingRpcMethodBuilder<Params, Result> Undiscoverable()
    {
        _discoverable = false;
        return this;
    }

    public OutgoingRpcMethodBuilder<Params, Result> AllowPreServerInit()
    {
        _allowPreServerInit = true;
        return this;
    }

    public OutgoingRpcMethod<Params, Result> Build()
    {
        var attributes = new OutgoingRpcMethodAttributes(_discoverable, _allowPreServerInit);
        var paramInfo = Optional<ParamInfo>.Empty();
        if (_paramSchema is not null) paramInfo = Optional<ParamInfo>.Of(new ParamInfo(_paramName, _paramSchema.Info(), true));
        var resultInfo = Optional<ResultInfo>.Empty();
        if (_resultSchema is not null) resultInfo = Optional<ResultInfo>.Of(new ResultInfo(_resultName, _resultSchema.Info()));
        var info = new MethodInfo(_description, paramInfo, resultInfo);
        return _factory(info, attributes, _paramSchema, _resultSchema);
    }

    //Register registers the built method into the outgoing registry, maps to vanilla register
    public OutgoingRpcMethod<Params, Result> Register(NacreCraft.Registry.Registry<NacreCraft.Registry.OutgoingRpcMethod<object, object>> registry, string key)
    {
        var method = Build();
        NacreCraft.Registry.Registry<NacreCraft.Registry.OutgoingRpcMethod<object, object>>.Register(registry, key, method);
        return method;
    }

    //RegisterForHolder registers and returns the holder, which is what the notification fields keep
    public NacreCraft.Registry.Reference<NacreCraft.Registry.OutgoingRpcMethod<object, object>> RegisterForHolder(
        NacreCraft.Registry.Registry<NacreCraft.Registry.OutgoingRpcMethod<object, object>> registry, string key)
    {
        var method = Build();
        return NacreCraft.Registry.Registry<NacreCraft.Registry.OutgoingRpcMethod<object, object>>.RegisterForHolder(
            registry, NacreCraft.Registry.Identifier.WithDefaultNamespace(key), method);
    }

    //Notification a notification with no parameters, maps to vanilla OutgoingRpcMethod.notification
    public static OutgoingRpcMethodBuilder<object, object?> Notification()
        => new((info, attributes, _, _) => new ParameterlessNotification(info, attributes));

    public static OutgoingRpcMethodBuilder<Params, object?> NotificationWithParams()
        => new((info, attributes, paramSchema, _) => new Notification<Params>(info, attributes,
            paramSchema ?? throw new InvalidOperationException("No param schema defined")));

    public static OutgoingRpcMethodBuilder<object, Result> Request()
        => new((info, attributes, _, resultSchema) => new ParameterlessOutgoingMethod<Result>(info, attributes,
            resultSchema ?? throw new InvalidOperationException("No result schema defined")));

    public static OutgoingRpcMethodBuilder<Params, Result> RequestWithParams()
        => new((info, attributes, paramSchema, resultSchema) => new OutgoingMethod<Params, Result>(info, attributes,
            paramSchema ?? throw new InvalidOperationException("No param schema defined"),
            resultSchema ?? throw new InvalidOperationException("No result schema defined")));
}
