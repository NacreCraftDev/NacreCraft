using NacreCraft.DataFixer.Util;
using NacreCraft.Logging;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util;
using NcEntity = NacreCraft.Registry.Entity;

namespace NacreCraft.Game.World.Waypoints;

//TrackedWaypoint a waypoint as sent to a client, maps to vanilla net.minecraft.world.waypoints.TrackedWaypoint
//The identifier is either a UUID (entities) or a string (other sources); the type decides what the contents mean
public abstract class TrackedWaypoint : Waypoint
{
    //STREAM_CODEC network codec for the whole tracked waypoint, maps to vanilla STREAM_CODEC
    public static readonly StreamCodec<FriendlyByteBuf, TrackedWaypoint> STREAM_CODEC = new TrackedWaypointCodec();

    protected readonly Either<Guid, string> Identifier;
    private readonly Waypoint.Icon _icon;
    private readonly Type _type;

    private TrackedWaypoint(Either<Guid, string> identifier, Waypoint.Icon icon, Type type)
    {
        Identifier = identifier;
        _icon = icon;
        _type = type;
    }

    //Id the identifier, maps to vanilla id
    public Either<Guid, string> Id() => Identifier;

    public abstract void Update(TrackedWaypoint other);

    //Write serializes the identifier, icon, type and contents, maps to vanilla write
    public void Write(FriendlyByteBuf buf)
    {
        buf.WriteBoolean(Identifier.IsLeft);
        if (Identifier.IsLeft) buf.WriteUuid(Identifier.GetLeft().Get());
        else buf.WriteString(Identifier.GetRight().Get());
        Waypoint.Icon.STREAM_CODEC.Encode(buf, _icon);
        buf.WriteVarInt((int)_type);
        WriteContents(buf);
    }

    public abstract void WriteContents(FriendlyByteBuf buf);

    private static TrackedWaypoint Read(FriendlyByteBuf buf)
    {
        Either<Guid, string> identifier = buf.ReadBoolean()
            ? Either<Guid, string>.Left(buf.ReadUuid())
            : Either<Guid, string>.Right(buf.ReadString());
        var icon = Waypoint.Icon.STREAM_CODEC.Decode(buf);
        var type = (Type)buf.ReadVarInt();
        return type switch
        {
            Type.EMPTY => new EmptyWaypoint(identifier, icon, buf),
            Type.VEC3I => new Vec3iWaypoint(identifier, icon, buf),
            Type.CHUNK => new ChunkWaypoint(identifier, icon, buf),
            Type.AZIMUTH => new AzimuthWaypoint(identifier, icon, buf),
            _ => throw new InvalidOperationException($"Unknown tracked waypoint type {type}")
        };
    }

    public static TrackedWaypoint SetPosition(Guid identifier, Waypoint.Icon icon, Vec3i position)
        => new Vec3iWaypoint(identifier, icon, position);

    public static TrackedWaypoint SetChunk(Guid identifier, Waypoint.Icon icon, ChunkPos chunk)
        => new ChunkWaypoint(identifier, icon, chunk);

    public static TrackedWaypoint SetAzimuth(Guid identifier, Waypoint.Icon icon, float angle)
        => new AzimuthWaypoint(identifier, icon, angle);

    public static TrackedWaypoint Empty(Guid identifier) => new EmptyWaypoint(identifier);

    public abstract double YawAngleToCamera(ServerLevel level, Camera camera, PartialTickSupplier partialTickSupplier);

    public abstract PitchDirection PitchDirectionToCamera(ServerLevel level, Projector projector, PartialTickSupplier partialTickSupplier);

    public abstract double DistanceSquared(NcEntity fromEntity);

    //Icon the icon sent with this waypoint, maps to vanilla icon
    public Waypoint.Icon Icon() => _icon;

    private enum Type
    {
        EMPTY,
        VEC3I,
        CHUNK,
        AZIMUTH
    }

    //Vec3iWaypoint a waypoint fixed at a block position, maps to vanilla Vec3iWaypoint
    private sealed class Vec3iWaypoint : TrackedWaypoint
    {
        private Vec3i _vector;

        public Vec3iWaypoint(Guid identifier, Waypoint.Icon icon, Vec3i vector)
            : base(Either<Guid, string>.Left(identifier), icon, Type.VEC3I)
        {
            _vector = vector;
        }

        public Vec3iWaypoint(Either<Guid, string> identifier, Waypoint.Icon icon, FriendlyByteBuf buf)
            : base(identifier, icon, Type.VEC3I)
        {
            _vector = new Vec3i(buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadVarInt());
        }

