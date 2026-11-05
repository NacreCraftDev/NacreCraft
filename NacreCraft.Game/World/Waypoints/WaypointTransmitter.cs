using NacreCraft.Codec;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Entity;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.EntityAttribute;
using NacreCraft.Util;
using NcEntity = NacreCraft.Registry.Entity;

namespace NacreCraft.Game.World.Waypoints;

//WaypointTransmitter an NcEntity that can transmit a waypoint to players, maps to vanilla net.minecraft.world.waypoints.WaypointTransmitter
public interface WaypointTransmitter : Waypoint
{
    public const int REALLY_FAR_DISTANCE = 332;

    bool IsTransmittingWaypoint();

    Optional<Connection> MakeWaypointConnectionWith(ServerPlayer player);

    Waypoint.Icon WaypointIcon();

    //DoesSourceIgnoreReceiver whether the source should not be shown to the receiver, maps to vanilla doesSourceIgnoreReceiver
    //The passenger check has no counterpart yet because the rider hierarchy is not ported, so only spectators are excluded
    public static bool DoesSourceIgnoreReceiver(LivingEntity source, ServerPlayer receiver)
    {
        if (receiver.IsSpectator()) return false;
        if (source.IsSpectator()) return true;
        var broadcastRange = Math.Min(
            source.GetAttributeValue(Attributes.WaypointTransmitRange),
            receiver.GetAttributeValue(Attributes.WaypointReceiveRange));
        return source.Pos.DistanceTo(receiver.Pos) >= broadcastRange;
    }

    //IsChunkVisible whether the receiver's chunk view already covers the chunk, maps to vanilla isChunkVisible
    public static bool IsChunkVisible(ChunkPos chunkPos, ServerPlayer receiver)
        => receiver.ChunkSender?.IsInViewDistance(chunkPos.X, chunkPos.Z) ?? false;

    //IsReallyFar whether the source is beyond the always-visible distance, maps to vanilla isReallyFar
    public static bool IsReallyFar(LivingEntity source, ServerPlayer receiver)
        => source.Pos.DistanceTo(receiver.Pos) > REALLY_FAR_DISTANCE;

    //ChunkOf the chunk an NcEntity stands in, maps to vanilla Entity.chunkPosition
    public static ChunkPos ChunkOf(NcEntity entity)
        => new(Mth.Floor(entity.Pos.X) >> 4, Mth.Floor(entity.Pos.Z) >> 4);

    //BlockPositionOf the block an NcEntity stands in, maps to vanilla Entity.blockPosition
    public static BlockPos BlockPositionOf(NcEntity entity)
        => new(Mth.Floor(entity.Pos.X), Mth.Floor(entity.Pos.Y), Mth.Floor(entity.Pos.Z));

    //Connection a live waypoint link to one receiver, maps to vanilla WaypointTransmitter.Connection
    public interface Connection
    {
        void Connect();

        void Disconnect();

        void Update();

        bool IsBroken();
    }

    //ChunkConnection a connection whose payload is a chunk position, maps to vanilla ChunkConnection
    public interface ChunkConnection : Connection
    {
        int DistanceChessboard();
    }

    //BlockConnection a connection whose payload is a block position, maps to vanilla BlockConnection
    public interface BlockConnection : Connection
    {
        int DistanceManhattan();
    }

    //EntityAzimuthConnection keeps only the angle to the source, maps to vanilla EntityAzimuthConnection
    public sealed class EntityAzimuthConnection : Connection
    {
        private readonly LivingEntity _source;
        private readonly Waypoint.Icon _icon;
        private readonly ServerPlayer _receiver;
        private float _lastAngle;

        public EntityAzimuthConnection(LivingEntity source, Waypoint.Icon icon, ServerPlayer receiver)
        {
            _source = source;
            _icon = icon;
            _receiver = receiver;
            var direction = receiver.Pos.Subtract(source.Pos).RotateClockwise90();
            _lastAngle = (float)Mth.Atan2(direction.Z, direction.X);
        }

        public bool IsBroken()
            => DoesSourceIgnoreReceiver(_source, _receiver)
               || IsChunkVisible(ChunkOf(_source), _receiver)
               || !IsReallyFar(_source, _receiver);

