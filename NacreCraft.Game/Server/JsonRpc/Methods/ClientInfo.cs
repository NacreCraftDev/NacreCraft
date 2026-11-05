namespace NacreCraft.Game.Server.JsonRpc;

//ClientInfo the connection behind one RPC call, maps to vanilla net.minecraft.server.jsonrpc.methods.ClientInfo
public sealed record ClientInfo(int ConnectionId)
{
    public static ClientInfo Of(int connectionId) => new(connectionId);
}

public sealed class EncodeJsonRpcException(string message) : Exception(message);

public sealed class InvalidParameterJsonRpcException(string message) : Exception(message);

public sealed class InvalidRequestJsonRpcException(string message) : Exception(message);

public sealed class MethodNotFoundJsonRpcException(string message) : Exception(message);

public sealed class RemoteRpcErrorException(string message) : Exception(message);