        public override void Update(TrackedWaypoint other)
        {
            if (other is Vec3iWaypoint waypoint) _vector = waypoint._vector;
            else Log.Warning($"Unsupported Waypoint update operation: {other.GetType().Name}");
        }

        public override void WriteContents(FriendlyByteBuf buf)
        {
            buf.WriteVarInt(_vector.X);
            buf.WriteVarInt(_vector.Y);
            buf.WriteVarInt(_vector.Z);
        }

        //Position resolves the waypoint to a world position, maps to vanilla position
        //An identified NcEntity within three blocks supplies its eye position; anything else falls back to the block center
        private Vec3 Position(ServerLevel level, PartialTickSupplier partialTick)
        {
            var entityId = Identifier.GetLeft();
            if (entityId.IsPresent)
            {
                var entity = level.GetEntity(entityId.Get());
                if (entity is not null)
                {
                    var blockPos = new BlockPos(Mth.Floor(entity.Pos.X), Mth.Floor(entity.Pos.Y), Mth.Floor(entity.Pos.Z));
                    if (blockPos.AsVec3i().DistManhattan(_vector) <= 3)
                        return entity.GetEyePosition();
                }
            }
            return Vec3.AtCenterOf(_vector);
        }

        public override double YawAngleToCamera(ServerLevel level, Camera camera, PartialTickSupplier partialTickSupplier)
        {
            var direction = camera.Position().Subtract(Position(level, partialTickSupplier)).RotateClockwise90();
            var waypointAngle = (float)Mth.Atan2(direction.Z, direction.X) * 57.295776f;
            return Mth.DegreesDifference(camera.Yaw(), waypointAngle);
        }

        public override PitchDirection PitchDirectionToCamera(ServerLevel level, Projector projector, PartialTickSupplier partialTickSupplier)
        {
            var pointOnScreen = projector.ProjectPointToScreen(Position(level, partialTickSupplier));
            var isBehindCamera = pointOnScreen.Z > 1.0;
            var yInFrontOfCamera = isBehindCamera ? -pointOnScreen.Y : pointOnScreen.Y;
            if (yInFrontOfCamera < -1.0) return PitchDirection.DOWN;
            if (yInFrontOfCamera > 1.0) return PitchDirection.UP;
            if (isBehindCamera)
            {
                if (pointOnScreen.Y > 0.0) return PitchDirection.UP;
                if (pointOnScreen.Y < 0.0) return PitchDirection.DOWN;
            }
            return PitchDirection.NONE;
        }

        public override double DistanceSquared(NcEntity fromEntity)
            => fromEntity.Pos.DistanceToSqr(Vec3.AtCenterOf(_vector));
    }

    //ChunkWaypoint a waypoint fixed at a chunk column, maps to vanilla ChunkWaypoint
    private sealed class ChunkWaypoint : TrackedWaypoint
    {
        private ChunkPos _chunkPos;

        public ChunkWaypoint(Guid identifier, Waypoint.Icon icon, ChunkPos chunkPos)
            : base(Either<Guid, string>.Left(identifier), icon, Type.CHUNK)
        {
            _chunkPos = chunkPos;
        }

        public ChunkWaypoint(Either<Guid, string> identifier, Waypoint.Icon icon, FriendlyByteBuf buf)
            : base(identifier, icon, Type.CHUNK)
        {
            _chunkPos = new ChunkPos(buf.ReadVarInt(), buf.ReadVarInt());
        }

        public override void Update(TrackedWaypoint other)
        {
            if (other is ChunkWaypoint waypoint) _chunkPos = waypoint._chunkPos;
            else Log.Warning($"Unsupported Waypoint update operation: {other.GetType().Name}");
        }

        public override void WriteContents(FriendlyByteBuf buf)
        {
            buf.WriteVarInt(_chunkPos.X);
            buf.WriteVarInt(_chunkPos.Z);
        }

        private Vec3 Position(double positionY)
            => Vec3.AtCenterOf(_chunkPos.GetMiddleBlockPosition((int)positionY));

        public override double YawAngleToCamera(ServerLevel level, Camera camera, PartialTickSupplier partialTickSupplier)
        {
            var cameraPosition = camera.Position();
            var direction = cameraPosition.Subtract(Position(cameraPosition.Y)).RotateClockwise90();
            var waypointAngle = (float)Mth.Atan2(direction.Z, direction.X) * 57.295776f;
            return Mth.DegreesDifference(camera.Yaw(), waypointAngle);
        }

