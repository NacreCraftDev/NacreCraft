using NacreCraft.Primitives;

namespace NacreCraft.Registry;

//EntityHitResult a ray that stopped on an entity, maps to vanilla net.minecraft.world.phys.EntityHitResult
public sealed class EntityHitResult : HitResult
{
    public Entity Entity { get; }

    //Without an explicit hit point the entity position is used, maps to vanilla EntityHitResult(Entity)
    public EntityHitResult(Entity entity) : this(entity, entity.Pos) { }

    public EntityHitResult(Entity entity, Vec3 location) : base(location) => Entity = entity;

    public override HitResult.Type ResultType => HitResult.Type.Entity;
}
