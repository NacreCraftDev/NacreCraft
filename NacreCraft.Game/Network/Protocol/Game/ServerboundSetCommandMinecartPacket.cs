namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundSetCommandMinecartPacket set command minecart packet, maps to vanilla ServerboundSetCommandMinecartPacket
//Fields: Entity(int), Command(String), TrackOutput(boolean)
public sealed record ServerboundSetCommandMinecartPacket(int Entity, string Command, bool TrackOutput) : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundSetCommandMinecartPacket> StreamCodec { get; } = new SetCommandMinecartCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundSetCommandMinecart;

    public void Handle(ServerGamePacketListener handler) => handler.HandleSetCommandMinecart(this);

    private sealed class SetCommandMinecartCodec : StreamCodec<FriendlyByteBuf, ServerboundSetCommandMinecartPacket>
    {
        public ServerboundSetCommandMinecartPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadVarInt(), buf.ReadString(), buf.ReadBoolean());

        public void Encode(FriendlyByteBuf buf, ServerboundSetCommandMinecartPacket value)
        {
            buf.WriteVarInt(value.Entity);
            buf.WriteString(value.Command);
            buf.WriteBoolean(value.TrackOutput);
        }
    }
}
