using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDamageEventPacket damage event packet, maps to vanilla ClientboundDamageEventPacket
//SourceType is the damage type registry id, SourcePosition is a Vec3 when the damage has an explicit source position
public sealed record ClientboundDamageEventPacket(int EntityId, object SourceType, int SourceCauseId, int SourceDirectId, object SourcePosition) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundDamageEventPacket> StreamCodec { get; } = new DamageEventCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDamageEvent;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDamageEvent(this);

    private sealed class DamageEventCodec : StreamCodec<FriendlyByteBuf, ClientboundDamageEventPacket>
    {
        public ClientboundDamageEventPacket Decode(FriendlyByteBuf buf)
        {
            var entityId = buf.ReadVarInt();
            //DamageType uses a plain registry id, the holderRegistry codec has no inline form
            var sourceType = buf.ReadVarInt();
            //Optional entity ids are written as id + 1 so -1 can mean "absent"
            var sourceCauseId = buf.ReadVarInt() - 1;
            var sourceDirectId = buf.ReadVarInt() - 1;
            object sourcePosition;
            if (buf.ReadBoolean())
                sourcePosition = new Vec3(buf.ReadDouble(), buf.ReadDouble(), buf.ReadDouble());
            else
                sourcePosition = null!;
            return new ClientboundDamageEventPacket(entityId, sourceType, sourceCauseId, sourceDirectId, sourcePosition);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundDamageEventPacket value)
        {
            buf.WriteVarInt(value.EntityId);
            buf.WriteVarInt(value.SourceType is int sourceType ? sourceType : 0);
            buf.WriteVarInt(value.SourceCauseId + 1);
            buf.WriteVarInt(value.SourceDirectId + 1);
            if (value.SourcePosition is Vec3 position)
            {
                buf.WriteBoolean(true);
                buf.WriteDouble(position.X);
                buf.WriteDouble(position.Y);
                buf.WriteDouble(position.Z);
            }
            else
            {
                buf.WriteBoolean(false);
            }
        }
    }
}
