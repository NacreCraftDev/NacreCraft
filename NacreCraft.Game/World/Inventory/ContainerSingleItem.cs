using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level.Block;

namespace NacreCraft.Game.World.Inventory;

//ContainerSingleItem a container holding exactly one stack, maps to vanilla net.minecraft.world.ticks.ContainerSingleItem
//Vanilla implements the whole slot API on top of the single held item, so an implementor only supplies the item accessors
public interface ContainerSingleItem : Container
{
    ItemStack GetTheItem();

    void SetTheItem(ItemStack stack);

    //MaxStackSize the single slot holds a full stack, maps to the vanilla Container.getMaxStackSize default
    int MaxStackSize => 64;

    ItemStack SplitTheItem(int count) => GetTheItem().Split(count);

    ItemStack RemoveTheItem() => SplitTheItem(MaxStackSize);

    int Size => 1;

    bool IsEmpty() => GetTheItem().IsEmpty();

    void ClearContent() => RemoveTheItem();

    ItemStack RemoveItemNoUpdate(int slot) => RemoveItem(slot, MaxStackSize);

    ItemStack GetItem(int slot) => slot == 0 ? GetTheItem() : ItemStack.Empty;

    ItemStack RemoveItem(int slot, int count) => slot != 0 ? ItemStack.Empty : SplitTheItem(count);

    void SetItem(int slot, ItemStack stack)
    {
        if (slot == 0) SetTheItem(stack);
    }

    //BlockContainerSingleItem a single-slot container owned by a block entity, maps to vanilla BlockContainerSingleItem
    //StillValid stays with the implementor because NC's Container does not declare it
    public interface BlockContainerSingleItem : ContainerSingleItem
    {
        BlockEntity GetContainerBlockEntity();
    }
}
