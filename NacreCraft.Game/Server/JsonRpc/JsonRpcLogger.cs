using System.Globalization;
using System.Text;
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
            : prefix + FormatPlaceholders(message, args);
        NacreCraft.Logging.Log.Info(line);
    }

    //FormatPlaceholders turns the SLF4J "{}" placeholders into indexed .NET placeholders before formatting
    private static string FormatPlaceholders(string message, object?[] args)
    {
        var builder = new StringBuilder(message.Length + 16);
        var argumentIndex = 0;
        for (var i = 0; i < message.Length; i++)
        {
            if (message[i] == '{' && i + 1 < message.Length && message[i + 1] == '}' && argumentIndex < args.Length)
            {
                builder.Append('{').Append(argumentIndex++).Append('}');
                i++;
                continue;
            }
            builder.Append(message[i]);
        }
        return string.Format(CultureInfo.InvariantCulture, builder.ToString(), args);
    }
}
