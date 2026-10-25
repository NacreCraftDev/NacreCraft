using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Network.Protocol;

namespace NacreCraft.Client.Network.Protocol.Game;

//ClientGameProtocols client outbound play protocol
//Starts from the shared GameProtocols.Serverbound table and appends the packets only a client sends from its own screens
//Keeping these registrations here leaves the shared Game table free of client-only entries
public static class ClientGameProtocols
{
    public static readonly SimpleUnboundProtocol<ServerGamePacketListener> ServerboundTemplate =
        ProtocolInfoBuilder<ServerGamePacketListener>.Extend(GameProtocols.ServerboundTemplate)
            //Tag queries come from the F3 debug screens
            .AddPacket(GamePacketTypes.ServerboundBlockEntityTagQuery, ServerboundBlockEntityTagQueryPacket.StreamCodec)
            .AddPacket(GamePacketTypes.ServerboundEntityTagQuery, ServerboundEntityTagQueryPacket.StreamCodec)
            //Bundle item selection comes from the inventory bundle tooltip
            .AddPacket(GamePacketTypes.ServerboundBundleItemSelected, ServerboundSelectBundleItemPacket.StreamCodec)
            //Block editor screens: command block, jigsaw, structure, test block and test instance
            .AddPacket(GamePacketTypes.ServerboundSetCommandBlock, ServerboundSetCommandBlockPacket.StreamCodec)
            .AddPacket(GamePacketTypes.ServerboundSetJigsawBlock, ServerboundSetJigsawBlockPacket.StreamCodec)
            .AddPacket(GamePacketTypes.ServerboundSetStructureBlock, ServerboundSetStructureBlockPacket.StreamCodec)
            .AddPacket(GamePacketTypes.ServerboundSetTestBlock, ServerboundSetTestBlockPacket.StreamCodec)
            .AddPacket(GamePacketTypes.ServerboundTestInstanceBlockAction, ServerboundTestInstanceBlockActionPacket.StreamCodec)
            .AddPacket(GamePacketTypes.ServerboundJigsawGenerate, ServerboundJigsawGeneratePacket.StreamCodec)
            .AddPacket(GamePacketTypes.ServerboundSetCommandMinecart, ServerboundSetCommandMinecartPacket.StreamCodec)
            //Game rule editing
            .AddPacket(GamePacketTypes.ServerboundSetGameRule, ServerboundSetGameRulePacket.StreamCodec)
            .BuildUnbound();

    public static readonly ProtocolInfo<ServerGamePacketListener> Serverbound = ServerboundTemplate.Bind();
}
