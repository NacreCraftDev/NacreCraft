using System.Text.Json.Nodes;
using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;

namespace NacreCraft.Game.Server.JsonRpc;

//IncomingRpcMethod an RPC method the client calls, maps to vanilla net.minecraft.server.jsonrpc.IncomingRpcMethod
//apply decodes the params, runs the handler and encodes the result; a parameterless method takes no params
public interface IncomingRpcMethod<Params, Result> : IIncomingRpcMethod
{
    //Apply runs the method and returns the encoded result, maps to vanilla apply
    JsonNode? Apply(MinecraftApi api, JsonNode? paramsJson, ClientInfo clientInfo);

    //ParameterlessRpcMethodFunction handler with no parameters
    public delegate Result ParameterlessRpcMethodFunction(MinecraftApi api, ClientInfo clientInfo);

    //RpcMethodFunction handler with one parameter value
    public delegate Result RpcMethodFunction(MinecraftApi api, Params parameters, ClientInfo clientInfo);
}

//ParameterlessIncomingMethod a method with no parameters, maps to vanilla ParameterlessMethod
public sealed class ParameterlessIncomingMethod<Params, Result>(MethodInfo info, RpcMethodAttributes attributes,
    Schema<Result> resultSchema, IncomingRpcMethod<Params, Result>.ParameterlessRpcMethodFunction supplier)
    : IncomingRpcMethod<Params, Result>
{
    public MethodInfo Info() => info;

    public RpcMethodAttributes Attributes { get; } = attributes;

    public JsonNode? Apply(MinecraftApi api, JsonNode? paramsJson, ClientInfo clientInfo)
    {
        if (paramsJson is JsonArray { Count: > 0 })
            throw new InvalidParameterJsonRpcException("Expected no params, or an empty array");
        if (paramsJson is JsonObject)
            throw new InvalidParameterJsonRpcException("Expected no params, or an empty array");
        var result = supplier(api, clientInfo);
        return resultSchema.ValueCodec.EncodeStart(JsonOps.Instance, result)
            .GetOrThrow(reason => new EncodeJsonRpcException(reason));
    }
}

//IncomingMethod a method with one parameter, maps to vanilla Method
public sealed class IncomingMethod<Params, Result>(MethodInfo info, RpcMethodAttributes attributes,
    Schema<Params> paramSchema, Schema<Result> resultSchema, IncomingRpcMethod<Params, Result>.RpcMethodFunction function)
    : IncomingRpcMethod<Params, Result>
{
    public MethodInfo Info() => info;

    public RpcMethodAttributes Attributes { get; } = attributes;

    public JsonNode? Apply(MinecraftApi api, JsonNode? paramsJson, ClientInfo clientInfo)
    {
        if (paramsJson is not (JsonArray or JsonObject))
            throw new InvalidParameterJsonRpcException("Expected params as array or named");
        var paramName = info.Params.IsPresent ? info.Params.Get().Name : "params";
        JsonNode? paramJson;
        if (paramsJson is JsonObject obj)
        {
            if (!obj.TryGetPropertyValue(paramName, out paramJson) || paramJson is null)
                throw new InvalidParameterJsonRpcException(
                    $"Params passed by-name, but expected param [{paramName}] does not exist");
        }
        else
        {
            var array = (JsonArray)paramsJson!;
            if (array.Count != 1)
                throw new InvalidParameterJsonRpcException("Expected exactly one element in the params array");
            paramJson = array[0];
        }
        var parameters = paramSchema.ValueCodec.Parse(JsonOps.Instance, paramJson)
            .GetOrThrow(reason => new InvalidParameterJsonRpcException(reason));
        var result = function(api, parameters, clientInfo);
        return resultSchema.ValueCodec.EncodeStart(JsonOps.Instance, result)
            .GetOrThrow(reason => new EncodeJsonRpcException(reason));
    }
}

//IncomingRpcMethodBuilder builder for incoming methods, maps to vanilla IncomingRpcMethodBuilder
public sealed class IncomingRpcMethodBuilder<Params, Result>
{
    private readonly IncomingRpcMethod<Params, Result>.RpcMethodFunction? _function;
    private readonly IncomingRpcMethod<Params, Result>.ParameterlessRpcMethodFunction? _parameterlessFunction;
    private string _description = "";
    private Schema<Params>? _paramSchema;
    private string _paramName = "params";
    private Schema<Result>? _resultSchema;
    private string _resultName = "result";
    private bool _discoverable = true;
    private bool _runOnMainThread = true;
    private bool _allowPreServerInit;

    public IncomingRpcMethodBuilder(IncomingRpcMethod<Params, Result>.RpcMethodFunction function)
        => _function = function;

    public IncomingRpcMethodBuilder(IncomingRpcMethod<Params, Result>.ParameterlessRpcMethodFunction function)
        => _parameterlessFunction = function;

    public IncomingRpcMethodBuilder<Params, Result> Description(string description)
    {
        _description = description;
        return this;
    }

    public IncomingRpcMethodBuilder<Params, Result> Param(string name, Schema<Params> schema)
    {
        _paramName = name;
        _paramSchema = schema;
        return this;
    }

    public IncomingRpcMethodBuilder<Params, Result> Response(string name, Schema<Result> schema)
    {
        _resultName = name;
        _resultSchema = schema;
        return this;
    }

    public IncomingRpcMethodBuilder<Params, Result> Undiscoverable()
    {
        _discoverable = false;
        return this;
    }

    public IncomingRpcMethodBuilder<Params, Result> NotOnMainThread()
    {
        _runOnMainThread = false;
        return this;
    }

    public IncomingRpcMethodBuilder<Params, Result> AllowPreServerInit()
    {
        _allowPreServerInit = true;
        return this;
    }

    public IncomingRpcMethod<Params, Result> Build()
    {
        if (_resultSchema is null) throw new InvalidOperationException("No response defined");
        var attributes = new RpcMethodAttributes(_discoverable, _runOnMainThread, _allowPreServerInit);
        var paramInfo = Optional<ParamInfo>.Empty();
        if (_paramSchema is not null) paramInfo = Optional<ParamInfo>.Of(new ParamInfo(_paramName, _paramSchema.Info(), true));
        var info = new MethodInfo(_description, paramInfo, Optional<ResultInfo>.Of(new ResultInfo(_resultName, _resultSchema.Info())));
        if (_parameterlessFunction is not null)
            return new ParameterlessIncomingMethod<Params, Result>(info, attributes, _resultSchema, _parameterlessFunction);
        if (_function is not null)
        {
            if (_paramSchema is null) throw new InvalidOperationException("No param schema defined");
            return new IncomingMethod<Params, Result>(info, attributes, _paramSchema, _resultSchema, _function);
        }
        throw new InvalidOperationException("No method defined");
    }

    //Register registers the built method into the incoming registry, maps to vanilla register
    public IncomingRpcMethod<Params, Result> Register(NacreCraft.Registry.Registry<NacreCraft.Registry.IncomingRpcMethod<object, object>> registry, string key)
    {
        var method = Build();
        NacreCraft.Registry.Registry<NacreCraft.Registry.IncomingRpcMethod<object, object>>.Register(registry, key, method);
        return method;
    }
}
