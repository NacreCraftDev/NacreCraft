namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundOpenBookPacket open book packet, maps to vanilla ClientboundOpenBookPacket
//Field: Hand(InteractionHand)
public sealed record ClientboundOpenBookPacket(object Hand) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundOpenBookPacket> StreamCodec { get; } = new OpenBookCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundOpenBook;

    public void Handle(ClientGamePacketListener handler) => handler.HandleOpenBook(this);

    private sealed class OpenBookCodec : StreamCodec<FriendlyByteBuf, ClientboundOpenBookPacket>
    {
        public ClientboundOpenBookPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadVarInt());

        public void Encode(FriendlyByteBuf buf, ClientboundOpenBookPacket value)
            => buf.WriteVarInt(value.Hand is int hand ? hand : 0);
    }
}
