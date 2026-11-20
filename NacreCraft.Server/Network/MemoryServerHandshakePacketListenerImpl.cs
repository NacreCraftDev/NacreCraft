using NacreCraft.Network;
using NacreCraft.Network.Protocol.Handshake;
using NacreCraft.Network.Protocol.Login;

namespace NacreCraft.Server.Network;

//MemoryServerHandshakePacketListenerImpl handshake listener for in-memory connections, maps to vanilla net.minecraft.server.network.MemoryServerHandshakePacketListenerImpl
//Only LOGIN is accepted; the connection moves to the login protocol with the supplied context
public sealed class MemoryServerHandshakePacketListenerImpl : ServerHandshakePacketListener
{
    private readonly Connection _connection;
    private readonly ServerLoginContext _context;

    public MemoryServerHandshakePacketListenerImpl(Connection connection, ServerLoginContext context)
    {
        _connection = connection;
        _context = context;
    }

    public void HandleIntention(ClientIntentionPacket packet)
    {
        if (packet.Intention != ClientIntent.Login)
            throw new NotSupportedException($"Invalid intention {packet.Intention}");
        _connection.SetupInboundProtocol(LoginProtocols.Serverbound,
            new ServerLoginPacketListenerImpl(_connection, _context));
        _connection.SetupOutboundProtocol(LoginProtocols.Clientbound);
    }

    public void OnDisconnect(string reason)
    {
    }

    public bool IsAcceptingMessages => _connection.IsConnected;
}
