using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//MerchantMenu villager trading menu, maps to vanilla net.minecraft.world.inventory.MerchantMenu
//Slot layout: 0/1 payments, 2 result, 3-38 player inventory
//The Merchant interface and the offer table are not ported, so the result slot stays empty until trading lands
public sealed class MerchantMenu : AbstractContainerMenu
{
    public const int Payment1Slot = 0;
    public const int Payment2Slot = 1;
    public const int ResultSlot = 2;
    public const int InvSlotStart = 3;
    public const int InvSlotEnd = 30;
    public const int HotbarSlotStart = 30;
    public const int HotbarSlotEnd = 39;

    private int _merchantLevel;
    private bool _showProgressBar;
    private bool _canRestock;

    public MerchantMenu(int containerId, PlayerInventory inventory)
        : base(MenuTypes.MERCHANT, containerId)
    {
        OwnerInventory = inventory;
        TradeContainer = new MerchantContainer();
        AddSlot(new Slot(TradeContainer, Payment1Slot, 136, 37));
        AddSlot(new Slot(TradeContainer, Payment2Slot, 162, 37));
        AddSlot(new MerchantResultSlot(TradeContainer, ResultSlot, 220, 37));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 108 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 108 + i * 18, 142));
    }

    public MerchantContainer TradeContainer { get; }

    public override bool StillValid(ServerPlayer player) => true;

    public void SetShowProgressBar(bool show) => _showProgressBar = show;

    public bool ShowProgressBar => _showProgressBar;

    public void SetSelectionHint(int hint) => TradeContainer.SetSelectionHint(hint);

    public int TraderLevel => _merchantLevel;

    public void SetMerchantLevel(int level) => _merchantLevel = level;

    public bool CanRestock => _canRestock;

    public void SetCanRestock(bool canRestock) => _canRestock = canRestock;

    public override void SlotsChanged(Container container) => TradeContainer.UpdateSellItem();

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex == ResultSlot)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, HotbarSlotEnd, true)) return ItemStack.Empty;
        }
        else if (slotIndex is Payment1Slot or Payment2Slot)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, HotbarSlotEnd, false)) return ItemStack.Empty;
        }
        else if (slotIndex is >= InvSlotStart and < InvSlotEnd)
        {
            if (!MoveItemStackTo(stack, HotbarSlotStart, HotbarSlotEnd, false)) return ItemStack.Empty;
        }
        else if (!MoveItemStackTo(stack, InvSlotStart, InvSlotEnd, false))
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }
}
