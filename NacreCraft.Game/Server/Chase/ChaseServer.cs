using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NacreCraft.Game.Commands;
using NacreCraft.Logging;
using NacreCraft.Storage;

namespace NacreCraft.Game.Server.Chase;

//ChaseServer broadcasts the lead player's position to connected chase clients, maps to vanilla net.minecraft.server.chase.ChaseServer
//The position line is "t <dimension> <x> <y> <z> <yaw> <pitch>" in ASCII, sent only when it changes
public sealed class ChaseServer
{
    private readonly string _serverBindAddress;
    private readonly int _serverPort;
    private readonly Func<IReadOnlyList<ServerPlayer>> _players;
    private readonly int _broadcastIntervalMs;
    private readonly List<TcpClient> _clientSockets = new();
    private readonly object _clientsLock = new();

    private volatile bool _wantsToRun;
    private TcpListener? _serverSocket;

    public ChaseServer(string serverBindAddress, int serverPort, PlayerList playerList, int broadcastIntervalMs)
        : this(serverBindAddress, serverPort, () => playerList.Players, broadcastIntervalMs)
    {
    }

    //Player supplier overload keeps the transport testable without a live player list; the player-list constructor is the
    //production path used by /chase lead
    public ChaseServer(string serverBindAddress, int serverPort,
        Func<IReadOnlyList<ServerPlayer>> players, int broadcastIntervalMs)
    {
        _serverBindAddress = serverBindAddress;
        _serverPort = serverPort;
        _players = players;
        _broadcastIntervalMs = broadcastIntervalMs;
    }

    //Port the bound port, useful when the listener was started on port 0
    public int Port => _serverSocket?.LocalEndpoint is IPEndPoint endpoint ? endpoint.Port : _serverPort;

    //Start binds the listener and starts the acceptor and sender threads, maps to vanilla start
    public void Start()
    {
        if (_serverSocket is not null)
        {
            Log.Warning("Remote control server was asked to start, but it is already running. Will ignore.");
            return;
        }
        _wantsToRun = true;
        var address = ResolveBindAddress(_serverBindAddress);
        _serverSocket = new TcpListener(address, _serverPort);
        _serverSocket.Start(50);
        new Thread(RunAcceptor) { Name = "chase-server-acceptor", IsBackground = true }.Start();
        new Thread(RunSender) { Name = "chase-server-sender", IsBackground = true }.Start();
    }

    //ResolveBindAddress maps the vanilla bind address forms to an IP; an empty value or 0.0.0.0 means every interface
    private static IPAddress ResolveBindAddress(string address)
    {
        if (address is "" or "*" or "0.0.0.0") return IPAddress.Any;
        if (IPAddress.TryParse(address, out var parsed)) return parsed;
        return Dns.GetHostAddresses(address).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
               ?? IPAddress.Any;
    }

    //RunSender periodically pushes the first player's position to every connected client, maps to vanilla runSender
    private void RunSender()
    {
        PlayerPosition? oldPlayerPosition = null;
        while (_wantsToRun)
        {
            List<TcpClient> snapshot;
            lock (_clientsLock) snapshot = _clientSockets.ToList();
            if (snapshot.Count > 0)
            {
                var playerPosition = GetPlayerPosition();
                if (playerPosition is not null && !playerPosition.Equals(oldPlayerPosition))
                {
                    oldPlayerPosition = playerPosition;
                    var messageBytes = Encoding.ASCII.GetBytes(playerPosition.Format());
                    foreach (var client in snapshot)
                    {
                        if (!client.Connected) continue;
                        try
                        {
                            var output = client.GetStream();
                            output.Write(messageBytes);
                            output.Flush();
                        }
                        catch (IOException e)
                        {
                            Log.Info($"Remote control client socket got an IO exception and will be closed: {e.Message}");
                            CloseQuietly(client);
                        }
                        catch (ObjectDisposedException)
                        {
                            CloseQuietly(client);
                        }
                    }
                }
                lock (_clientsLock) _clientSockets.RemoveAll(client => !client.Connected);
            }
            if (_wantsToRun)
            {
                try
                {
                    Thread.Sleep(_broadcastIntervalMs);
                }
                catch (ThreadInterruptedException)
                {
                }
            }
        }
    }

    //Stop stops both threads and closes the listener, maps to vanilla stop
    public void Stop()
    {
        _wantsToRun = false;
        _serverSocket?.Stop();
        _serverSocket = null;
        lock (_clientsLock)
        {
            foreach (var client in _clientSockets) CloseQuietly(client);
            _clientSockets.Clear();
        }
    }

    //RunAcceptor accepts chase clients until stopped, maps to vanilla runAcceptor
    private void RunAcceptor()
    {
        while (_wantsToRun)
        {
            try
            {
                if (_serverSocket is null) break;
                Log.Info($"Remote control server is listening for connections on port {Port}");
                var clientSocket = _serverSocket.AcceptTcpClient();
                Log.Info($"Remote control server received client connection on port {clientSocket.Client.RemoteEndPoint}");
                lock (_clientsLock) _clientSockets.Add(clientSocket);
            }
            catch (SocketException e)
            {
                if (_wantsToRun) Log.Error($"Remote control server closed because of an IO exception: {e.Message}");
            }
            catch (ObjectDisposedException)
            {
                if (_wantsToRun) Log.Info("Remote control server closed by interrupt");
            }
            catch (InvalidOperationException)
            {
                if (_wantsToRun) Log.Info("Remote control server closed by interrupt");
            }
        }
        Log.Info("Remote control server is now stopped");
        _wantsToRun = false;
    }

    //GetPlayerPosition the first online player's position, null when nobody is online or the dimension is unknown
    private PlayerPosition? GetPlayerPosition()
    {
        var players = _players();
        if (players.Count == 0) return null;
        var player = players[0];
        var level = player.Level;
        var dimensionName = ChaseCommand.DimensionName(level.Dimension);
        if (dimensionName is null) return null;
        return new PlayerPosition(dimensionName, player.Pos.X, player.Pos.Y, player.Pos.Z, player.YRot, player.XRot);
    }

    private static void CloseQuietly(TcpClient client)
    {
        try { client.Close(); } catch (Exception) { }
    }

    //PlayerPosition one broadcast position line, maps to the vanilla nested record
    private sealed record PlayerPosition(string DimensionName, double X, double Y, double Z, float YRot, float XRot)
    {
        //Format builds the wire line; two decimals match the vanilla format string
        public string Format()
            => string.Create(CultureInfo.InvariantCulture,
                $"t {DimensionName} {X:0.00} {Y:0.00} {Z:0.00} {YRot:0.00} {XRot:0.00}\n");
    }
}
