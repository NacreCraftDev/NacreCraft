using NacreCraft.Game.World.Effect;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//WitherSkull a wither skull projectile, maps to vanilla net.minecraft.world.entity.projectile.hurtingprojectile.WitherSkull
//Dangerous skulls travel faster and destroy blocks in vanilla; NC has no explosion engine, so the hit is approximated by an
//area magic hit and the wither effect uses the normal-difficulty duration
public sealed class WitherSkull : AbstractHurtingProjectile
{
    private bool _dangerous;

    public WitherSkull(EntityType<object> type) : base(type)
    {
    }

    public WitherSkull(EntityType<object> type, LivingEntity owner, Vec3 direction) : base(type, owner, direction)
    {
    }

    public bool IsDangerous() => _dangerous;

    public void SetDangerous(bool dangerous) => _dangerous = dangerous;

    public override double Inertia => _dangerous ? 0.73 : base.Inertia;

    public override bool ShouldBurn => false;

    protected override void OnHitEntity(ProjectileHitResult hit)
    {
        if (hit.Entity is not LivingEntity target || Level is not ServerLevel level) return;
        var owner = GetOwnerLiving();
        float damage;
        if (owner is not null)
        {
            damage = 8f;
            if (target.HurtServer(level, target.DamageSources().WitherSkull(this, owner), damage) && target.IsDeadOrDying)
                owner.Heal(5f);
        }
        else
        {
            target.HurtServer(level, target.DamageSources().Magic(), 5f);
        }
        if (!target.IsDeadOrDying)
            target.AddEffect(new MobEffectInstance(MobEffects.WITHER, 200, 1, false, true));
        Discard();
    }

    protected override void OnHit(ProjectileHitResult hit)
    {
        if (hit.Type == ProjectileHitType.entity)
        {
            base.OnHit(hit);
            return;
        }
        if (Level is not ServerLevel level)
        {
            Discard();
            return;
        }
        var box = BoundingBox.Inflate(2.0, 2.0, 2.0);
        foreach (var living in level.GetEntitiesOfClass<LivingEntity>(box))
        {
            if (living.IsDeadOrDying) continue;
            living.HurtServer(level, living.DamageSources().Magic(), 5f);
        }
        Discard();
    }

    private LivingEntity? GetOwnerLiving()
        => OwnerUuid is { } uuid && Level is ServerLevel level ? level.GetEntity(uuid) as LivingEntity : null;
}
