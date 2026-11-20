using System.Globalization;
using System.Net.Sockets;
using System.Text;
using NacreCraft.Game.Commands;
using NacreCraft.Logging;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.Chase;

//ChaseClient follows a chasing server and teleports the first online player to the broadcast position, maps to vanilla net.minecraft.server.chase.ChaseClient
//The client reconnects every five seconds after a lost connection, and every position line is turned into an /execute in ... run tp command
public sealed class ChaseClient
{
    //ReconnectIntervalSeconds delay before retrying a connection, maps to vanilla RECONNECT_INTERVAL_SECONDS
    private const int ReconnectIntervalSeconds = 5;

    private readonly string _serverHost;
    private readonly int _serverPort;
    private readonly MinecraftServer _server;
    private volatile bool _wantsToRun;
    private TcpClient? _socket;
    private Thread? _thread;

    public ChaseClient(string serverHost, int serverPort, MinecraftServer server)
    {
        _serverHost = serverHost;
        _serverPort = serverPort;
        _server = server;
    }

    //Start launches the reconnect/read thread, maps to vanilla start
    public void Start()
    {
        if (_thread is { IsAlive: true })
        {
            Log.Warning("Remote control client was asked to start, but it is already running. Will ignore.");
            return;
        }
        _wantsToRun = true;
        _thread = new Thread(Run) { Name = "chase-client", IsBackground = true };
        _thread.Start();
    }

    //Stop closes the socket and lets the loop exit, maps to vanilla stop
    public void Stop()
    {
        _wantsToRun = false;
        CloseQuietly(_socket);
        _socket = null;
        _thread = null;
    }

    //Run connects, reads position lines and reconnects until stopped, maps to vanilla run
    private void Run()
    {
        var serverAddress = _serverHost + ":" + _serverPort;
        while (_wantsToRun)
        {
            try
            {
                Log.Info($"Connecting to remote control server {serverAddress}");
                var socket = new TcpClient();
                _socket = socket;
                socket.Connect(_serverHost, _serverPort);
                Log.Info("Connected to remote control server! Will continuously execute the command broadcasted by that server.");
                using var reader = new StreamReader(socket.GetStream(), Encoding.ASCII);
                while (_wantsToRun)
                {
                    var message = reader.ReadLine();
                    if (message is null)
                    {
                        Log.Warning($"Lost connection to remote control server {serverAddress}. Will retry in {ReconnectIntervalSeconds}s.");
                        break;
                    }
                    HandleMessage(message);
                }
            }
            catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
            {
                if (_wantsToRun)
                    Log.Warning($"Lost connection to remote control server {serverAddress}. Will retry in {ReconnectIntervalSeconds}s.");
            }
            finally
            {
                CloseQuietly(_socket);
                _socket = null;
            }
            if (!_wantsToRun) break;
            try
            {
                Thread.Sleep(ReconnectIntervalSeconds * 1000);
            }
            catch (ThreadInterruptedException)
            {
            }
        }
    }

    //HandleMessage parses one line; only the teleport message type exists, maps to vanilla handleMessage
    private void HandleMessage(string message)
    {
        var parts = message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            Log.Warning($"Could not parse message '{message}', ignoring");
            return;
        }
        if (parts[0] != "t")
        {
            Log.Warning($"Unknown message type '{parts[0]}'");
            return;
        }
        HandleTeleport(message, parts);
    }

    //HandleTeleport parses the position and schedules the teleport command on the main loop
    private void HandleTeleport(string message, string[] parts)
    {
        if (parts.Length < 7)
        {
            Log.Warning($"Could not parse message '{message}', ignoring");
            return;
        }
        var levelKey = ChaseCommand.DimensionKey(parts[1]);
        if (levelKey is null)
        {
            Log.Warning($"Could not parse message '{message}', ignoring");
            return;
        }
        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
            || !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)
            || !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var yRot)
            || !float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var xRot))
        {
            Log.Warning($"Could not parse message '{message}', ignoring");
            return;
        }
        var target = new TeleportTarget(levelKey, x, y, z, yRot, xRot);
        ExecuteCommand(target);
    }

    //ExecuteCommand schedules the teleport for the first online player on the main loop
    //Vanilla builds a player command source with owner permissions; NC enqueues a console command, which has the same access
    //and runs at the next tick boundary instead of on the chase thread
    private void ExecuteCommand(TeleportTarget target)
    {
        var players = _server.PlayerList.Players;
        if (players.Count == 0) return;
        var playerName = players[0].Profile.Name;
        var command = string.Create(CultureInfo.InvariantCulture,
            $"execute in {target.Level.Identifier} run tp {playerName} {target.X:0.000} {target.Y:0.000} {target.Z:0.000} {target.YRot:0.000} {target.XRot:0.000}");
        _server.EnqueueConsoleCommand(ServerCommandSource.Console(_server), command);
    }

    private static void CloseQuietly(TcpClient? client)
    {
        if (client is null) return;
        try { client.Close(); } catch (Exception) { }
    }

    //TeleportTarget one decoded position line, maps to the vanilla nested record
    private sealed record TeleportTarget(ResourceKey<Level> Level, float X, float Y, float Z, float YRot, float XRot);
}
