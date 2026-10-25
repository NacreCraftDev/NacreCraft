namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundServerDataPacket server data packet, maps to vanilla ClientboundServerDataPacket
//Fields: Motd(Component), IconBytes(byte[]?, optional server icon)
public sealed record ClientboundServerDataPacket(Component Motd, byte[]? IconBytes) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundServerDataPacket> StreamCodec { get; } = new ServerDataCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundServerData;

    public void Handle(ClientGamePacketListener handler) => handler.HandleServerData(this);

    private sealed class ServerDataCodec : StreamCodec<FriendlyByteBuf, ClientboundServerDataPacket>
    {
        public ClientboundServerDataPacket Decode(FriendlyByteBuf buf)
        {
            var motd = ComponentSerialization.StreamCodec.Decode(buf);
            var icon = buf.ReadBoolean() ? buf.ReadByteArray() : null;
            return new ClientboundServerDataPacket(motd, icon);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundServerDataPacket value)
        {
            ComponentSerialization.StreamCodec.Encode(buf, value.Motd);
            buf.WriteBoolean(value.IconBytes is not null);
            if (value.IconBytes is not null) buf.WriteByteArray(value.IconBytes);
        }
    }
}
