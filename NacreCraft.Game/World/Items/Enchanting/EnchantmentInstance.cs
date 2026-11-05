using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Enchanting;

//EnchantmentInstance one enchantment at a level, maps to vanilla net.minecraft.world.item.enchantment.EnchantmentInstance
//Used when rolling enchantment offers; weight comes from the enchantment definition
public sealed record EnchantmentInstance(Holder<Enchantment> Enchantment, int Level)
{
    //Weight selection weight of this enchantment, maps to vanilla weight
    public int Weight
        => Enchantment.Value is NacreCraft.Game.World.Items.Enchanting.Enchantment enchantment
            ? enchantment.Definition.Weight
            : 0;
}
