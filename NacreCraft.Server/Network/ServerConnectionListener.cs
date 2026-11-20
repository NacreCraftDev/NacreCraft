using System.Net;
using NacreCraft.Network;

namespace NacreCraft.Server.Network;

//ServerConnectionListener the server-side TCP connection owner, maps to vanilla net.minecraft.server.network.ServerConnectionListener
//NC has no Netty, so it wraps ConnectionAcceptor and keeps the live Connection set; the per-connection listener is installed by the callback
public sealed class ServerConnectionListener : IDisposable
{
    private readonly List<Connection> _connections = new();
    private readonly Action<Connection> _onConnection;
    private ConnectionAcceptor? _acceptor;
    private bool _disposed;

    public ServerConnectionListener(Action<Connection> onConnection) => _onConnection = onConnection;

    //Connections a snapshot of the live connections
    public IReadOnlyList<Connection> Connections => _connections.ToArray();

    public int ConnectionCount => _connections.Count;

    //StartTcpServerListener starts the TCP listener on the given address and port
    public void StartTcpServerListener(IPAddress address, int port)
    {
        _acceptor = new ConnectionAcceptor(address, port, connection =>
        {
            lock (_connections) _connections.Add(connection);
            _onConnection(connection);
        });
        _acceptor.Start();
    }

    //Stop stops accepting and disconnects the live connections
    public void Stop()
    {
        _acceptor?.Stop();
        lock (_connections)
        {
            foreach (var connection in _connections) connection.Disconnect("Server closed");
            _connections.Clear();
        }
    }

    //Tick prunes closed connections and lets the connection layer advance
    public void Tick()
    {
        lock (_connections)
        {
            _connections.RemoveAll(connection => !connection.IsConnected);
            foreach (var connection in _connections) connection.Tick();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _acceptor?.Dispose();
        _acceptor = null;
    }
}
