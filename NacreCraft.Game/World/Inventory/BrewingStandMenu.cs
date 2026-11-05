using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Alchemy;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Inventory;

//BrewingStandMenu brewing stand menu, maps to vanilla net.minecraft.world.inventory.BrewingStandMenu
//Slot layout: 0-2 bottles, 3 ingredient, 4 fuel, 5-31 main inventory, 32-40 hotbar
//The item tag system is not ported, potion slots and the fuel slot resolve their items by registry name instead
public sealed class BrewingStandMenu : AbstractContainerMenu
{
    public const int BottleSlotStart = 0;
    public const int BottleSlotEnd = 2;
    public const int IngredientSlot = 3;
    public const int FuelSlot = 4;
    private const int InvSlotStart = 5;
    private const int InvSlotEnd = 32;
    private const int HotbarSlotEnd = 41;

    private readonly Container _brewingStand;
    private readonly ContainerData _brewingStandData;
    private readonly Slot _ingredientSlot;

    public BrewingStandMenu(int containerId, PlayerInventory inventory, Container brewingStand,
        ContainerData brewingStandData)
        : base(MenuTypes.BREWING_STAND, containerId)
    {
        OwnerInventory = inventory;
        _brewingStand = brewingStand;
        _brewingStandData = brewingStandData;
        AddSlot(new PotionSlot(brewingStand, 0, 56, 51));
        AddSlot(new PotionSlot(brewingStand, 1, 79, 58));
        AddSlot(new PotionSlot(brewingStand, 2, 102, 51));
        _ingredientSlot = AddSlot(new IngredientSlotImpl(PotionBrewing.Empty, brewingStand, 3, 79, 17));
        AddSlot(new FuelSlotImpl(brewingStand, 4, 17, 17));
        AddDataSlots(brewingStandData);
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 8 + i * 18, 142));
    }

    public int Fuel => _brewingStandData.Get(1);

    public int BrewingTicks => _brewingStandData.Get(0);

    public override bool StillValid(ServerPlayer player) => true;

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex <= FuelSlot)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, HotbarSlotEnd, true)) return ItemStack.Empty;
        }
        else if (FuelSlotImpl.IsBrewingFuel(stack))
        {
            if (!MoveItemStackTo(stack, FuelSlot, FuelSlot + 1, false)) return ItemStack.Empty;
        }
        else if (_ingredientSlot.MayPlace(stack))
        {
            if (!MoveItemStackTo(stack, IngredientSlot, IngredientSlot + 1, false)) return ItemStack.Empty;
        }
        else if (PotionSlot.IsPotionItem(stack))
        {
            if (!MoveItemStackTo(stack, BottleSlotStart, BottleSlotEnd + 1, false)) return ItemStack.Empty;
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

    //PotionSlot one bottle slot, maps to vanilla PotionSlot
    private sealed class PotionSlot : Slot
    {
        public PotionSlot(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override bool MayPlace(ItemStack stack) => IsPotionItem(stack);

        public override int GetMaxStackSize() => 1;

        public static bool IsPotionItem(ItemStack stack)
            => IsItem(stack, "potion") || IsItem(stack, "splash_potion")
                || IsItem(stack, "lingering_potion") || IsItem(stack, "glass_bottle");
    }

    //IngredientSlotImpl brewing ingredient slot, maps to vanilla IngredientsSlot
    private sealed class IngredientSlotImpl : Slot
    {
        private readonly PotionBrewing _potionBrewing;

        public IngredientSlotImpl(PotionBrewing potionBrewing, Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y) => _potionBrewing = potionBrewing;

        public override bool MayPlace(ItemStack stack) => _potionBrewing.IsIngredient(stack);
    }

    //FuelSlotImpl brewing fuel slot, maps to vanilla FuelSlot; the brewing fuel tag maps to blaze powder
    private sealed class FuelSlotImpl : Slot
    {
        public FuelSlotImpl(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override bool MayPlace(ItemStack stack) => IsBrewingFuel(stack);

        public static bool IsBrewingFuel(ItemStack stack) => IsItem(stack, "blaze_powder");
    }

    private static bool IsItem(ItemStack stack, string name)
    {
        if (stack.IsEmpty()) return false;
        var id = Identifier.WithDefaultNamespace(name);
        return BuiltInRegistries.ITEM.ContainsKey(id)
            && ReferenceEquals(stack.GetItem(), BuiltInRegistries.ITEM.GetValue(id));
    }
}
