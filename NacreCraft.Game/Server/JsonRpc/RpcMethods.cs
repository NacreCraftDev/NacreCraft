using System.Text.Json.Nodes;
using NacreCraft.Game.Server.JsonRpc.InternalApi;

namespace NacreCraft.Game.Server.JsonRpc;

//RpcMethodAttributes flags of one RPC method, maps to the Attributes record of vanilla IncomingRpcMethod/OutgoingRpcMethod
public sealed record RpcMethodAttributes(bool Discoverable, bool RunOnMainThread, bool AllowPreServerInit);

//IIncomingRpcMethod the discoverable half of an incoming RPC method, maps to vanilla IncomingRpcMethod
//The registry element type is the Registry layer's stub; the metadata half is shared with the discovery dataprovider and the
//transport half is the generic IncomingRpcMethod<Params,Result>
public interface IIncomingRpcMethod : NacreCraft.Registry.IncomingRpcMethod<object, object>
{
    MethodInfo Info();

    RpcMethodAttributes Attributes { get; }

    //Apply runs the method with the wire parameters and returns the encoded result, maps to vanilla apply
    JsonNode? Apply(MinecraftApi api, JsonNode? paramsJson, ClientInfo clientInfo);
}

//IOutgoingRpcMethod the discoverable half of an outgoing RPC method, maps to vanilla OutgoingRpcMethod
public interface IOutgoingRpcMethod : NacreCraft.Registry.OutgoingRpcMethod<object, object>
{
    MethodInfo Info();

    RpcMethodAttributes Attributes { get; }
}
