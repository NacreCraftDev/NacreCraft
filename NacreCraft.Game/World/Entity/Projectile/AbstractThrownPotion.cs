using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//AbstractThrownPotion the shared potion throwable, maps to vanilla net.minecraft.world.entity.projectile.throwableitemprojectile.AbstractThrownPotion
//Water potions douse burning entities, effect potions delegate to the concrete splash/lingering behavior; NC has no candle/campfire
//dowsing and no water-sensitive mob check yet
public abstract class AbstractThrownPotion : ThrowableItemProjectile
{
    public const double SplashRange = 4.0;

    protected AbstractThrownPotion(EntityType<object> type) : base(type)
    {
    }

    protected AbstractThrownPotion(EntityType<object> type, LivingEntity owner, ItemStack item) : base(type, owner, item)
    {
    }

    public override double DefaultGravity => 0.05;

    protected abstract void OnHitAsPotion(ServerLevel level, ItemStack potionItem, ProjectileHitResult hit);

    protected override void OnHit(ProjectileHitResult hit)
    {
        base.OnHit(hit);
        if (Level is not ServerLevel level) return;
        var potion = GetPotionContents();
        if (potion.Is(Potions.WATER)) OnHitAsWater(level);
        else if (potion.HasEffects) OnHitAsPotion(level, Item, hit);
        Discard();
    }

    protected PotionContents GetPotionContents()
        => Item.Get<object>(DataComponents.POTION_CONTENTS) as PotionContents ?? PotionContents.Empty;

    private void OnHitAsWater(ServerLevel level)
    {
        var box = BoundingBox.Inflate(SplashRange, 2.0, SplashRange);
        foreach (var living in level.GetEntitiesOfClass<LivingEntity>(box))
        {
            if (Pos.DistanceToSqr(living.Pos) <= SplashRange * SplashRange && living.IsOnFire) living.ClearFire();
        }
    }
}
