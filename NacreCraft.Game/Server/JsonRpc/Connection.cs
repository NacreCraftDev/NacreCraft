using System.Text.Json.Nodes;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc;

//IRpcChannel the transport half of one management connection, replaces the Netty channel the vanilla Connection writes to
public interface IRpcChannel
{
    //RemoteAddress the peer address used in log lines
    string RemoteAddress { get; }

    //Send writes one JSON message
    void Send(JsonNode? message);

    //Close closes the transport
    void Close();
}

//IRpcConnectionListener receives connection lifecycle events, implemented by the management server
public interface IRpcConnectionListener
{
    void OnConnected(Connection connection);

    void OnDisconnected(Connection connection);
}

//Connection one management connection, maps to vanilla net.minecraft.server.jsonrpc.Connection
//Handles JSON-RPC envelopes, dispatches incoming methods and tracks outgoing requests waiting for replies
public sealed class Connection
{
    private static int _connectionIdCounter;

    private readonly JsonRpcLogger _jsonRpcLogger;
    private readonly IRpcConnectionListener? _listener;
    private readonly IRpcChannel _channel;
    private readonly MinecraftApi _minecraftApi;
    private readonly ClientInfo _clientInfo;
    private readonly Dictionary<int, PendingRpcRequestBase> _pendingRequests = new();
    private readonly object _pendingLock = new();
    private int _transactionId;

    public Connection(IRpcChannel channel, IRpcConnectionListener? listener, MinecraftApi minecraftApi,
        JsonRpcLogger jsonRpcLogger)
    {
        _channel = channel;
        _listener = listener;
        _minecraftApi = minecraftApi;
        _jsonRpcLogger = jsonRpcLogger;
        _clientInfo = ClientInfo.Of(Interlocked.Increment(ref _connectionIdCounter));
    }

    public ClientInfo ClientInfo => _clientInfo;

    //Open logs the connection and notifies the listener, maps to vanilla channelActive
    public void Open()
    {
        _jsonRpcLogger.Log(_clientInfo, "Management connection opened for {}", _channel.RemoteAddress);
        _listener?.OnConnected(this);
    }

    //Close logs the connection and notifies the listener, maps to vanilla channelInactive
    public void Close()
    {
        _jsonRpcLogger.Log(_clientInfo, "Management connection closed for {}", _channel.RemoteAddress);
        FailAllPending(new ObjectDisposedException(nameof(Connection), "connection closed"));
        _listener?.OnDisconnected(this);
    }

    //Tick expires pending requests, maps to vanilla tick
    public void Tick()
    {
        var now = Environment.TickCount64;
        lock (_pendingLock)
        {
            List<int>? expired = null;
            foreach (var (id, request) in _pendingRequests)
            {
                if (!request.TimedOut(now)) continue;
                (expired ??= new()).Add(id);
            }
            if (expired is null) return;
            foreach (var id in expired)
            {
                var request = _pendingRequests[id];
                _pendingRequests.Remove(id);
                request.Fail(new TimeoutException(
                    $"RPC method {request.MethodName} timed out waiting for response"));
            }
        }
    }

    //Receive handles one decoded message and writes the response, maps to vanilla channelRead0
    public void Receive(JsonNode? message)
    {
        var response = HandleMessage(message);
        if (response is not null) _channel.Send(response);
    }

    //HandleMessage handles one decoded message and returns the response, null when nothing should be written
    public JsonNode? HandleMessage(JsonNode? message)
    {
        if (message is JsonObject jsonObject) return HandleJsonObject(jsonObject);
        if (message is JsonArray batch) return HandleBatchRequest(batch);
        return JsonRPCErrors.InvalidRequest.CreateWithUnknownId(null);
    }

    //OnParseError reports a transport parse failure back to the peer, maps to vanilla exceptionCaught
    public void OnParseError(string message)
        => _channel.Send(JsonRPCErrors.ParseError.CreateWithUnknownId(message));

