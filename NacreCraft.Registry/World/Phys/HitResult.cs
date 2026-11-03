using NacreCraft.Primitives;

namespace NacreCraft.Registry;

//HitResult the base of every ray trace result, maps to vanilla net.minecraft.world.phys.HitResult
//Holds the exact hit point plus the kind of thing that was hit; the concrete subclasses carry the extra data
public abstract class HitResult
{
    //Location the exact hit point in world space
    public Vec3 Location { get; }

    protected HitResult(Vec3 location) => Location = location;

    //DistanceTo squared distance from the hit point to the entity position, maps to vanilla distanceTo
    public double DistanceTo(Entity entity)
    {
        var dx = Location.X - entity.Pos.X;
        var dy = Location.Y - entity.Pos.Y;
        var dz = Location.Z - entity.Pos.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    //ResultType what the ray hit, maps to vanilla getType
    public abstract HitResult.Type ResultType { get; }

    //Type the kinds of hit a ray trace can produce, maps to vanilla HitResult.Type
    public enum Type
    {
        Miss,
        Block,
        Entity,
    }
}
