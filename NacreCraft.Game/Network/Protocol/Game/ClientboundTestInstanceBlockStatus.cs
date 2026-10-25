using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundTestInstanceBlockStatus test instance block status packet, maps to vanilla ClientboundTestInstanceBlockStatus
//Fields: Status(Component), Size(Vec3i or null)
public sealed record ClientboundTestInstanceBlockStatus(object Status, object Size) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundTestInstanceBlockStatus> StreamCodec { get; } = new TestInstanceBlockStatusCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundTestInstanceBlockStatus;

    public void Handle(ClientGamePacketListener handler) => handler.HandleTestInstanceBlockStatus(this);

    private sealed class TestInstanceBlockStatusCodec : StreamCodec<FriendlyByteBuf, ClientboundTestInstanceBlockStatus>
    {
        public ClientboundTestInstanceBlockStatus Decode(FriendlyByteBuf buf)
        {
            var status = ComponentSerialization.StreamCodec.Decode(buf);
            object size = buf.ReadBoolean()
                ? new Vec3i(buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadVarInt())
                : null!;
            return new ClientboundTestInstanceBlockStatus(status, size);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundTestInstanceBlockStatus value)
        {
            if (value.Status is not Component status)
                throw new InvalidOperationException("Test instance status payload is missing");
            ComponentSerialization.StreamCodec.Encode(buf, status);
            buf.WriteBoolean(value.Size is Vec3i);
            if (value.Size is Vec3i size)
            {
                buf.WriteVarInt(size.X);
                buf.WriteVarInt(size.Y);
                buf.WriteVarInt(size.Z);
            }
        }
    }
}
