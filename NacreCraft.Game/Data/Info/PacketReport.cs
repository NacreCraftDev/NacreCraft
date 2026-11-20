using System.Reflection;
using System.Text.Json.Nodes;
using NacreCraft.Game.Data;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Network;
using NacreCraft.Network.Protocol.Common;
using NacreCraft.Network.Protocol.Configuration;
using NacreCraft.Network.Protocol.Cookie;
using NacreCraft.Network.Protocol.Handshake;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Ping;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Data.Info;

//PacketReport dumps every registered packet type grouped by protocol and flow, maps to vanilla net.minecraft.data.info.PacketReport
//NC packet type tables are static fields holding IPacketType instances, so reflection over the tables replaces the vanilla
//ProtocolInfo.DetailsProvider list; the identifier falls back to the field name when the table did not set one
public sealed class PacketReport(PackOutput output) : DataProvider
{
    private static readonly Type[] Tables =
    {
        typeof(HandshakePacketTypes),
        typeof(StatusPacketTypes),
        typeof(LoginPacketTypes),
        typeof(ConfigurationPacketTypes),
        typeof(GamePacketTypes),
        typeof(CommonPacketTypes),
        typeof(CookiePacketTypes),
        typeof(PingPacketTypes),
    };

    public Task Run(CachedOutput cache)
    {
        var protocols = new JsonObject();
        foreach (var table in Tables)
        foreach (var field in table.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not IPacketType packet) continue;
            if (protocols[packet.Protocol.Id()] is not JsonObject protocolData)
            {
                protocolData = new JsonObject();
                protocols[packet.Protocol.Id()] = protocolData;
            }
            var flowId = packet.Direction.ToString().ToLowerInvariant();
            if (protocolData[flowId] is not JsonObject flowData)
            {
                flowData = new JsonObject();
                protocolData[flowId] = flowData;
            }
            var packetId = packet.Identifier?.ToString()
                           ?? NacreCraft.Registry.Identifier.WithDefaultNamespace(field.Name.ToLowerInvariant()).ToString();
            flowData[packetId] = new JsonObject { ["protocol_id"] = packet.Id };
        }
        var path = Path.Combine(output.GetOutputFolder(PackOutput.Target.REPORTS), "packets.json");
        return DataProvider.SaveStable(cache, protocols, path);
    }

    public string GetName() => "Packet Report";
}
