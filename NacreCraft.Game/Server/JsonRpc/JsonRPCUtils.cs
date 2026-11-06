using System.Text.Json.Nodes;

namespace NacreCraft.Game.Server.JsonRpc;

//JsonRPCUtils JSON-RPC envelope helpers, maps to vanilla net.minecraft.server.jsonrpc.JsonRPCUtils
//The envelope is built on System.Text.Json nodes; every node handed in is cloned because a JsonNode can only have one parent
public static class JsonRPCUtils
{
    //JsonRpcVersion protocol version written into every envelope, maps to vanilla JSON_RPC_VERSION
    public const string JsonRpcVersion = "2.0";

    //OpenRpcVersion OpenRPC version used by the discovery document, maps to vanilla OPEN_RPC_VERSION
    public const string OpenRpcVersion = "1.3.2";

    //CreateSuccessResult builds a response carrying a result, maps to vanilla createSuccessResult
    public static JsonObject CreateSuccessResult(JsonNode? id, JsonNode? result)
    {
        var response = new JsonObject { ["jsonrpc"] = JsonRpcVersion, ["id"] = id?.DeepClone() };
        response["result"] = result?.DeepClone();
        return response;
    }

    //CreateRequest builds a request envelope, maps to vanilla createRequest(Integer, Identifier, List)
    public static JsonObject CreateRequest(int? id, NacreCraft.Registry.Identifier method, IReadOnlyList<JsonNode?> parameters)
        => CreateRequest(id, method.ToString(), parameters);

    //CreateRequest with the method as a plain string, maps to vanilla createRequest(Integer, String, List)
    public static JsonObject CreateRequest(int? id, string method, IReadOnlyList<JsonNode?> parameters)
    {
        var request = new JsonObject { ["jsonrpc"] = JsonRpcVersion };
        if (id is { } requestId) request["id"] = requestId;
        request["method"] = method;
        if (parameters.Count > 0)
        {
            var array = new JsonArray();
            foreach (var parameter in parameters) array.Add(parameter?.DeepClone());
            request["params"] = array;
        }
        return request;
    }

    //CreateError builds an error response, maps to vanilla createError
    public static JsonObject CreateError(JsonNode? id, string message, int errorCode, string? data)
    {
        var error = new JsonObject { ["code"] = errorCode, ["message"] = message };
        if (!string.IsNullOrWhiteSpace(data)) error["data"] = data;
        return new JsonObject
        {
            ["jsonrpc"] = JsonRpcVersion,
            ["id"] = id?.DeepClone(),
            ["error"] = error,
        };
    }

    //GetRequestId returns the id node, null when absent or JSON null, maps to vanilla getRequestId
    public static JsonNode? GetRequestId(JsonObject jsonObject)
        => jsonObject.TryGetPropertyValue("id", out var id) ? id : null;

    //GetMethodName returns the method name, null when absent, maps to vanilla getMethodName
    public static string? GetMethodName(JsonObject jsonObject)
        => jsonObject.TryGetPropertyValue("method", out var method) && method is JsonValue value
           && value.TryGetValue<string>(out var text)
            ? text
            : null;

    //GetParams returns the params node, null when absent, maps to vanilla getParams
    public static JsonNode? GetParams(JsonObject jsonObject)
        => jsonObject.TryGetPropertyValue("params", out var parameters) ? parameters : null;

    //GetResult returns the result node, null when absent, maps to vanilla getResult
    public static JsonNode? GetResult(JsonObject jsonObject)
        => jsonObject.TryGetPropertyValue("result", out var result) ? result : null;

    //GetError returns the error object, null when absent, maps to vanilla getError
    public static JsonObject? GetError(JsonObject jsonObject)
        => jsonObject.TryGetPropertyValue("error", out var error) ? error as JsonObject : null;
}
