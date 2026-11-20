using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Entity;

//ItemOwner the owner of a flying item, maps to vanilla net.minecraft.world.entity.ItemOwner
//Carries the level, position and visual yaw; an offset owner adds a fixed offset to the wrapped owner
public interface ItemOwner
{
    object? Level { get; }
    Vec3 Position { get; }
    float VisualRotationYInDegrees { get; }

    LivingEntity? AsLivingEntity() => null;

    //OffsetFromOwner wraps an owner with a position offset, maps to vanilla ItemOwner.offsetFromOwner
    public static ItemOwner OffsetFromOwner(ItemOwner owner, Vec3 offset) => new OffsetOwner(owner, offset);

    //OffsetOwner an owner plus a position offset
    public sealed class OffsetOwner(ItemOwner owner, Vec3 offset) : ItemOwner
    {
        public ItemOwner Owner { get; } = owner;
        public Vec3 Offset { get; } = offset;
        public object? Level => Owner.Level;
        public Vec3 Position => Owner.Position.Add(Offset);
        public float VisualRotationYInDegrees => Owner.VisualRotationYInDegrees;
        public LivingEntity? AsLivingEntity() => Owner.AsLivingEntity();
    }
}
