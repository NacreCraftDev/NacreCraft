namespace NacreCraft.Game.Server.JsonRpc;

//RpcMethodAttributes flags of one RPC method, maps to the Attributes record of vanilla IncomingRpcMethod/OutgoingRpcMethod
public sealed record RpcMethodAttributes(bool Discoverable, bool RunOnMainThread, bool AllowPreServerInit);

//IIncomingRpcMethod the discoverable half of an incoming RPC method, maps to vanilla IncomingRpcMethod
//The registry element type is the Registry layer's stub; only the metadata half is ported until the RPC transport lands
public interface IIncomingRpcMethod : NacreCraft.Registry.IncomingRpcMethod<object, object>
{
    MethodInfo Info();

    RpcMethodAttributes Attributes { get; }
}

//IOutgoingRpcMethod the discoverable half of an outgoing RPC method, maps to vanilla OutgoingRpcMethod
public interface IOutgoingRpcMethod : NacreCraft.Registry.OutgoingRpcMethod<object, object>
{
    MethodInfo Info();

    RpcMethodAttributes Attributes { get; }
}
