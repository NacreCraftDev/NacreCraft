using NacreCraft.Game.Server;
using NacreCraft.Game.World.Crafting;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//CrafterMenu crafter menu, maps to vanilla net.minecraft.world.inventory.CrafterMenu
//Slot layout: 0-8 crafting grid, 9-35 main inventory, 36-44 hotbar, 45 preview result
//The result slot is non-interactive: it only previews what the block entity would craft
public sealed class CrafterMenu : AbstractContainerMenu, ContainerListener
{
    public const int SlotCount = 9;
    public const int ResultSlotIndex = 45;

    private readonly ResultContainer _resultContainer = new();
    private readonly ContainerData _containerData;
    private readonly CraftingContainer _container;

    public CrafterMenu(int containerId, PlayerInventory inventory)
        : base(MenuTypes.CRAFTER_3X3, containerId)
    {
        OwnerInventory = inventory;
        _containerData = new SimpleContainerData(10);
        _container = new TransientCraftingContainer(this, 3, 3);
        AddSlots(inventory);
    }

    public CrafterMenu(int containerId, PlayerInventory inventory, CraftingContainer container,
        ContainerData containerData)
        : base(MenuTypes.CRAFTER_3X3, containerId)
    {
        OwnerInventory = inventory;
        _containerData = containerData;
        _container = container;
        AddSlots(inventory);
    }

    public CraftingContainer Container => _container;

    private void AddSlots(PlayerInventory inventory)
    {
        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
            {
                var slot = x + y * 3;
                AddSlot(new CrafterSlot(_container, slot, 26 + x * 18, 17 + y * 18, this));
            }
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 8 + i * 18, 142));
        AddSlot(new NonInteractiveResultSlot(_resultContainer, 0, 134, 35));
        AddDataSlots(_containerData);
        RefreshRecipeResult();
    }

    public override bool StillValid(ServerPlayer player) => true;

    public void SetSlotState(int slotId, bool enabled)
    {
        _containerData.Set(slotId, enabled ? 0 : 1);
        BroadcastChanges();
    }

    public bool IsSlotDisabled(int slotId)
        => slotId is >= 0 and < SlotCount && _containerData.Get(slotId) == 1;

    public bool IsPowered => _containerData.Get(9) == 1;

    public override void SlotsChanged(Container container) => RefreshRecipeResult();

    public void SlotChanged(AbstractContainerMenu menu, int slotIndex, ItemStack stack) => RefreshRecipeResult();

    public void DataChanged(AbstractContainerMenu menu, int id, int value) { }

    //RefreshRecipeResult previews the current crafting result, maps to vanilla refreshRecipeResult
    private void RefreshRecipeResult()
    {
        var result = RecipeManager.Active?.GetCraftingResult(_container.AsCraftInput()) ?? ItemStack.Empty;
        _resultContainer.SetItem(0, result);
    }

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex < SlotCount)
        {
            if (!MoveItemStackTo(stack, SlotCount, ResultSlotIndex, true)) return ItemStack.Empty;
        }
        else if (!MoveItemStackTo(stack, 0, SlotCount, false))
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }
}
