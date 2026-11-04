using NacreCraft.Game.World.Entity;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Damage;

//CombatRules armor reduction formulas, maps to vanilla net.minecraft.world.damagesource.CombatRules
public static class CombatRules
{
    public const float MaxArmor = 20.0f;

    public const float ArmorProtectionDivider = 25.0f;

    public const float BaseArmorToughness = 2.0f;

    public const float MinArmorRatio = 0.2f;

    //GetDamageAfterAbsorb damage left after armor absorbs part of it, maps to vanilla getDamageAfterAbsorb
    //Vanilla lets the weapon's armor-effectiveness enchantments modify the fraction before applying it; enchantments are not ported
    //yet, so the plain fraction is used
    public static float GetDamageAfterAbsorb(LivingEntity victim, float damage, DamageSource source, float totalArmor,
        float armorToughness)
    {
        var toughness = BaseArmorToughness + armorToughness / 4.0f;
        var realArmor = Mth.Clamp(totalArmor - damage / toughness, totalArmor * MinArmorRatio, MaxArmor);
        return damage * (1.0f - realArmor / ArmorProtectionDivider);
    }

    //GetDamageAfterMagicAbsorb damage left after magic absorption, maps to vanilla getDamageAfterMagicAbsorb
    public static float GetDamageAfterMagicAbsorb(float damage, float totalMagicArmor)
    {
        var realArmor = Mth.Clamp(totalMagicArmor, 0.0f, MaxArmor);
        return damage * (1.0f - realArmor / ArmorProtectionDivider);
    }
}
