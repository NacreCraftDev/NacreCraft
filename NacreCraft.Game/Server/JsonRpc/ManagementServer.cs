using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.JsonRpc.Security;
using NacreCraft.Game.Server.Notifications;
using NacreCraft.Logging;
using WebSocketType = System.Net.WebSockets.WebSocket;

namespace NacreCraft.Game.Server.JsonRpc;

//ManagementServer the management HTTP/WebSocket listener, maps to vanilla net.minecraft.server.jsonrpc.ManagementServer
//Vanilla is Netty based. NC has no Netty, so this runs a TcpListener with an optional SslStream and performs the HTTP upgrade
//handshake itself; WebSocket.CreateFromStream then turns the stream into a websocket. This keeps TLS and plaintext on one path
public sealed class ManagementServer : IRpcConnectionSet, IRpcConnectionListener
{
    //WebSocketGuid the RFC 6455 accept-key salt
    private const string WebSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private readonly string _host;
    private readonly int _port;
    private readonly JsonRpcAuthenticator _authenticator;
    private readonly HashSet<Connection> _connections = new();
    private readonly object _connectionsLock = new();
    private readonly JsonRpcLogger _logger = new();

    private TcpListener? _listener;
    private X509Certificate2? _certificate;
    private CancellationTokenSource? _cancellation;
    private Timer? _heartbeat;
    private Timer? _connectionTicker;

    public ManagementServer(string host, int port, JsonRpcAuthenticator authenticator)
    {
        _host = host;
        _port = port;
        _authenticator = authenticator;
    }

    //Port the bound port, maps to vanilla getPort
    public int Port => _listener?.LocalEndpoint is IPEndPoint endpoint ? endpoint.Port : _port;

    //ScheduleHeartbeat restarts the status heartbeat timer, maps to vanilla scheduleHeartbeat
    public bool ScheduleHeartbeat(NotificationManager notificationManager, long periodSeconds)
    {
        _heartbeat?.Dispose();
        _heartbeat = null;
        if (periodSeconds <= 0) return true;
        var period = TimeSpan.FromSeconds(periodSeconds);
        _heartbeat = new Timer(_ => notificationManager.StatusHeartbeat(), null, period, period);
        return true;
    }

    public void OnConnected(Connection connection)
    {
        lock (_connectionsLock) _connections.Add(connection);
    }

    public void OnDisconnected(Connection connection)
    {
        lock (_connectionsLock) _connections.Remove(connection);
    }

    public void StartWithoutTls(MinecraftApi minecraftApi) => Start(minecraftApi, null);

    //StartWithTls starts the listener with the supplied certificate, maps to vanilla startWithTls
    public void StartWithTls(MinecraftApi minecraftApi, X509Certificate2 certificate)
        => Start(minecraftApi, certificate);

