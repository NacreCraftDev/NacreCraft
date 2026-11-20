using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//ItemSupplier an entity whose visual item comes from its stack, maps to vanilla net.minecraft.world.entity.projectile.ItemSupplier
//Used by projectiles that render as an item, such as eyes of ender and firework rockets
public interface ItemSupplier
{
    ItemStack GetItem();

    //DefaultStack resolves a built-in item by id, an unregistered id gives an empty stack
    //Vanilla constructs the stack directly from Items.*; NC resolves through the registry so a placeholder item still works
    public static ItemStack DefaultStack(string id)
    {
        var item = BuiltInRegistries.ITEM.GetValue(Identifier.WithDefaultNamespace(id));
        return item is null ? ItemStack.Empty : new ItemStack(item.BuiltInRegistryHolder, 1, DataComponentPatch.Empty);
    }
}
