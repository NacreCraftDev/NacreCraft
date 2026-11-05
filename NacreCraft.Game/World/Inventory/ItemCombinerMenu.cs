using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Inventory;

//ItemCombinerMenu base for anvil and smithing menus, maps to vanilla net.minecraft.world.inventory.ItemCombinerMenu
//Builds the input slots from a definition, adds the result slot and the standard player inventory
public abstract class ItemCombinerMenu : AbstractContainerMenu
{
    protected ItemCombinerMenu(MenuType? menuType, int containerId, ServerPlayer player,
        ContainerLevelAccess access, ItemCombinerMenuSlotDefinition definition)
        : base(menuType, containerId)
    {
        Player = player;
        OwnerInventory = player.Inventory;
        Access = access;
        InputSlots = new SimpleContainer(definition.NumOfInputSlots);
        ResultSlots = new ResultContainer();
        ResultSlotIndex = definition.ResultSlotIndex;
        foreach (var slot in definition.Slots)
            AddSlot(new InputSlot(InputSlots, slot.SlotIndex, slot.X, slot.Y, slot.MayPlace));
        AddSlot(new CombinerResultSlot(this, ResultSlots, definition.ResultSlot.SlotIndex,
            definition.ResultSlot.X, definition.ResultSlot.Y));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(player.Inventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + i * 18, 142));
        InputSlots.Changed += _ => SlotsChanged(InputSlots);
    }

    protected ServerPlayer Player { get; }

    protected ContainerLevelAccess Access { get; }

    protected SimpleContainer InputSlots { get; }

    protected ResultContainer ResultSlots { get; }

    public int ResultSlotIndex { get; }

    protected virtual bool MayPickup(ServerPlayer player, bool hasItem) => true;

    protected abstract void OnTake(ServerPlayer player, ItemStack carried);

    protected abstract bool IsValidBlock(BlockState state);

    public abstract void CreateResult();

    public override void SlotsChanged(Container container)
    {
        if (ReferenceEquals(container, InputSlots)) CreateResult();
    }

    //Removed returns the input contents to the player, maps to vanilla removed
    public override void Removed(ServerPlayer player)
    {
        for (var i = 0; i < InputSlots.Size; i++)
        {
            var stack = InputSlots.RemoveItemNoUpdate(i);
            if (!stack.IsEmpty()) player.Drop(stack, randomly: false, thrownFromHand: false);
        }
    }

    public override bool StillValid(ServerPlayer player)
        => Access.Evaluate((level, pos) =>
        {
            var state = level.GetBlockState(pos);
            return state.HasValue && IsValidBlock(state.Value);
        }, true);

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        var inventoryStart = ResultSlotIndex + 1;
        var inventoryEnd = inventoryStart + 27;
        var useRowStart = inventoryEnd;
        var useRowEnd = useRowStart + 9;
        if (slotIndex == ResultSlotIndex)
        {
            if (!MoveItemStackTo(stack, inventoryStart, useRowEnd, true)) return ItemStack.Empty;
        }
        else if (slotIndex < ResultSlotIndex)
        {
            if (!MoveItemStackTo(stack, inventoryStart, useRowEnd, false)) return ItemStack.Empty;
        }
        else if (CanMoveIntoInputSlots(stack) && slotIndex >= inventoryStart && slotIndex < useRowEnd)
        {
            if (!MoveItemStackTo(stack, 0, ResultSlotIndex, false)) return ItemStack.Empty;
        }
        else if (slotIndex >= inventoryStart && slotIndex < inventoryEnd)
        {
            if (!MoveItemStackTo(stack, useRowStart, useRowEnd, false)) return ItemStack.Empty;
        }
        else if (slotIndex >= useRowStart && slotIndex < useRowEnd)
        {
            if (!MoveItemStackTo(stack, inventoryStart, inventoryEnd, false)) return ItemStack.Empty;
        }
        else
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }

    protected virtual bool CanMoveIntoInputSlots(ItemStack stack) => true;

    private sealed class InputSlot : Slot
    {
        private readonly Func<ItemStack, bool> _mayPlace;

        public InputSlot(Container container, int slotIndex, int x, int y, Func<ItemStack, bool> mayPlace)
            : base(container, slotIndex, x, y) => _mayPlace = mayPlace;

        public override bool MayPlace(ItemStack stack) => _mayPlace(stack);
    }

    private sealed class CombinerResultSlot : Slot
    {
        private readonly ItemCombinerMenu _menu;

        public CombinerResultSlot(ItemCombinerMenu menu, Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y) => _menu = menu;

        public override bool MayPlace(ItemStack stack) => false;

        public override ItemStack Remove(int count)
        {
            if (!_menu.MayPickup(_menu.Player, HasItem())) return ItemStack.Empty;
            var taken = base.Remove(count);
            if (!taken.IsEmpty()) _menu.OnTake(_menu.Player, taken);
            return taken;
        }
    }
}