        public void Connect()
            => _receiver.Connection.Send(ClientboundTrackedWaypointPacket.AddWaypointAzimuth(_source.Uuid, _icon, _lastAngle));

        public void Disconnect()
            => _receiver.Connection.Send(ClientboundTrackedWaypointPacket.RemoveWaypoint(_source.Uuid));

        public void Update()
        {
            var direction = _receiver.Pos.Subtract(_source.Pos).RotateClockwise90();
            var currentAngle = (float)Mth.Atan2(direction.Z, direction.X);
            if (Mth.Abs(currentAngle - _lastAngle) > (float)Math.PI / 360)
            {
                _receiver.Connection.Send(ClientboundTrackedWaypointPacket.UpdateWaypointAzimuth(_source.Uuid, _icon, currentAngle));
                _lastAngle = currentAngle;
            }
        }
    }

    //EntityChunkConnection sends the chunk the source stands in, maps to vanilla EntityChunkConnection
    public sealed class EntityChunkConnection : ChunkConnection
    {
        private readonly LivingEntity _source;
        private readonly Waypoint.Icon _icon;
        private readonly ServerPlayer _receiver;
        private ChunkPos _lastPosition;

        public EntityChunkConnection(LivingEntity source, Waypoint.Icon icon, ServerPlayer receiver)
        {
            _source = source;
            _icon = icon;
            _receiver = receiver;
            _lastPosition = ChunkOf(source);
        }

        public int DistanceChessboard() => _lastPosition.GetChessboardDistance(ChunkOf(_source));

        public void Connect()
            => _receiver.Connection.Send(ClientboundTrackedWaypointPacket.AddWaypointChunk(_source.Uuid, _icon, _lastPosition));

        public void Disconnect()
            => _receiver.Connection.Send(ClientboundTrackedWaypointPacket.RemoveWaypoint(_source.Uuid));

        public void Update()
        {
            var currentPosition = ChunkOf(_source);
            if (currentPosition.GetChessboardDistance(_lastPosition) > 0)
            {
                _receiver.Connection.Send(ClientboundTrackedWaypointPacket.UpdateWaypointChunk(_source.Uuid, _icon, currentPosition));
                _lastPosition = currentPosition;
            }
        }

        public bool IsBroken()
        {
            if (DistanceChessboard() > 1 || DoesSourceIgnoreReceiver(_source, _receiver)) return true;
            return IsChunkVisible(_lastPosition, _receiver);
        }
    }

    //EntityBlockConnection sends the block the source stands in, maps to vanilla EntityBlockConnection
    public sealed class EntityBlockConnection : BlockConnection
    {
        private readonly LivingEntity _source;
        private readonly Waypoint.Icon _icon;
        private readonly ServerPlayer _receiver;
        private BlockPos _lastPosition;

        public EntityBlockConnection(LivingEntity source, Waypoint.Icon icon, ServerPlayer receiver)
        {
            _source = source;
            _receiver = receiver;
            _icon = icon;
            _lastPosition = BlockPositionOf(source);
        }

        public void Connect()
            => _receiver.Connection.Send(ClientboundTrackedWaypointPacket.AddWaypointPosition(_source.Uuid, _icon, _lastPosition));

        public void Disconnect()
            => _receiver.Connection.Send(ClientboundTrackedWaypointPacket.RemoveWaypoint(_source.Uuid));

        public void Update()
        {
            var currentPosition = BlockPositionOf(_source);
            if (currentPosition.AsVec3i().DistManhattan(_lastPosition.AsVec3i()) > 0)
            {
                _receiver.Connection.Send(ClientboundTrackedWaypointPacket.UpdateWaypointPosition(_source.Uuid, _icon, currentPosition));
                _lastPosition = currentPosition;
            }
        }

        public int DistanceManhattan()
            => (int)_lastPosition.AsVec3i().DistManhattan(BlockPositionOf(_source).AsVec3i());

        public bool IsBroken()
            => DistanceManhattan() > 1 || DoesSourceIgnoreReceiver(_source, _receiver);
    }
}


