namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundProjectilePowerPacket projectile power packet, maps to vanilla ClientboundProjectilePowerPacket
//Fields: Id(int), AccelerationPower(double)
public sealed record ClientboundProjectilePowerPacket(int Id, double AccelerationPower) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundProjectilePowerPacket> StreamCodec { get; } = new ProjectilePowerCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundProjectilePower;

    public void Handle(ClientGamePacketListener handler) => handler.HandleProjectilePowerPacket(this);

    private sealed class ProjectilePowerCodec : StreamCodec<FriendlyByteBuf, ClientboundProjectilePowerPacket>
    {
        public ClientboundProjectilePowerPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadVarInt(), buf.ReadDouble());

        public void Encode(FriendlyByteBuf buf, ClientboundProjectilePowerPacket value)
        {
            buf.WriteVarInt(value.Id);
            buf.WriteDouble(value.AccelerationPower);
        }
    }
}
