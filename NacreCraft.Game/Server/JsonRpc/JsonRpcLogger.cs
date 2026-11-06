using System.Globalization;
using NacreCraft.Logging;

namespace NacreCraft.Game.Server.JsonRpc;

//JsonRpcLogger logs connection-scoped RPC traffic, maps to vanilla net.minecraft.server.jsonrpc.JsonRpcLogger
//Every line carries the connection id so concurrent connections can be told apart in the server log
public sealed class JsonRpcLogger
{
    private const string Prefix = "RPC Connection #{0}: ";

    public void Log(ClientInfo clientInfo, string message, params object?[] args)
    {
        var prefix = string.Format(CultureInfo.InvariantCulture, Prefix, clientInfo.ConnectionId);
        var line = args.Length == 0
            ? prefix + message
            : prefix + string.Format(CultureInfo.InvariantCulture, message, args);
        NacreCraft.Logging.Log.Info(line);
    }
}
