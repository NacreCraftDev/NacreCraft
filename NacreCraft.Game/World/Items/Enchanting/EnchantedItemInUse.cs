using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Enchanting;

//EnchantedItemInUse the item an enchantment effect is currently acting through, maps to vanilla net.minecraft.world.item.enchantment.EnchantedItemInUse
//Carries the stack, the slot it occupies, the owner and the break callback
public sealed record EnchantedItemInUse(
    ItemStack ItemStack,
    EquipmentSlot? InSlot,
    LivingEntity? Owner,
    Action<Item> OnBreak)
{
    public EnchantedItemInUse(ItemStack itemStack, EquipmentSlot? inSlot, LivingEntity? owner)
        : this(itemStack, inSlot, owner, _ => { })
    {
    }
}
