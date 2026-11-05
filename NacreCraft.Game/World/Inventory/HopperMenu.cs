using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//HopperMenu hopper menu, maps to vanilla net.minecraft.world.inventory.HopperMenu
//Slot layout: 0-4 hopper, 5-31 main inventory, 32-40 hotbar
public sealed class HopperMenu : AbstractContainerMenu
{
    public const int ContainerSize = 5;
    private const int HotbarSlotEnd = 41;

    public HopperMenu(int containerId, PlayerInventory inventory, Container hopper)
        : base(MenuTypes.HOPPER, containerId)
    {
        OwnerInventory = inventory;
        Hopper = hopper;
        for (var i = 0; i < ContainerSize; i++)
            AddSlot(new Slot(hopper, i, 44 + i * 18, 20));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 51 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 8 + i * 18, 109));
    }

    public Container Hopper { get; }

    public override bool StillValid(ServerPlayer player) => true;

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex < ContainerSize)
        {
            if (!MoveItemStackTo(stack, ContainerSize, HotbarSlotEnd, true)) return ItemStack.Empty;
        }
        else if (!MoveItemStackTo(stack, 0, ContainerSize, false))
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }
}
