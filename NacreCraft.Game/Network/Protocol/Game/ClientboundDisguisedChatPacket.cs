namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDisguisedChatPacket disguised chat packet, maps to vanilla ClientboundDisguisedChatPacket
//Fields: Message(Component), ChatType(ChatType.Bound)
public sealed record ClientboundDisguisedChatPacket(Component Message, object ChatType) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundDisguisedChatPacket> StreamCodec { get; } = new DisguisedChatCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDisguisedChat;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDisguisedChat(this);

    private sealed class DisguisedChatCodec : StreamCodec<FriendlyByteBuf, ClientboundDisguisedChatPacket>
    {
        //Vanilla order: message component, chat type holder (VAR_INT, 0 means inline), sender name, optional target name
        public ClientboundDisguisedChatPacket Decode(FriendlyByteBuf buf)
        {
            var message = ComponentSerialization.StreamCodec.Decode(buf);
            var chatType = buf.ReadVarInt();
            if (chatType == 0)
                throw new NotSupportedException("Inline chat type is not implemented");
            //Sender name is required by the format but the client does not decorate messages yet
            ComponentSerialization.StreamCodec.Decode(buf);
            if (buf.ReadBoolean()) ComponentSerialization.StreamCodec.Decode(buf);
            return new ClientboundDisguisedChatPacket(message, chatType - 1);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundDisguisedChatPacket value)
        {
            ComponentSerialization.StreamCodec.Encode(buf, value.Message);
            var chatType = value.ChatType is int id ? id : 0;
            buf.WriteVarInt(chatType + 1);
            ComponentSerialization.StreamCodec.Encode(buf, CommonComponents.Empty);
            buf.WriteBoolean(false);
        }
    }
}
