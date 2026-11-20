using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//Fireball a fireball carrying an item stack, maps to vanilla net.minecraft.world.entity.projectile.hurtingprojectile.Fireball
//The stack is what the client renders and what the server reads for the item form; NC has no synced data yet, so it is a plain field
public abstract class Fireball : AbstractHurtingProjectile, ItemSupplier
{
    private ItemStack _item;

    protected Fireball(EntityType<object> type) : base(type)
    {
        _item = ItemSupplier.DefaultStack("fire_charge");
    }

    protected Fireball(EntityType<object> type, double x, double y, double z, Vec3 direction) : base(type, x, y, z, direction)
    {
        _item = ItemSupplier.DefaultStack("fire_charge");
    }

    protected Fireball(EntityType<object> type, LivingEntity owner, Vec3 direction) : base(type, owner, direction)
    {
        _item = ItemSupplier.DefaultStack("fire_charge");
    }

    public ItemStack GetItem() => _item;

    public void SetItem(ItemStack stack) => _item = stack.IsEmpty() ? ItemSupplier.DefaultStack("fire_charge") : stack.CopyWithCount(1);
}

//LargeFireball a ghast fireball, maps to vanilla net.minecraft.world.entity.projectile.hurtingprojectile.LargeFireball
//NC has no explosion engine, so the blast is approximated by direct fireball damage to every living entity in the blast radius
public sealed class LargeFireball : Fireball
{
    private const float DirectDamage = 6f;

    public LargeFireball(EntityType<object> type) : base(type)
    {
    }

    public LargeFireball(EntityType<object> type, LivingEntity owner, Vec3 direction, int explosionPower)
        : base(type, owner, direction)
    {
        ExplosionPower = explosionPower;
    }

    public int ExplosionPower { get; private set; } = 1;

    public void SetExplosionPower(int power) => ExplosionPower = power;

    protected override void OnHit(ProjectileHitResult hit)
    {
        if (Level is not ServerLevel level) return;
        var box = BoundingBox.Inflate(ExplosionPower + 1.0, ExplosionPower + 1.0, ExplosionPower + 1.0);
        foreach (var living in level.GetEntitiesOfClass<LivingEntity>(box))
        {
            if (living.IsDeadOrDying) continue;
            living.HurtServer(level, living.DamageSources().Fireball(this, GetOwnerLiving()), DirectDamage);
        }
        Discard();
    }

    private LivingEntity? GetOwnerLiving()
        => OwnerUuid is { } uuid && Level is ServerLevel level ? level.GetEntity(uuid) as LivingEntity : null;
}
