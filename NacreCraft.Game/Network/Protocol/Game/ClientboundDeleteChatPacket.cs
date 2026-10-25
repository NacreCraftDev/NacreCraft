namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDeleteChatPacket delete chat packet, maps to vanilla ClientboundDeleteChatPacket
//Field: MessageSignature(PackedMessageSignature)
public sealed record ClientboundDeleteChatPacket(object MessageSignature) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundDeleteChatPacket> StreamCodec { get; } = new DeleteChatCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDeleteChat;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDeleteChat(this);

    private sealed class DeleteChatCodec : StreamCodec<FriendlyByteBuf, ClientboundDeleteChatPacket>
    {
        //Vanilla MessageSignature.Packed: a VAR_INT id + 1, where 0 means a full 256-byte signature follows
        public ClientboundDeleteChatPacket Decode(FriendlyByteBuf buf)
        {
            var id = buf.ReadVarInt() - 1;
            if (id == -1)
            {
                var fullSignature = buf.ReadBytes(256);
                return new ClientboundDeleteChatPacket(new PackedMessageSignature(-1, fullSignature));
            }
            return new ClientboundDeleteChatPacket(new PackedMessageSignature(id, null));
        }

        public void Encode(FriendlyByteBuf buf, ClientboundDeleteChatPacket value)
        {
            var signature = value.MessageSignature as PackedMessageSignature
                ?? throw new InvalidOperationException("Delete chat signature payload is missing");
            buf.WriteVarInt(signature.Id + 1);
            if (signature.FullSignature is not null) buf.WriteBytes(signature.FullSignature);
        }
    }
}

//PackedMessageSignature either refers to a cached signature by id or carries the full 256-byte signature
public sealed record PackedMessageSignature(int Id, byte[]? FullSignature);
