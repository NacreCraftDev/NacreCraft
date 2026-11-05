using NacreCraft.Game.World.Waypoints;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundTrackedWaypointPacket tracked waypoint packet, maps to vanilla ClientboundTrackedWaypointPacket
//Fields: operation plus a TrackedWaypoint whose type decides the contents
public sealed record ClientboundTrackedWaypointPacket(Operation Operation, TrackedWaypoint Waypoint) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundTrackedWaypointPacket> StreamCodec { get; } = new TrackedWaypointPacketCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundWaypoint;

    public void Handle(ClientGamePacketListener handler) => handler.HandleWaypoint(this);

    public static ClientboundTrackedWaypointPacket RemoveWaypoint(Guid identifier)
        => new(Operation.UNTRACK, TrackedWaypoint.Empty(identifier));

    public static ClientboundTrackedWaypointPacket AddWaypointPosition(Guid identifier, Waypoint.Icon icon, BlockPos position)
        => new(Operation.TRACK, TrackedWaypoint.SetPosition(identifier, icon, position.AsVec3i()));

    public static ClientboundTrackedWaypointPacket UpdateWaypointPosition(Guid identifier, Waypoint.Icon icon, BlockPos position)
        => new(Operation.UPDATE, TrackedWaypoint.SetPosition(identifier, icon, position.AsVec3i()));

    public static ClientboundTrackedWaypointPacket AddWaypointChunk(Guid identifier, Waypoint.Icon icon, ChunkPos chunk)
        => new(Operation.TRACK, TrackedWaypoint.SetChunk(identifier, icon, chunk));

    public static ClientboundTrackedWaypointPacket UpdateWaypointChunk(Guid identifier, Waypoint.Icon icon, ChunkPos chunk)
        => new(Operation.UPDATE, TrackedWaypoint.SetChunk(identifier, icon, chunk));

    public static ClientboundTrackedWaypointPacket AddWaypointAzimuth(Guid identifier, Waypoint.Icon icon, float angle)
        => new(Operation.TRACK, TrackedWaypoint.SetAzimuth(identifier, icon, angle));

    public static ClientboundTrackedWaypointPacket UpdateWaypointAzimuth(Guid identifier, Waypoint.Icon icon, float angle)
        => new(Operation.UPDATE, TrackedWaypoint.SetAzimuth(identifier, icon, angle));

    //Apply routes the operation into a client-side manager, maps to vanilla apply
    public void Apply(TrackedWaypointManager manager)
    {
        switch (Operation)
        {
            case Operation.TRACK:
                manager.TrackWaypoint(Waypoint);
                break;
            case Operation.UNTRACK:
                manager.UntrackWaypoint(Waypoint);
                break;
            case Operation.UPDATE:
                manager.UpdateWaypoint(Waypoint);
                break;
        }
    }

    private sealed class TrackedWaypointPacketCodec : StreamCodec<FriendlyByteBuf, ClientboundTrackedWaypointPacket>
    {
        public ClientboundTrackedWaypointPacket Decode(FriendlyByteBuf buf)
            => new((Operation)buf.ReadVarInt(), TrackedWaypoint.STREAM_CODEC.Decode(buf));

        public void Encode(FriendlyByteBuf buf, ClientboundTrackedWaypointPacket value)
        {
            buf.WriteVarInt((int)value.Operation);
            TrackedWaypoint.STREAM_CODEC.Encode(buf, value.Waypoint);
        }
    }
}

//Operation what to do with the waypoint, maps to vanilla Operation
public enum Operation
{
    TRACK,
    UNTRACK,
    UPDATE
}
