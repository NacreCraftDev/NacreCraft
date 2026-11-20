using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//ThrownSplashPotion a splash potion, maps to vanilla net.minecraft.world.entity.projectile.throwableitemprojectile.ThrownSplashPotion
//Effects are applied to every living entity within four blocks, scaled down with distance; instant effects use the scaled amplifier path
public sealed class ThrownSplashPotion : AbstractThrownPotion
{
    public ThrownSplashPotion(EntityType<object> type) : base(type)
    {
    }

    public ThrownSplashPotion(EntityType<object> type, LivingEntity owner, ItemStack item) : base(type, owner, item)
    {
    }

    protected override void OnHitAsPotion(ServerLevel level, ItemStack potionItem, ProjectileHitResult hit)
    {
        var potion = GetPotionContents();
        var effects = potion.GetAllEffects();
        if (effects.Count == 0) return;
        var box = BoundingBox.Inflate(4.0, 2.0, 4.0);
        var owner = OwnerUuid is { } uuid ? level.GetEntity(uuid) : null;
        foreach (var living in level.GetEntitiesOfClass<LivingEntity>(box))
        {
            if (living.IsDeadOrDying) continue;
            var distance = Math.Sqrt(Pos.DistanceToSqr(living.Pos));
            if (distance >= 4.0) continue;
            var scale = 1.0 - distance / 4.0;
            foreach (var effect in effects)
            {
                var holder = effect.Effect;
                if (holder.Value is not NacreCraft.Game.World.Effect.MobEffect behaviour) continue;
                if (behaviour.IsInstantaneous)
                {
                    behaviour.ApplyInstantaneousEffect(level, this, owner, living, effect.Amplifier, scale);
                    continue;
                }
                var duration = (int)(scale * effect.Duration + 0.5);
                if (duration < 20) continue;
                living.AddEffect(new MobEffectInstance(holder, duration, effect.Amplifier, effect.IsAmbient, effect.IsVisible));
            }
        }
    }
}
