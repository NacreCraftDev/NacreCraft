namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundSetEntityLinkPacket entity link packet, maps to vanilla ClientboundSetEntityLinkPacket
//Fields: SourceId(int), DestId(int)
public sealed record ClientboundSetEntityLinkPacket(int SourceId, int DestId) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundSetEntityLinkPacket> StreamCodec { get; } = new SetEntityLinkCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundSetEntityLink;

    public void Handle(ClientGamePacketListener handler) => handler.HandleEntityLinkPacket(this);

    private sealed class SetEntityLinkCodec : StreamCodec<FriendlyByteBuf, ClientboundSetEntityLinkPacket>
    {
        //Vanilla order: two fixed-length ints, not var ints
        public ClientboundSetEntityLinkPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadInt(), buf.ReadInt());

        public void Encode(FriendlyByteBuf buf, ClientboundSetEntityLinkPacket value)
        {
            buf.WriteInt(value.SourceId);
            buf.WriteInt(value.DestId);
        }
    }
}
