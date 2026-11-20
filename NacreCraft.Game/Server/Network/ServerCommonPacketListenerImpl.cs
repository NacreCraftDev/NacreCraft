using NacreCraft.Network;
using NacreCraft.Network.Protocol.Common;
using NacreCraft.Network.Protocol.Cookie;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Ping;

namespace NacreCraft.Game.Server.Network;

//ServerCommonPacketListenerImpl common base for server-side listeners, maps to vanilla net.minecraft.server.network.ServerCommonPacketListenerImpl
//Owns the connection/profile/latency and the keep-alive bookkeeping; protocol-specific listeners inherit it and add their handlers
public abstract class ServerCommonPacketListenerImpl : ServerCommonPacketListener, TickablePacketListener
{
    protected readonly Connection Connection;

    protected ServerCommonPacketListenerImpl(Connection connection, GameProfile profile, int latency)
    {
        Connection = connection;
        Profile = profile;
        Latency = latency;
    }

    public GameProfile Profile { get; }

    public int Latency { get; set; }

    public abstract ConnectionProtocol Protocol { get; }

    //Send forwards a packet on the connection
    public void Send<THandler>(Packet<THandler> packet) where THandler : class => Connection.Send(packet);

    public void Disconnect(string reason) => Connection.Disconnect(reason);

    public virtual void OnDisconnect(string reason)
    {
    }

    public virtual bool IsAcceptingMessages => Connection.IsConnected;

    //TickListener runs keep-alive/timeout bookkeeping; the current connection layer handles timeouts itself
    public virtual void TickListener()
    {
    }

    public abstract void HandleClientInformation(ServerboundClientInformationPacket packet);

    public abstract void HandleCustomPayload(ServerboundCustomPayloadPacket packet);

    public abstract void HandleKeepAlive(ServerboundKeepAlivePacket packet);

    public abstract void HandlePong(ServerboundPongPacket packet);

    public abstract void HandleResourcePack(ServerboundResourcePackPacket packet);

    public abstract void HandleCustomClickAction(ServerboundCustomClickActionPacket packet);

    public abstract void HandleCookieResponse(ServerboundCookieResponsePacket packet);

    public abstract void HandlePingRequest(ServerboundPingRequestPacket packet);
}
