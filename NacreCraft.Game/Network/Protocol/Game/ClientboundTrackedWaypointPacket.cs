using NacreCraft.Registry;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundTrackedWaypointPacket tracked waypoint packet, maps to vanilla ClientboundTrackedWaypointPacket
//Fields: Operation(0 track, 1 untrack, 2 update), Waypoint(WaypointData)
public sealed record ClientboundTrackedWaypointPacket(object Operation, object Waypoint) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundTrackedWaypointPacket> StreamCodec { get; } = new TrackedWaypointCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundWaypoint;

    public void Handle(ClientGamePacketListener handler) => handler.HandleWaypoint(this);

    private sealed class TrackedWaypointCodec : StreamCodec<FriendlyByteBuf, ClientboundTrackedWaypointPacket>
    {
        //Vanilla TrackedWaypoint: either identifier (UUID or string), the icon, then a type byte with its own contents
        public ClientboundTrackedWaypointPacket Decode(FriendlyByteBuf buf)
        {
            var operation = buf.ReadVarInt();
            var hasUuid = buf.ReadBoolean();
            var uuid = hasUuid ? buf.ReadUuid() : Guid.Empty;
            var name = hasUuid ? null : buf.ReadString();
            var style = buf.ReadIdentifier();
            int? color = buf.ReadBoolean()
                ? (buf.ReadByte() << 16) | (buf.ReadByte() << 8) | buf.ReadByte()
                : null;
            var type = buf.ReadVarInt();
            var x = 0;
            var y = 0;
            var z = 0;
            var angle = 0f;
            switch (type)
            {
                case 1:
                    x = buf.ReadVarInt();
                    y = buf.ReadVarInt();
                    z = buf.ReadVarInt();
                    break;
                case 2:
                    x = buf.ReadVarInt();
                    z = buf.ReadVarInt();
                    break;
                case 3:
                    angle = buf.ReadFloat();
                    break;
            }
            return new ClientboundTrackedWaypointPacket(operation, new WaypointData(hasUuid, uuid, name, style, color, type, x, y, z, angle));
        }

        public void Encode(FriendlyByteBuf buf, ClientboundTrackedWaypointPacket value)
        {
            if (value.Waypoint is not WaypointData waypoint)
                throw new InvalidOperationException("Waypoint payload is missing");
            buf.WriteVarInt(value.Operation is int operation ? operation : 0);
            buf.WriteBoolean(waypoint.HasUuid);
            if (waypoint.HasUuid) buf.WriteUuid(waypoint.Uuid);
            else buf.WriteString(waypoint.Name ?? "");
            buf.WriteIdentifier(waypoint.Style);
            buf.WriteBoolean(waypoint.Color.HasValue);
            if (waypoint.Color is int color)
            {
                buf.WriteByte((byte)((color >> 16) & 0xFF));
                buf.WriteByte((byte)((color >> 8) & 0xFF));
                buf.WriteByte((byte)(color & 0xFF));
            }
            buf.WriteVarInt(waypoint.Type);
            switch (waypoint.Type)
            {
                case 1:
                    buf.WriteVarInt(waypoint.X);
                    buf.WriteVarInt(waypoint.Y);
                    buf.WriteVarInt(waypoint.Z);
                    break;
                case 2:
                    buf.WriteVarInt(waypoint.X);
                    buf.WriteVarInt(waypoint.Z);
                    break;
                case 3:
                    buf.WriteFloat(waypoint.Angle);
                    break;
            }
        }
    }
}

//WaypointData one tracked waypoint; type 0 empty, 1 position, 2 chunk, 3 azimuth
public sealed record WaypointData(
    bool HasUuid,
    Guid Uuid,
    string? Name,
    Identifier Style,
    int? Color,
    int Type,
    int X,
    int Y,
    int Z,
    float Angle);
