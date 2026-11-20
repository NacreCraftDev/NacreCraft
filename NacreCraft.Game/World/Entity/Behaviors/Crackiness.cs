using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Entity;

//Crackiness picks a crack overlay level from an item durability fraction, maps to vanilla net.minecraft.world.entity.Crackiness
//GOLEM and WOLF_ARMOR are the two vanilla thresholds; the level is chosen by descending fractions
public sealed class Crackiness
{
    public static readonly Crackiness GOLEM = new(0.75f, 0.5f, 0.25f);
    public static readonly Crackiness WOLF_ARMOR = new(0.95f, 0.69f, 0.32f);

    private readonly float _fractionLow;
    private readonly float _fractionMedium;
    private readonly float _fractionHigh;

    private Crackiness(float fractionLow, float fractionMedium, float fractionHigh)
    {
        _fractionLow = fractionLow;
        _fractionMedium = fractionMedium;
        _fractionHigh = fractionHigh;
    }

    public enum Level
    {
        NONE,
        LOW,
        MEDIUM,
        HIGH
    }

    //ByFraction the crack level for a remaining durability fraction, maps to vanilla byFraction
    public Level ByFraction(float fraction)
    {
        if (fraction < _fractionHigh) return Level.HIGH;
        if (fraction < _fractionMedium) return Level.MEDIUM;
        if (fraction < _fractionLow) return Level.LOW;
        return Level.NONE;
    }

    //ByDamage the crack level for an item stack, maps to vanilla byDamage(ItemStack)
    //NC has no isDamageableItem helper, so the max damage component decides whether the item is damageable
    public Level ByDamage(ItemStack item)
    {
        var maxDamage = item.Get(DataComponents.MAX_DAMAGE) as int? ?? 0;
        if (maxDamage <= 0) return Level.NONE;
        var damage = item.Get(DataComponents.DAMAGE) as int? ?? 0;
        return ByDamage(damage, maxDamage);
    }

    public Level ByDamage(int damage, int maxDamage)
        => maxDamage <= 0 ? Level.NONE : ByFraction((maxDamage - damage) / (float)maxDamage);
}
