using System.Text.Json.Nodes;

namespace NacreCraft.Game.Server.JsonRpc;

//JsonRpcError one JSON-RPC error code, maps to one enum constant of vanilla net.minecraft.server.jsonrpc.JsonRPCErrors
//A record instance per code keeps the create methods while C# enums cannot carry behaviour
public sealed record JsonRpcError(int ErrorCode, string Message)
{
    public static readonly JsonRpcError ParseError = new(-32700, "Parse error");
    public static readonly JsonRpcError InvalidRequest = new(-32600, "Invalid Request");
    public static readonly JsonRpcError MethodNotFound = new(-32601, "Method not found");
    public static readonly JsonRpcError InvalidParams = new(-32602, "Invalid params");
    public static readonly JsonRpcError InternalError = new(-32603, "Internal error");

    //CreateWithUnknownId an error whose request id could not be read, maps to vanilla createWithUnknownId
    public JsonObject CreateWithUnknownId(string? data) => JsonRPCUtils.CreateError(null, Message, ErrorCode, data);

    //CreateWithoutData an error without a data field, maps to vanilla createWithoutData
    public JsonObject CreateWithoutData(JsonNode? id) => JsonRPCUtils.CreateError(id, Message, ErrorCode, null);

    //Create an error with a data field, maps to vanilla create
    public JsonObject Create(JsonNode? id, string? data) => JsonRPCUtils.CreateError(id, Message, ErrorCode, data);
}

//JsonRPCErrors the JSON-RPC error constants, maps to vanilla net.minecraft.server.jsonrpc.JsonRPCErrors
public static class JsonRPCErrors
{
    public static readonly JsonRpcError ParseError = JsonRpcError.ParseError;
    public static readonly JsonRpcError InvalidRequest = JsonRpcError.InvalidRequest;
    public static readonly JsonRpcError MethodNotFound = JsonRpcError.MethodNotFound;
    public static readonly JsonRpcError InvalidParams = JsonRpcError.InvalidParams;
    public static readonly JsonRpcError InternalError = JsonRpcError.InternalError;
}
