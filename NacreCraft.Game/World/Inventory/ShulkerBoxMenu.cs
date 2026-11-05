using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//ShulkerBoxMenu shulker box menu, maps to vanilla net.minecraft.world.inventory.ShulkerBoxMenu
//Slot layout: 0-26 shulker box, 27-53 main inventory, 54-62 hotbar
public sealed class ShulkerBoxMenu : AbstractContainerMenu
{
    public const int ContainerSize = 27;

    public ShulkerBoxMenu(int containerId, PlayerInventory inventory, Container container)
        : base(MenuTypes.SHULKER_BOX, containerId)
    {
        OwnerInventory = inventory;
        Container = container;
        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 9; x++)
                AddSlot(new ShulkerBoxSlot(container, x + y * 9, 8 + x * 18, 18 + y * 18));
        AddStandardInventorySlots(inventory, 8, 84);
    }

    public Container Container { get; }

    public override bool StillValid(ServerPlayer player) => true;

    private void AddStandardInventorySlots(PlayerInventory inventory, int x, int y)
    {
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, x + i % 9 * 18, y + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, x + i * 18, y + 58));
    }

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex < ContainerSize)
        {
            if (!MoveItemStackTo(stack, ContainerSize, Slots.Count, true)) return ItemStack.Empty;
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
