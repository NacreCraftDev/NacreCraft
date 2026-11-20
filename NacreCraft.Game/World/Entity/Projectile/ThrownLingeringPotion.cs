using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//ThrownLingeringPotion a lingering potion, maps to vanilla net.minecraft.world.entity.projectile.throwableitemprojectile.ThrownLingeringPotion
//On impact it spawns a radius-3 cloud lasting 600 ticks; the cloud applies the potion effects every second
public sealed class ThrownLingeringPotion : AbstractThrownPotion
{
    public ThrownLingeringPotion(EntityType<object> type) : base(type)
    {
    }

    public ThrownLingeringPotion(EntityType<object> type, LivingEntity owner, ItemStack item) : base(type, owner, item)
    {
    }

    protected override void OnHitAsPotion(ServerLevel level, ItemStack potionItem, ProjectileHitResult hit)
    {
        var cloud = new AreaEffectCloud
        {
            Radius = 3f,
            Duration = 600,
            WaitTime = 10,
            Owner = OwnerUuid is { } uuid ? level.GetEntity(uuid) as Player : null,
        };
        cloud.Pos = Pos;
        cloud.RadiusPerTick = -cloud.Radius / cloud.Duration;
        foreach (var effect in GetPotionContents().GetAllEffects()) cloud.AddEffect(new MobEffectInstance(effect));
        level.AddEntity(cloud);
    }
}
