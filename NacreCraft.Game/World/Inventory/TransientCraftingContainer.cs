using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//TransientCraftingContainer crafting grid that lives only inside the menu, maps to vanilla net.minecraft.world.inventory.TransientCraftingContainer
//Every write reports to the owning menu so it can rebuild the result slot
public sealed class TransientCraftingContainer : CraftingContainer
{
    private readonly ItemStack[] _items;
    private readonly AbstractContainerMenu _menu;

    public TransientCraftingContainer(AbstractContainerMenu menu, int width, int height)
    {
        _menu = menu;
        Width = width;
        Height = height;
        _items = new ItemStack[width * height];
        for (var i = 0; i < _items.Length; i++) _items[i] = ItemStack.Empty;
    }

    public int Width { get; }

    public int Height { get; }

    public int Size => _items.Length;

    public IReadOnlyList<ItemStack> Items => _items;

    public ItemStack GetItem(int slot)
        => (uint)slot < (uint)_items.Length ? _items[slot] : ItemStack.Empty;

    public void SetItem(int slot, ItemStack stack)
    {
        if ((uint)slot >= (uint)_items.Length) return;
        _items[slot] = stack ?? ItemStack.Empty;
        _menu.SlotsChanged(this);
    }

    public ItemStack RemoveItem(int slot, int count)
    {
        if ((uint)slot >= (uint)_items.Length || count <= 0) return ItemStack.Empty;
        var stack = _items[slot];
        if (stack.IsEmpty()) return ItemStack.Empty;
        var taken = stack.GetCount() <= count ? stack : stack.CopyWithCount(count);
        var remain = stack.GetCount() - taken.GetCount();
        _items[slot] = remain <= 0 ? ItemStack.Empty : stack.CopyWithCount(remain);
        _menu.SlotsChanged(this);
        return taken;
    }

    public ItemStack RemoveItemNoUpdate(int slot)
    {
        if ((uint)slot >= (uint)_items.Length) return ItemStack.Empty;
        var stack = _items[slot];
        _items[slot] = ItemStack.Empty;
        return stack;
    }

    public bool IsEmpty()
    {
        foreach (var stack in _items)
            if (!stack.IsEmpty()) return false;
        return true;
    }

    public void ClearContent()
    {
        for (var i = 0; i < _items.Length; i++) _items[i] = ItemStack.Empty;
    }

    public bool CanPlaceItem(int slot, ItemStack stack) => true;

    //SetChanged is a no-op here: the grid reports through the menu on every write instead, maps to vanilla setChanged
    public void SetChanged() { }

    //FillStackedContents accounts every grid stack, maps to vanilla fillStackedContents
    public void FillStackedContents(StackedItemContents contents)
    {
        foreach (var stack in _items)
            contents.AccountSimpleStack(stack);
    }
}
