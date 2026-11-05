using NacreCraft.Game.World.Items.Component;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Enchanting;

//EnchantmentHelper enchantment queries on item stacks, maps to vanilla net.minecraft.world.item.enchantment.EnchantmentHelper
//Only the component level helpers are ported; effect processing needs the effect component table and the entity/loot layers
public static class EnchantmentHelper
{
    //GetComponentType enchantments live in STORED_ENCHANTMENTS on an enchanted book, ENCHANTMENTS elsewhere, maps to vanilla getComponentType
    private static DataComponentType<object> GetComponentType(ItemStack stack)
        => stack.GetItem().Id.Path == "enchanted_book"
            ? DataComponents.STORED_ENCHANTMENTS
            : DataComponents.ENCHANTMENTS;

    //GetEnchantments reads the relevant enchantment component, empty when absent
    public static ItemEnchantments GetEnchantments(ItemStack stack, DataComponentType<object> componentType)
        => stack.Get(componentType) as ItemEnchantments ?? ItemEnchantments.Empty;

    //GetItemEnchantmentLevel level of one enchantment on the stack, 0 when absent, maps to vanilla getItemEnchantmentLevel
    public static int GetItemEnchantmentLevel(Holder<NacreCraft.Registry.Enchantment> enchantment, ItemStack stack)
        => GetEnchantments(stack, GetComponentType(stack)).GetLevel(enchantment);

    //GetEnchantmentsForCrafting the stack's crafting enchantments, maps to vanilla getEnchantmentsForCrafting
    public static ItemEnchantments GetEnchantmentsForCrafting(ItemStack stack)
        => GetEnchantments(stack, GetComponentType(stack));

    //CanStoreEnchantments whether the stack has an enchantment component at all, maps to vanilla canStoreEnchantments
    public static bool CanStoreEnchantments(ItemStack stack) => stack.Get(GetComponentType(stack)) is ItemEnchantments;

    //SetEnchantments overwrites the stack's enchantments, maps to vanilla setEnchantments
    public static void SetEnchantments(ItemStack stack, ItemEnchantments enchantments)
        => stack.Set(GetComponentType(stack), enchantments);

    //UpdateEnchantments mutates a copy of the stack's enchantments and writes it back, maps to vanilla updateEnchantments
    public static ItemEnchantments UpdateEnchantments(ItemStack stack, Action<ItemEnchantments.Mutable> updater)
    {
        var componentType = GetComponentType(stack);
        var current = stack.Get(componentType) as ItemEnchantments;
        if (current is null) return ItemEnchantments.Empty;
        var mutable = new ItemEnchantments.Mutable(current);
        updater(mutable);
        var updated = mutable.ToImmutable();
        stack.Set(componentType, updated);
        return updated;
    }

    //HasAnyEnchantments whether either enchantment component is non-empty, maps to vanilla hasAnyEnchantments
    public static bool HasAnyEnchantments(ItemStack stack)
        => GetEnchantments(stack, DataComponents.ENCHANTMENTS) is { IsEmpty: false }
           || GetEnchantments(stack, DataComponents.STORED_ENCHANTMENTS) is { IsEmpty: false };
}
