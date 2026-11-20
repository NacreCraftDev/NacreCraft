using System.Text;
using NacreCraft.Game.Server.Network;
using NacreCraft.Network;
using NacreCraft.Network.Protocol.Handshake;

namespace NacreCraft.Server.Network;

//ServerInfo server identity shown in the status ping and legacy query, maps to vanilla net.minecraft.server.ServerInfo
public interface ServerInfo
{
    string Motd { get; }

    string ServerVersion { get; }

    int PlayerCount { get; }

    int MaxPlayers { get; }
}

//LegacyQueryHandler answers pre-1.7 legacy server list pings, maps to vanilla net.minecraft.server.network.LegacyQueryHandler
//NC has no Netty pipeline, so callers pass the first packet bytes and receive the response bytes; a non-legacy payload returns false
public sealed class LegacyQueryHandler(ServerInfo server)
{
    //TryHandle parses a legacy ping payload and writes the 0xFF disconnect response
    public bool TryHandle(byte[] payload, out byte[] response)
    {
        response = Array.Empty<byte>();
        if (payload.Length == 0 || payload[0] != 254) return false;
        if (payload.Length == 1)
        {
            response = CreateLegacyDisconnectPacket(CreateVersion0Response(server));
            return true;
        }
        var buffer = new FriendlyByteBuf(payload);
        if (buffer.ReadByte() != 254) return false;
        if (!buffer.IsReadable) return false;
        if (buffer.ReadByte() != 1) return false;
        if (buffer.IsReadable && !ReadCustomPayloadPacket(buffer)) return false;
        response = CreateLegacyDisconnectPacket(CreateVersion1Response(server));
        return true;
    }

    private static bool ReadCustomPayloadPacket(FriendlyByteBuf buffer)
    {
        if (buffer.ReadByte() != LegacyProtocolUtils.CustomPayloadPacketId) return false;
        if (LegacyProtocolUtils.ReadLegacyString(buffer) != LegacyProtocolUtils.CustomPayloadPacketPingChannel) return false;
        var payloadSize = buffer.ReadUnsignedShort();
        if (buffer.ReadableBytes != payloadSize) return false;
        if (buffer.ReadByte() < 73) return false;
        LegacyProtocolUtils.ReadLegacyString(buffer);
        return buffer.ReadInt() <= 65535;
    }

    private static string CreateVersion0Response(ServerInfo server)
        => $"{server.Motd}§{server.PlayerCount}§{server.MaxPlayers}";

    private static string CreateVersion1Response(ServerInfo server)
        => $"§1\u0000{LegacyProtocolUtils.FakeProtocolVersion}\u0000{server.ServerVersion}\u0000{server.Motd}\u0000{server.PlayerCount}\u0000{server.MaxPlayers}";

    private static byte[] CreateLegacyDisconnectPacket(string reason)
    {
        var buffer = new FriendlyByteBuf();
        buffer.WriteByte((byte)LegacyProtocolUtils.DisconnectPacketId);
        LegacyProtocolUtils.WriteLegacyString(buffer, reason);
        return buffer.ToArray();
    }
}