    private JsonArray HandleBatchRequest(JsonArray batchRequests)
    {
        var batchResponses = new JsonArray();
        foreach (var entry in batchRequests)
        {
            if (entry is not JsonObject obj) continue;
            var response = HandleJsonObject(obj);
            if (response is not null) batchResponses.Add(response);
        }
        return batchResponses;
    }

    //SendNotification sends a notification without waiting for a reply
    public void SendNotification<Params>(Identifier methodName, OutgoingRpcMethod<Params, object?> method,
        Params parameters)
        => SendRequestCore(methodName, method, parameters, false);

    //SendRequest sends a request and returns the future result
    public Task<Result> SendRequest<Params, Result>(Identifier methodName,
        OutgoingRpcMethod<Params, Result> method, Params parameters)
        => SendRequestCore(methodName, method, parameters, true)!;

    private Task<Result>? SendRequestCore<Params, Result>(Identifier methodName,
        OutgoingRpcMethod<Params, Result> method, Params parameters, bool expectReply)
    {
        if (_minecraftApi.NotificationManager.Server() is null && !method.Attributes.AllowPreServerInit)
        {
            return expectReply
                ? Task.FromException<Result>(new InvalidRequestJsonRpcException(
                    $"Method cannot be dispatched pre server initialization: {methodName}"))
                : null;
        }
        var jsonParams = parameters is null
            ? Array.Empty<JsonNode?>()
            : new[] { method.EncodeParams(parameters) };
        if (!expectReply)
        {
            _channel.Send(JsonRPCUtils.CreateRequest(null, methodName, jsonParams));
            return null;
        }
        var future = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        var id = Interlocked.Increment(ref _transactionId);
        lock (_pendingLock)
        {
            _pendingRequests[id] = new PendingRpcRequest<Result>(methodName, method, future,
                Environment.TickCount64 + 5000);
        }
        _channel.Send(JsonRPCUtils.CreateRequest(id, methodName, jsonParams));
        return future.Task;
    }

    //HandleJsonObject routes one envelope, maps to vanilla handleJsonObject
    public JsonObject? HandleJsonObject(JsonObject jsonObject)
    {
        try
        {
            var hasId = jsonObject.TryGetPropertyValue("id", out var id);
            var hasResult = jsonObject.TryGetPropertyValue("result", out var result);
            var hasError = jsonObject.TryGetPropertyValue("error", out var errorNode);
            var hasParams = jsonObject.TryGetPropertyValue("params", out var parameters);
            var method = JsonRPCUtils.GetMethodName(jsonObject);
            if (method is not null && !hasResult && !hasError)
            {
                if (hasId && !IsValidRequestId(id))
                    return JsonRPCErrors.InvalidRequest.CreateWithUnknownId(
                        "Invalid request id - only String, Number and NULL supported");
                return HandleIncomingRequest(hasId ? id : null, method, hasParams ? parameters : null);
            }
            if (method is null && hasResult && !hasError && hasId)
            {
                if (TryReadResponseId(id, out var responseId))
                {
                    HandleRequestResponse(responseId, result);
                    return null;
                }
                _jsonRpcLogger.Log(_clientInfo, "Received response with id we did not request: {}", id?.ToJsonString());
                return null;
            }
            if (method is null && !hasResult && hasError)
                return HandleError(hasId ? id : null, errorNode as JsonObject);
            return JsonRPCErrors.InvalidRequest.CreateWithoutData(hasId ? id : null);
        }
        catch (Exception e)
        {
            _jsonRpcLogger.Log(_clientInfo, "Error while handling rpc request: {}", e.Message);
            return JsonRPCErrors.InternalError.CreateWithUnknownId(
                "Unknown error handling request - check server logs for stack trace");
        }
    }

    private static bool IsValidRequestId(JsonNode? id)
    {
        if (id is null) return true;
        if (id is not JsonValue value) return false;
        return value.TryGetValue<string>(out _) || value.TryGetValue<int>(out _)
            || value.TryGetValue<long>(out _) || value.TryGetValue<double>(out _);
    }

    private static bool TryReadResponseId(JsonNode? id, out int responseId)
    {
        responseId = 0;
        return id is JsonValue value && value.TryGetValue<int>(out responseId);
    }