        public override PitchDirection PitchDirectionToCamera(ServerLevel level, Projector projector, PartialTickSupplier partialTickSupplier)
        {
            var onScreenHorizon = projector.ProjectHorizonToScreen();
            if (onScreenHorizon < -1.0) return PitchDirection.DOWN;
            if (onScreenHorizon > 1.0) return PitchDirection.UP;
            return PitchDirection.NONE;
        }

        public override double DistanceSquared(NcEntity fromEntity)
            => fromEntity.Pos.DistanceToSqr(
                Vec3.AtCenterOf(_chunkPos.GetMiddleBlockPosition(Mth.Floor(fromEntity.Pos.Y))));
    }

    //AzimuthWaypoint a waypoint given only as an angle, maps to vanilla AzimuthWaypoint
    private sealed class AzimuthWaypoint : TrackedWaypoint
    {
        private float _angle;

        public AzimuthWaypoint(Guid identifier, Waypoint.Icon icon, float angle)
            : base(Either<Guid, string>.Left(identifier), icon, Type.AZIMUTH)
        {
            _angle = angle;
        }

        public AzimuthWaypoint(Either<Guid, string> identifier, Waypoint.Icon icon, FriendlyByteBuf buf)
            : base(identifier, icon, Type.AZIMUTH)
        {
            _angle = buf.ReadFloat();
        }

        public override void Update(TrackedWaypoint other)
        {
            if (other is AzimuthWaypoint waypoint) _angle = waypoint._angle;
            else Log.Warning($"Unsupported Waypoint update operation: {other.GetType().Name}");
        }

        public override void WriteContents(FriendlyByteBuf buf) => buf.WriteFloat(_angle);

        public override double YawAngleToCamera(ServerLevel level, Camera camera, PartialTickSupplier partialTickSupplier)
            => Mth.DegreesDifference(camera.Yaw(), _angle * 57.295776f);

        public override PitchDirection PitchDirectionToCamera(ServerLevel level, Projector projector, PartialTickSupplier partialTickSupplier)
        {
            var horizon = projector.ProjectHorizonToScreen();
            if (horizon < -1.0) return PitchDirection.DOWN;
            if (horizon > 1.0) return PitchDirection.UP;
            return PitchDirection.NONE;
        }

        public override double DistanceSquared(NcEntity fromEntity) => double.PositiveInfinity;
    }

    //EmptyWaypoint a waypoint carrying no position, maps to vanilla EmptyWaypoint
    private sealed class EmptyWaypoint : TrackedWaypoint
    {
        public EmptyWaypoint(Either<Guid, string> identifier, Waypoint.Icon icon, FriendlyByteBuf buf)
            : base(identifier, icon, Type.EMPTY)
        {
        }

        public EmptyWaypoint(Guid identifier)
            : base(Either<Guid, string>.Left(identifier), Waypoint.Icon.NULL, Type.EMPTY)
        {
        }

        public override void Update(TrackedWaypoint other) { }

        public override void WriteContents(FriendlyByteBuf buf) { }

        public override double YawAngleToCamera(ServerLevel level, Camera camera, PartialTickSupplier partialTickSupplier)
            => double.NaN;

        public override PitchDirection PitchDirectionToCamera(ServerLevel level, Projector projector, PartialTickSupplier partialTickSupplier)
            => PitchDirection.NONE;

        public override double DistanceSquared(NcEntity fromEntity) => double.PositiveInfinity;
    }

    //Camera the camera view needed to aim a waypoint, maps to vanilla TrackedWaypoint.Camera
    public interface Camera
    {
        float Yaw();

        Vec3 Position();
    }

    //Projector maps world points onto the screen, maps to vanilla TrackedWaypoint.Projector
    public interface Projector
    {
        Vec3 ProjectPointToScreen(Vec3 position);

        double ProjectHorizonToScreen();
    }

    public enum PitchDirection
    {
        NONE,
        UP,
        DOWN
    }

    //TrackedWaypointCodec reads and writes a tracked waypoint by type id, maps to vanilla STREAM_CODEC
    private sealed class TrackedWaypointCodec : StreamCodec<FriendlyByteBuf, TrackedWaypoint>
    {
        public TrackedWaypoint Decode(FriendlyByteBuf buf) => Read(buf);

        public void Encode(FriendlyByteBuf buf, TrackedWaypoint value) => value.Write(buf);
    }
}

