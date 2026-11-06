using System.Text.Json.Nodes;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc;

//PendingRpcRequestBase the non-generic pending request shape, so a connection can keep one dictionary for every result type
public abstract class PendingRpcRequestBase
{
    protected PendingRpcRequestBase(Identifier methodName, long timeoutTime)
    {
        MethodName = methodName;
        TimeoutTime = timeoutTime;
    }

    //MethodName the requested method, used in the timeout message
    public Identifier MethodName { get; }

    //TimeoutTime wall-clock millisecond deadline
    public long TimeoutTime { get; }

    //Accept decodes the response and completes the future, throws when decoding fails
    public abstract void Accept(JsonNode? result);

    //Fail completes the future with an error
    public abstract void Fail(Exception error);

    //TimedOut whether the deadline has passed, maps to vanilla timedOut
    public bool TimedOut(long currentTime) => currentTime > TimeoutTime;
}

//PendingRpcRequest an outgoing request waiting for its response, maps to vanilla net.minecraft.server.jsonrpc.PendingRpcRequest
//The decoder belongs to the method, the future completes when a matching response arrives or the timeout expires
public sealed class PendingRpcRequest<Result> : PendingRpcRequestBase
{
    private readonly IOutgoingRpcResultDecoder<Result> _method;

    public PendingRpcRequest(Identifier methodName, IOutgoingRpcResultDecoder<Result> method,
        TaskCompletionSource<Result> resultFuture, long timeoutTime)
        : base(methodName, timeoutTime)
    {
        _method = method;
        ResultFuture = resultFuture;
    }

    //ResultFuture completed by Accept or by the timeout
    public TaskCompletionSource<Result> ResultFuture { get; }

    public override void Accept(JsonNode? result)
    {
        try
        {
            ResultFuture.TrySetResult(_method.DecodeResult(result));
        }
        catch (Exception e)
        {
            ResultFuture.TrySetException(e);
        }
    }

    public override void Fail(Exception error) => ResultFuture.TrySetException(error);
}
