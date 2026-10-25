using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundMoveVehiclePacket move vehicle packet, maps to vanilla ClientboundMoveVehiclePacket
//Fields: Position(Vec3), YRot(float), XRot(float)
public sealed record ClientboundMoveVehiclePacket(object Position, float YRot, float XRot) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundMoveVehiclePacket> StreamCodec { get; } = new MoveVehicleCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundMoveVehicle;

    public void Handle(ClientGamePacketListener handler) => handler.HandleMoveVehicle(this);

    private sealed class MoveVehicleCodec : StreamCodec<FriendlyByteBuf, ClientboundMoveVehiclePacket>
    {
        //Vanilla order: three doubles for the position, then yRot and xRot floats
        public ClientboundMoveVehiclePacket Decode(FriendlyByteBuf buf)
            => new(new Vec3(buf.ReadDouble(), buf.ReadDouble(), buf.ReadDouble()), buf.ReadFloat(), buf.ReadFloat());

        public void Encode(FriendlyByteBuf buf, ClientboundMoveVehiclePacket value)
        {
            var pos = value.Position is Vec3 vec ? vec : default;
            buf.WriteDouble(pos.X);
            buf.WriteDouble(pos.Y);
            buf.WriteDouble(pos.Z);
            buf.WriteFloat(value.YRot);
            buf.WriteFloat(value.XRot);
        }
    }
}
