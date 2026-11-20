using NacreCraft.Game.World.Level.Portal;
using NacreCraft.Primitives;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Entity;

//PositionMoveRotation a position, velocity and rotation snapshot, maps to vanilla net.minecraft.world.entity.PositionMoveRotation
//Used by teleport interpolation; relative components are resolved against a source snapshot
public sealed record PositionMoveRotation(Vec3 Position, Vec3 DeltaMovement, float YRot, float XRot)
{
    //Of snapshots an entity, maps to vanilla of(Entity)
    public static PositionMoveRotation Of(NacreCraft.Registry.Entity entity)
        => new(entity.Pos, entity.Velocity, entity.YRot, entity.XRot);

    //Of snapshots a teleport transition, maps to vanilla of(TeleportTransition)
    public static PositionMoveRotation Of(TeleportTransition transition)
        => new(transition.Position, transition.DeltaMovement, transition.YRot, transition.XRot);

    public PositionMoveRotation WithRotation(float yRot, float xRot)
        => this with { YRot = yRot, XRot = xRot };

    //CalculateAbsolute resolves relative components of a teleport against the source snapshot, maps to vanilla calculateAbsolute
    public static PositionMoveRotation CalculateAbsolute(PositionMoveRotation source, PositionMoveRotation change,
        IReadOnlySet<Relative> relatives)
    {
        var offsetX = relatives.Contains(Relative.X) ? source.Position.X : 0;
        var offsetY = relatives.Contains(Relative.Y) ? source.Position.Y : 0;
        var offsetZ = relatives.Contains(Relative.Z) ? source.Position.Z : 0;
        var offsetYRot = relatives.Contains(Relative.Y_ROT) ? source.YRot : 0f;
        var offsetXRot = relatives.Contains(Relative.X_ROT) ? source.XRot : 0f;
        var absolutePosition = new Vec3(offsetX + change.Position.X, offsetY + change.Position.Y,
            offsetZ + change.Position.Z);
        var absoluteYRot = offsetYRot + change.YRot;
        var absoluteXRot = Mth.Clamp(offsetXRot + change.XRot, -90f, 90f);
        var moved = source.DeltaMovement;
        if (relatives.Contains(Relative.ROTATE_DELTA))
        {
            var diffYRot = source.YRot - absoluteYRot;
            var diffXRot = source.XRot - absoluteXRot;
            moved = RotateDelta(moved, diffXRot, diffYRot);
        }
        var absoluteDelta = new Vec3(
            CalculateDelta(moved.X, change.DeltaMovement.X, relatives, Relative.DELTA_X),
            CalculateDelta(moved.Y, change.DeltaMovement.Y, relatives, Relative.DELTA_Y),
            CalculateDelta(moved.Z, change.DeltaMovement.Z, relatives, Relative.DELTA_Z));
        return new PositionMoveRotation(absolutePosition, absoluteDelta, absoluteYRot, absoluteXRot);
    }

    private static double CalculateDelta(double currentDelta, double deltaChange, IReadOnlySet<Relative> relatives,
        Relative relative)
        => relatives.Contains(relative) ? currentDelta + deltaChange : deltaChange;

    //RotateDelta rotates a velocity by pitch then yaw in degrees
    private static Vec3 RotateDelta(Vec3 movement, float xRot, float yRot)
    {
        var xRad = xRot * Math.PI / 180.0;
        var yRad = yRot * Math.PI / 180.0;
        //Pitch rotation around X
        var y1 = movement.Y * Math.Cos(xRad) - movement.Z * Math.Sin(xRad);
        var z1 = movement.Y * Math.Sin(xRad) + movement.Z * Math.Cos(xRad);
        //Yaw rotation around Y
        var x2 = movement.X * Math.Cos(yRad) + z1 * Math.Sin(yRad);
        var z2 = -movement.X * Math.Sin(yRad) + z1 * Math.Cos(yRad);
        return new Vec3(x2, y1, z2);
    }
}
