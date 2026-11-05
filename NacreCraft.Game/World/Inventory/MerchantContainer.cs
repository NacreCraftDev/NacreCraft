using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//MerchantContainer payment and result slots of a merchant menu, maps to vanilla net.minecraft.world.inventory.MerchantContainer
//Slot 0/1 take the payments, slot 2 holds the assembled offer
//Trading is not ported: there is no Merchant type or offer table, so updateSellItem clears the result slot
public sealed class MerchantContainer : Container
{
    private readonly ItemStack[] _items = { ItemStack.Empty, ItemStack.Empty, ItemStack.Empty };
    private int _selectionHint;
    private int _futureXp;

    public int Size => _items.Length;

    public bool IsEmpty()
    {
        foreach (var stack in _items)
            if (!stack.IsEmpty()) return false;
        return true;
    }

    public ItemStack GetItem(int slot) => (uint)slot < (uint)_items.Length ? _items[slot] : ItemStack.Empty;

    public void SetItem(int slot, ItemStack stack)
    {
        if ((uint)slot >= (uint)_items.Length) return;
        _items[slot] = stack ?? ItemStack.Empty;
        if (IsPaymentSlot(slot)) UpdateSellItem();
    }

    public ItemStack RemoveItem(int slot, int count)
    {
        if ((uint)slot >= (uint)_items.Length) return ItemStack.Empty;
        var taken = slot == 2 ? Take(slot) : Take(slot, count);
        if (!taken.IsEmpty() && IsPaymentSlot(slot)) UpdateSellItem();
        return taken;
    }

    public ItemStack RemoveItemNoUpdate(int slot)
        => (uint)slot < (uint)_items.Length ? Take(slot) : ItemStack.Empty;

    public void SetChanged() => UpdateSellItem();

    public void ClearContent()
    {
        for (var i = 0; i < _items.Length; i++) _items[i] = ItemStack.Empty;
    }

    public bool CanPlaceItem(int slot, ItemStack stack) => true;

    //ActiveOffer the offer currently shown, always null until trading is ported
    public object? ActiveOffer { get; private set; }

    public int FutureXp => _futureXp;

    public void SetSelectionHint(int selectionHint)
    {
        _selectionHint = selectionHint;
        UpdateSellItem();
    }

    //UpdateSellItem rebuilds the result from the payment slots, maps to vanilla updateSellItem
    public void UpdateSellItem()
    {
        ActiveOffer = null;
        _futureXp = 0;
        _items[2] = ItemStack.Empty;
    }

    private static bool IsPaymentSlot(int slot) => slot is 0 or 1;

    private ItemStack Take(int slot)
    {
        var stack = _items[slot];
        _items[slot] = ItemStack.Empty;
        return stack;
    }

    private ItemStack Take(int slot, int count)
    {
        var stack = _items[slot];
        if (stack.IsEmpty() || count <= 0) return ItemStack.Empty;
        var taken = stack.GetCount() <= count ? stack : stack.CopyWithCount(count);
        var remain = stack.GetCount() - taken.GetCount();
        _items[slot] = remain <= 0 ? ItemStack.Empty : stack.CopyWithCount(remain);
        return taken;
    }
}
