using NacreCraft.Game.Server;
using NacreCraft.Game.World.Crafting;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Level.Block;

namespace NacreCraft.Game.World.Inventory;

//AbstractFurnaceMenu furnace family menu base, maps to vanilla net.minecraft.world.inventory.AbstractFurnaceMenu
//Slot layout: 0 input, 1 fuel, 2 result, 3-29 main inventory, 30-38 hotbar; four data slots carry burn and cook progress
//The furnace, blast furnace and smoker differ only in menu type and cooking recipe type
public abstract class AbstractFurnaceMenu : AbstractContainerMenu
{
    public const int IngredientSlotIndex = 0;
    public const int FuelSlotIndex = 1;
    public const int ResultSlotIndex = 2;
    public const int SlotCount = 3;

    //Menu index ranges of the inventory and hotbar sections, same ranges vanilla quickMoveStack uses
    public const int InvSlotStart = 3;
    private const int InvSlotEnd = 30;
    private const int UseRowSlotStart = 30;
    private const int UseRowSlotEnd = 39;

    //InventoryTop y coordinate of the inventory's first row, aligned with vanilla addStandardInventorySlots(inv, 8, 84)
    private const int InventoryTop = 84;

    protected AbstractFurnaceMenu(MenuType kind, int containerId, PlayerInventory inventory,
        AbstractFurnaceBlockEntity furnace)
        : base(kind, containerId)
    {
        OwnerInventory = inventory;
        Furnace = furnace;
        AddSlot(new Slot(furnace, IngredientSlotIndex, 56, 17));
        AddSlot(new FurnaceFuelSlot(IsFuel, furnace, FuelSlotIndex, 56, 53));
        AddSlot(new Slot(furnace, ResultSlotIndex, 116, 35));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i,
                8 + i % 9 * 18, InventoryTop + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 8 + i * 18, InventoryTop + 58));
        AddDataSlots(furnace);
    }

    //Furnace the smelting block entity backing the menu
    public AbstractFurnaceBlockEntity Furnace { get; }

    //CanSmelt whether the item has a recipe in this menu's cooking type, maps to vanilla canSmelt
    public bool CanSmelt(ItemStack stack)
        => RecipeManager.Active?.GetCookingRecipe(Furnace.CookingRecipeType, stack) is not null;

    //IsFuel whether the item can be used as fuel, maps to vanilla isFuel
    public bool IsFuel(ItemStack stack) => FuelValues.Active?.IsFuel(stack) == true;

    public override bool StillValid(ServerPlayer player) => Furnace.StillValid(player);

    //QuickMoveStack moves the result and input/fuel to and from the inventory, matching the branch order of vanilla quickMoveStack
    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex == ResultSlotIndex)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, UseRowSlotEnd, true)) return ItemStack.Empty;
        }
        else if (slotIndex is IngredientSlotIndex or FuelSlotIndex)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, UseRowSlotEnd, false)) return ItemStack.Empty;
        }
        else if (CanSmelt(stack))
        {
            if (!MoveItemStackTo(stack, IngredientSlotIndex, IngredientSlotIndex + 1, false)) return ItemStack.Empty;
        }
        else if (IsFuel(stack))
        {
            if (!MoveItemStackTo(stack, FuelSlotIndex, FuelSlotIndex + 1, false)) return ItemStack.Empty;
        }
        else if (slotIndex is >= InvSlotStart and < InvSlotEnd)
        {
            if (!MoveItemStackTo(stack, UseRowSlotStart, UseRowSlotEnd, false)) return ItemStack.Empty;
        }
        else if (slotIndex is >= UseRowSlotStart and < UseRowSlotEnd
            && !MoveItemStackTo(stack, InvSlotStart, InvSlotEnd, false))
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }
}
