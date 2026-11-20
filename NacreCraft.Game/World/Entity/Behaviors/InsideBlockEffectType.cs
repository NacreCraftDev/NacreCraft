namespace NacreCraft.Game.World.Entity;

//InsideBlockEffectType the effects applied while an entity overlaps a block, maps to vanilla net.minecraft.world.entity.InsideBlockEffectType
//NC has no powder-snow/fire block systems yet, so the effects only touch the entity-side freeze/fire flags
public enum InsideBlockEffectType
{
    FREEZE,
    CLEAR_FREEZE,
    FIRE_IGNITE,
    LAVA_IGNITE,
    EXTINGUISH,
}

//InsideBlockEffectTypeExtensions the effect bodies, maps to the vanilla enum's Consumer field
public static class InsideBlockEffectTypeExtensions
{
    public static void ApplyEffect(this InsideBlockEffectType type, NacreCraft.Registry.Entity entity)
    {
        switch (type)
        {
            case InsideBlockEffectType.FREEZE:
                entity.IsInPowderSnow = true;
                if (entity.CanFreeze)
                    entity.TicksFrozen = Math.Min(entity.TicksRequiredToFreeze, entity.TicksFrozen + 1);
                break;
            case InsideBlockEffectType.CLEAR_FREEZE:
                entity.ClearFreeze();
                break;
            case InsideBlockEffectType.FIRE_IGNITE:
                entity.IsOnFire = true;
                break;
            case InsideBlockEffectType.LAVA_IGNITE:
                entity.LavaIgnite();
                break;
            case InsideBlockEffectType.EXTINGUISH:
                entity.ClearFire();
                break;
        }
    }
}
