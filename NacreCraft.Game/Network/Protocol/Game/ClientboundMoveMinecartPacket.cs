using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundMoveMinecartPacket minecart movement packet, maps to vanilla ClientboundMoveMinecartPacket
//Fields: EntityId(int), LerpSteps(List<MinecartStepData>)
public sealed record ClientboundMoveMinecartPacket(int EntityId, object LerpSteps) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundMoveMinecartPacket> StreamCodec { get; } = new MoveMinecartCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundMoveMinecartAlongTrack;

    public void Handle(ClientGamePacketListener handler) => handler.HandleMinecartAlongTrack(this);

    private sealed class MoveMinecartCodec : StreamCodec<FriendlyByteBuf, ClientboundMoveMinecartPacket>
    {
        //Vanilla MinecartStep.STREAM_CODEC: position, movement, rotation bytes for yRot/xRot, then the weight
        public ClientboundMoveMinecartPacket Decode(FriendlyByteBuf buf)
        {
            var entityId = buf.ReadVarInt();
            var count = buf.ReadVarInt();
            var steps = new List<MinecartStepData>(count);
            for (var i = 0; i < count; i++)
            {
                var position = new Vec3(buf.ReadDouble(), buf.ReadDouble(), buf.ReadDouble());
                var movement = new Vec3(buf.ReadDouble(), buf.ReadDouble(), buf.ReadDouble());
                var yRot = UnpackDegrees(buf.ReadByte());
                var xRot = UnpackDegrees(buf.ReadByte());
                var weight = buf.ReadFloat();
                steps.Add(new MinecartStepData(position, movement, yRot, xRot, weight));
            }
            return new ClientboundMoveMinecartPacket(entityId, steps);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundMoveMinecartPacket value)
        {
            buf.WriteVarInt(value.EntityId);
            var steps = value.LerpSteps as IReadOnlyList<MinecartStepData> ?? Array.Empty<MinecartStepData>();
            buf.WriteVarInt(steps.Count);
            foreach (var step in steps)
            {
                buf.WriteDouble(step.Position.X);
                buf.WriteDouble(step.Position.Y);
                buf.WriteDouble(step.Position.Z);
                buf.WriteDouble(step.Movement.X);
                buf.WriteDouble(step.Movement.Y);
                buf.WriteDouble(step.Movement.Z);
                buf.WriteByte(PackDegrees(step.YRot));
                buf.WriteByte(PackDegrees(step.XRot));
                buf.WriteFloat(step.Weight);
            }
        }

        //Rotations travel as one byte covering a full turn, matching Mth.packDegrees/unpackDegrees
        private static float UnpackDegrees(byte rot) => rot * 360.0f / 256.0f;

        private static byte PackDegrees(float angle) => (byte)(int)MathF.Floor(angle * 256.0f / 360.0f);
    }
}

//MinecartStepData one interpolation step; the rotations are already expanded to degrees
public sealed record MinecartStepData(Vec3 Position, Vec3 Movement, float YRot, float XRot, float Weight);
