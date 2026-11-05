using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//GrindstoneMenu grindstone menu, maps to vanilla net.minecraft.world.inventory.GrindstoneMenu
//Slot layout: 0/1 inputs, 2 result, 3-29 main inventory, 30-38 hotbar
//The result combines repair durability and keeps only curses; the enchantment component is not ported, so the result stays empty
public sealed class GrindstoneMenu : AbstractContainerMenu
{
    public const int InputSlot = 0;
    public const int AdditionalSlot = 1;
    public const int ResultSlot = 2;
    private const int InvSlotStart = 3;
    private const int InvSlotEnd = 30;
    private const int HotbarSlotEnd = 39;

    private readonly Container _repairSlots = new SimpleContainer(2);
    private readonly ResultContainer _resultSlots = new();

    public GrindstoneMenu(int containerId, PlayerInventory inventory, ContainerLevelAccess access)
        : base(MenuTypes.GRINDSTONE, containerId)
    {
        OwnerInventory = inventory;
        Access = access;
        AddSlot(new Slot(_repairSlots, 0, 49, 19));
        AddSlot(new Slot(_repairSlots, 1, 49, 40));
        AddSlot(new GrindstoneResultSlot(this, _resultSlots, 2, 129, 34));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 8 + i * 18, 142));
    }

    public GrindstoneMenu(int containerId, PlayerInventory inventory)
        : this(containerId, inventory, ContainerLevelAccess.Null)
    {
    }

    public ContainerLevelAccess Access { get; }

    public override bool StillValid(ServerPlayer player) => true;

    public override void SlotsChanged(Container container)
    {
        if (!ReferenceEquals(container, _repairSlots)) return;
        CreateResult();
    }

    //CreateResult recomputes the grindstone output, maps to vanilla createResult
    private void CreateResult() => _resultSlots.SetItem(0, ItemStack.Empty);

    //ConsumeInputs clears both inputs after the result is taken, maps to vanilla ResultSlot.onTake
    internal void ConsumeInputs()
    {
        _repairSlots.SetItem(0, ItemStack.Empty);
        _repairSlots.SetItem(1, ItemStack.Empty);
    }

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
        else if (slotIndex is InputSlot or AdditionalSlot)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, HotbarSlotEnd, false)) return ItemStack.Empty;
        }
        else if (_repairSlots.IsEmpty())
        {
            if (!MoveItemStackTo(stack, InputSlot, AdditionalSlot + 1, false)) return ItemStack.Empty;
        }
        else if (slotIndex is >= InvSlotStart and < InvSlotEnd)
        {
            if (!MoveItemStackTo(stack, InvSlotEnd, HotbarSlotEnd, false)) return ItemStack.Empty;
        }
        else if (!MoveItemStackTo(stack, InvSlotStart, InvSlotEnd, false))
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }

    private sealed class GrindstoneResultSlot : Slot
    {
        private readonly GrindstoneMenu _menu;

        public GrindstoneResultSlot(GrindstoneMenu menu, Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y) => _menu = menu;

        public override bool MayPlace(ItemStack stack) => false;

        public override ItemStack Remove(int count)
        {
            var taken = base.Remove(count);
            if (!taken.IsEmpty()) _menu.ConsumeInputs();
            return taken;
        }
    }
}
