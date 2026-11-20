using NacreCraft.Game.World.Items;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//ThrownExperienceBottle a bottle o' enchanting, maps to vanilla net.minecraft.world.entity.projectile.throwableitemprojectile.ThrownExperienceBottle
//Splashes 3..12 experience points at the hit point; NC spawns a single orb carrying the total instead of vanilla's split orbs
public sealed class ThrownExperienceBottle : ThrowableItemProjectile
{
    public ThrownExperienceBottle(EntityType<object> type) : base(type)
    {
    }

    public ThrownExperienceBottle(EntityType<object> type, LivingEntity owner, ItemStack item) : base(type, owner, item)
    {
    }

    public override double DefaultGravity => 0.07;

    protected override void OnHit(ProjectileHitResult hit)
    {
        base.OnHit(hit);
        if (Level is not ServerLevel level)
        {
            Discard();
            return;
        }
        var amount = 3 + Random.Shared.Next(5) + Random.Shared.Next(5);
        var orb = new ExperienceOrb(amount) { Pos = hit.Location };
        level.AddEntity(orb);
        Discard();
    }
}
