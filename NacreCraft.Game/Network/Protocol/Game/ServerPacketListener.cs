//ServerPacketListener maps to vanilla net.minecraft.network.protocol.game.ServerPacketListener
using NacreCraft.Logging;
using NacreCraft.Network;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerPacketListener base for all serverbound protocol listeners, maps to vanilla ServerPacketListener
//Vanilla only adds a default onPacketError override on top of ServerboundPacketListener
public interface ServerPacketListener : ServerboundPacketListener
{
    //OnPacketError logs and suppresses a packet handling failure, maps to vanilla onPacketError
    //NacreCraft's PacketListener has no onPacketError hook yet, so this is a new default method for the dispatcher to call
    void OnPacketError<THandler>(Packet<THandler> packet, Exception exception)
        where THandler : class
        => Log.Error($"Failed to handle packet {packet.Type}, suppressing error: {exception}");
}
