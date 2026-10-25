namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundTagQueryPacket tag query packet, maps to vanilla ClientboundTagQueryPacket
//Fields: TransactionId(int), Tag(CompoundTag)
public sealed record ClientboundTagQueryPacket(int TransactionId, object Tag) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundTagQueryPacket> StreamCodec { get; } = new TagQueryCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundTagQuery;

    public void Handle(ClientGamePacketListener handler) => handler.HandleTagQueryPacket(this);

    private sealed class TagQueryCodec : StreamCodec<FriendlyByteBuf, ClientboundTagQueryPacket>
    {
        //Vanilla order: VAR_INT transaction id + root NBT tag
        public ClientboundTagQueryPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadVarInt(), buf.ReadNbt());

        public void Encode(FriendlyByteBuf buf, ClientboundTagQueryPacket value)
        {
            buf.WriteVarInt(value.TransactionId);
            buf.WriteNbt(value.Tag as NacreCraft.Nbt.Tag);
        }
    }
}
