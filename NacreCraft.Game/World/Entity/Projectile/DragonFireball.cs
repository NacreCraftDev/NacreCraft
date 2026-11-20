using NacreCraft.Game.World.Effect;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//DragonFireball a dragon fireball that leaves a lingering cloud, maps to vanilla net.minecraft.world.entity.projectile.hurtingprojectile.DragonFireball
//On impact it spawns a radius-3 cloud with instant damage; NC has no custom particle option, so the cloud keeps its default
public sealed class DragonFireball : AbstractHurtingProjectile
{
    public const float SplashRange = 4.0f;

    public DragonFireball(EntityType<object> type) : base(type)
    {
    }

    public DragonFireball(EntityType<object> type, LivingEntity owner, Vec3 direction) : base(type, owner, direction)
    {
    }

    public override bool ShouldBurn => false;

    protected override void OnHit(ProjectileHitResult hit)
    {
        if (Level is not ServerLevel level) return;
        var cloud = new AreaEffectCloud
        {
            Radius = 3f,
            Duration = 600,
            Owner = OwnerUuid is { } uuid ? level.GetEntity(uuid) as Player : null,
        };
        cloud.Pos = Pos;
        cloud.RadiusPerTick = (7f - cloud.Radius) / cloud.Duration;
        cloud.AddEffect(new MobEffectInstance(MobEffects.INSTANT_DAMAGE, 1, 1, false, true));
        level.AddEntity(cloud);
        Discard();
    }
}