    private JsonObject? HandleIncomingRequest(JsonNode? id, string method, JsonNode? parameters)
    {
        var sendResponse = id is not null;
        try
        {
            var result = DispatchIncomingRequest(method, parameters);
            if (result is null || !sendResponse) return null;
            return JsonRPCUtils.CreateSuccessResult(id, result);
        }
        catch (EncodeJsonRpcException e)
        {
            _jsonRpcLogger.Log(_clientInfo, "Failed to encode json rpc response {}: {}", method, e.Message);
            return sendResponse ? JsonRPCErrors.InternalError.Create(id, e.Message) : null;
        }
        catch (InvalidParameterJsonRpcException e)
        {
            _jsonRpcLogger.Log(_clientInfo, "Invalid parameter invocation {}: {}", method, e.Message);
            return sendResponse ? JsonRPCErrors.InvalidParams.Create(id, e.Message) : null;
        }
        catch (InvalidRequestJsonRpcException e)
        {
            return sendResponse ? JsonRPCErrors.InvalidRequest.Create(id, e.Message) : null;
        }
        catch (MethodNotFoundJsonRpcException e)
        {
            return sendResponse ? JsonRPCErrors.MethodNotFound.Create(id, e.Message) : null;
        }
        catch (Exception e)
        {
            _jsonRpcLogger.Log(_clientInfo, "Error while dispatching rpc method {}: {}", method, e.Message);
            return sendResponse ? JsonRPCErrors.InternalError.CreateWithoutData(id) : null;
        }
    }

    //DispatchIncomingRequest runs a registered incoming method, maps to vanilla dispatchIncomingRequest
    public JsonNode? DispatchIncomingRequest(string method, JsonNode? parameters)
    {
        var identifier = Identifier.TryParse(method)
                         ?? throw new InvalidRequestJsonRpcException($"Failed to parse method value: {method}");
        var incoming = BuiltInRegistries.INCOMING_RPC_METHOD.GetValue(identifier) as IIncomingRpcMethod
                       ?? throw new MethodNotFoundJsonRpcException($"Method not found: {method}");
        var attributes = incoming.Attributes;
        var notificationManager = _minecraftApi.NotificationManager;
        if (notificationManager is not null && notificationManager.Server() is null && !attributes.AllowPreServerInit)
        {
            throw new InvalidRequestJsonRpcException(
                $"Method cannot be dispatched pre server initialization: {method}");
        }
        if (attributes.RunOnMainThread)
            return _minecraftApi.Submit(() => incoming.Apply(_minecraftApi, parameters, _clientInfo))
                .GetAwaiter().GetResult();
        return incoming.Apply(_minecraftApi, parameters, _clientInfo);
    }

    private void HandleRequestResponse(int id, JsonNode? result)
    {
        PendingRpcRequestBase? request;
        lock (_pendingLock)
        {
            _pendingRequests.TryGetValue(id, out request);
            if (request is not null) _pendingRequests.Remove(id);
        }
        if (request is null)
        {
            _jsonRpcLogger.Log(_clientInfo, "Received unknown response (id: {}): {}", id, result?.ToJsonString());
            return;
        }
        request.Accept(result);
    }

    private JsonObject? HandleError(JsonNode? id, JsonObject? error)
    {
        if (id is not null && TryReadResponseId(id, out var responseId))
        {
            PendingRpcRequestBase? request;
            lock (_pendingLock)
            {
                _pendingRequests.TryGetValue(responseId, out request);
                if (request is not null) _pendingRequests.Remove(responseId);
            }
            request?.Fail(new RemoteRpcErrorException($"Remote RPC error: {error?.ToJsonString()}"));
        }
        _jsonRpcLogger.Log(_clientInfo, "Received error (id: {}): {}", id?.ToJsonString(), error?.ToJsonString());
        return null;
    }

    private void FailAllPending(Exception error)
    {
        lock (_pendingLock)
        {
            foreach (var request in _pendingRequests.Values) request.Fail(error);
            _pendingRequests.Clear();
        }
    }
}
