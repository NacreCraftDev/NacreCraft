using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//CartographyTableMenu cartography table menu, maps to vanilla net.minecraft.world.inventory.CartographyTableMenu
//Slot layout: 0 map, 1 additional item, 2 result, 3-29 main inventory, 30-38 hotbar
//Map data classes are not ported, so the result slot stays empty
public sealed class CartographyTableMenu : AbstractContainerMenu
{
    public const int MapSlot = 0;
    public const int AdditionalSlot = 1;
    public const int ResultSlot = 2;
    private const int InvSlotStart = 3;
    private const int InvSlotEnd = 30;
    private const int HotbarSlotEnd = 39;

    private readonly SimpleContainer _container = new(2);
    private readonly ResultContainer _resultContainer = new();

    public CartographyTableMenu(int containerId, PlayerInventory inventory, ContainerLevelAccess access)
        : base(MenuTypes.CARTOGRAPHY_TABLE, containerId)
    {
        OwnerInventory = inventory;
        Access = access;
        AddSlot(new Slot(_container, 0, 15, 15));
        AddSlot(new AdditionalItemSlot(_container, 1, 15, 52));
        AddSlot(new CartographyResultSlot(this, _resultContainer, 2, 145, 39));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 8 + i * 18, 142));
    }

    public CartographyTableMenu(int containerId, PlayerInventory inventory)
        : this(containerId, inventory, ContainerLevelAccess.Null)
    {
    }

    public ContainerLevelAccess Access { get; }

    public override bool StillValid(ServerPlayer player) => true;

    //SlotsChanged recomputes the result, maps to vanilla slotsChanged; map data is not ported so it clears
    public override void SlotsChanged(Container container)
    {
        _resultContainer.SetItem(0, ItemStack.Empty);
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
        else if (slotIndex is MapSlot or AdditionalSlot)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, HotbarSlotEnd, false)) return ItemStack.Empty;
        }
        else if (stack.GetItem().Id.Path is "paper" or "map" or "glass_pane")
        {
            if (!MoveItemStackTo(stack, AdditionalSlot, AdditionalSlot + 1, false)) return ItemStack.Empty;
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

    private sealed class AdditionalItemSlot : Slot
    {
        public AdditionalItemSlot(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override bool MayPlace(ItemStack stack)
            => !stack.IsEmpty() && stack.GetItem().Id.Path is "paper" or "map" or "glass_pane";
    }

    private sealed class CartographyResultSlot : Slot
    {
        private readonly CartographyTableMenu _menu;

        public CartographyResultSlot(CartographyTableMenu menu, Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y) => _menu = menu;

        public override bool MayPlace(ItemStack stack) => false;

        //Remove consumes one map and one additional item, maps to vanilla onTake
        public override ItemStack Remove(int count)
        {
            var taken = base.Remove(count);
            if (taken.IsEmpty()) return taken;
            _menu.GetSlot(MapSlot).Remove(1);
            _menu.GetSlot(AdditionalSlot).Remove(1);
            return taken;
        }
    }
}