    private void Start(MinecraftApi minecraftApi, X509Certificate2? certificate)
    {
        _certificate = certificate;
        var listener = new TcpListener(ResolveBindAddress(_host), _port);
        listener.Start();
        _listener = listener;
        _cancellation = new CancellationTokenSource();
        _ = Task.Run(() => AcceptLoop(listener, minecraftApi, _cancellation.Token));
        _connectionTicker = new Timer(_ => Tick(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        Log.Info($"Json-RPC Management connection listening on {_host}:{Port} tls={certificate is not null}");
    }

    private static IPAddress ResolveBindAddress(string host)
        => host is "" or "*" or "0.0.0.0"
            ? IPAddress.Any
            : host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                ? IPAddress.Loopback
                : IPAddress.TryParse(host, out var address) ? address : IPAddress.Any;

    private async Task AcceptLoop(TcpListener listener, MinecraftApi minecraftApi, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                Log.Error($"Management server accept failed: {e.Message}");
                continue;
            }
            _ = Task.Run(() => HandleClient(client, minecraftApi, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClient(TcpClient client, MinecraftApi minecraftApi, CancellationToken cancellationToken)
    {
        var remoteAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Stream? stream = null;
        try
        {
            stream = client.GetStream();
            if (_certificate is not null)
            {
                var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
                await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = _certificate,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                }, cancellationToken).ConfigureAwait(false);
                stream = ssl;
            }
            var request = await ReadHttpRequestAsync(stream, cancellationToken).ConfigureAwait(false);
            if (request is null) return;
            var (requestLine, headers) = request.Value;
            var check = _authenticator.Check(headers);
            if (!check.Allowed)
            {
                await WriteHttpResponseAsync(stream,
                    "HTTP/1.1 401 Unauthorized\r\nContent-Type: application/json\r\n" +
                    $"Content-Length: {Encoding.UTF8.GetByteCount(new UnauthorizedResponse(check.Reason ?? "Unauthorized").Body)}\r\n" +
                    "Connection: close\r\n\r\n" + new UnauthorizedResponse(check.Reason ?? "Unauthorized").Body,
                    cancellationToken).ConfigureAwait(false);
                return;
            }
            if (!IsWebSocketUpgrade(headers) || !headers.TryGetValue("Sec-WebSocket-Key", out var webSocketKey)
                || string.IsNullOrEmpty(webSocketKey))
            {
                await WriteHttpResponseAsync(stream,
                    "HTTP/1.1 400 Bad Request\r\nContent-Length: 0\r\nConnection: close\r\n\r\n",
                    cancellationToken).ConfigureAwait(false);
                return;
            }
            var subProtocol = check.TokenSentInSecWebsocketProtocol
                ? JsonRpcAuthenticator.SubProtocolValue
                : null;
            var accept = Convert.ToBase64String(SHA1.HashData(
                Encoding.ASCII.GetBytes(webSocketKey + WebSocketGuid)));
            var handshake = new StringBuilder()
                .Append("HTTP/1.1 101 Switching Protocols\r\n")
                .Append("Upgrade: websocket\r\n")
                .Append("Connection: Upgrade\r\n")
                .Append("Sec-WebSocket-Accept: ").Append(accept).Append("\r\n");
            if (subProtocol is not null)
                handshake.Append("Sec-WebSocket-Protocol: ").Append(subProtocol).Append("\r\n");
            handshake.Append("\r\n");
            await WriteHttpResponseAsync(stream, handshake.ToString(), cancellationToken).ConfigureAwait(false);

            using var socket = WebSocketType.CreateFromStream(stream, new System.Net.WebSockets.WebSocketCreationOptions
            {
                IsServer = true,
                SubProtocol = subProtocol,
            });
            var channel = new WebSocketRpcChannel(socket, remoteAddress);
            var connection = new Connection(channel, this, minecraftApi, _logger);
            connection.Open();
            try
            {
                await ReceiveLoop(socket, connection, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                connection.Close();
            }
        }
        catch (Exception e) when (e is IOException or SocketException or AuthenticationException or OperationCanceledException)
        {
            Log.Debug($"Management client {remoteAddress} ended: {e.Message}");
        }
        catch (Exception e)
        {
            Log.Error($"Management client {remoteAddress} failed: {e.Message}");
        }
        finally
        {
            try { stream?.Dispose(); } catch (Exception) { }
            client.Dispose();
        }
    }

    //ReadHttpRequestAsync reads one HTTP request head, null when the peer closed early
    private static async Task<(string RequestLine, Dictionary<string, string?> Headers)?> ReadHttpRequestAsync(
        Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var builder = new StringBuilder();
        while (builder.Length < 65536)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read <= 0) return null;
            builder.Append(Encoding.ASCII.GetString(buffer, 0, read));
            var end = builder.ToString().IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (end < 0) continue;
            var head = builder.ToString(0, end);
            var lines = head.Split("\r\n");
            var headers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < lines.Length; i++)
            {
                var separator = lines[i].IndexOf(':');
                if (separator <= 0) continue;
                headers[lines[i][..separator].Trim()] = lines[i][(separator + 1)..].Trim();
            }
            return (lines[0], headers);
        }
        return null;
    }

    private static bool IsWebSocketUpgrade(Dictionary<string, string?> headers)
        => headers.TryGetValue("Upgrade", out var upgrade)
           && string.Equals(upgrade, "websocket", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteHttpResponseAsync(Stream stream, string response, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReceiveLoop(WebSocketType socket, Connection connection,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var builder = new StringBuilder();
        while (socket.State == System.Net.WebSockets.WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            System.Net.WebSockets.WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                return;
            }
            if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close) return;
            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (!result.EndOfMessage) continue;
            var message = builder.ToString();
            builder.Clear();
            try
            {
                connection.Receive(JsonNode.Parse(message));
            }
            catch (Exception e)
            {
                connection.OnParseError(e.Message);
            }
        }
    }

    //Stop closes the listener and every connection, maps to vanilla stop
    public void Stop(bool closeNioEventLoopGroup)
    {
        _cancellation?.Cancel();
        _heartbeat?.Dispose();
        _heartbeat = null;
        _connectionTicker?.Dispose();
        _connectionTicker = null;
        _listener?.Stop();
        _listener = null;
        _certificate = null;
        List<Connection> connections;
        lock (_connectionsLock)
        {
            connections = _connections.ToList();
            _connections.Clear();
        }
        foreach (var connection in connections) connection.Close();
    }

    //Tick expires pending requests on every connection, maps to vanilla tick
    public void Tick()
    {
        foreach (var connection in Snapshot()) connection.Tick();
    }

    public void ForEachConnection(Action<Connection> action)
    {
        foreach (var connection in Snapshot()) action(connection);
    }

    private List<Connection> Snapshot()
    {
        lock (_connectionsLock) return _connections.ToList();
    }

    //WebSocketRpcChannel IRpcChannel over one websocket
    private sealed class WebSocketRpcChannel(WebSocketType socket, string remoteAddress) : IRpcChannel
    {
        public string RemoteAddress => remoteAddress;

        public void Send(JsonNode? message)
        {
            if (socket.State != System.Net.WebSockets.WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(message?.ToJsonString() ?? "null");
            _ = socket.SendAsync(new ArraySegment<byte>(bytes),
                System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public void Close()
        {
            if (socket.State is System.Net.WebSockets.WebSocketState.Open
                or System.Net.WebSockets.WebSocketState.CloseReceived)
            {
                _ = socket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "closed",
                    CancellationToken.None);
            }
        }
    }
}
