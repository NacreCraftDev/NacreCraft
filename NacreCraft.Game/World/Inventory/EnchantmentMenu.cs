using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//EnchantmentMenu enchanting table menu, maps to vanilla net.minecraft.world.inventory.EnchantmentMenu
//Slot layout: 0 item, 1 lapis, 2-28 main inventory, 29-37 hotbar
//Data slot order: 0-2 costs, 3 enchantment seed, 4-6 enchantment clues, 7-9 level clues
//The enchantment registry and the bookshelf scan are not ported, so costs stay zero and enchanting is disabled
public sealed class EnchantmentMenu : AbstractContainerMenu
{
    public const int ItemSlot = 0;
    public const int LapisSlot = 1;
    private const int InvSlotStart = 2;
    private const int InvSlotEnd = 29;
    private const int HotbarSlotEnd = 38;

    private readonly SimpleContainer _enchantSlots = new(2);
    private readonly SimpleContainerData _data = new(10);

    public EnchantmentMenu(int containerId, PlayerInventory inventory, ContainerLevelAccess access)
        : base(MenuTypes.ENCHANTMENT, containerId)
    {
        OwnerInventory = inventory;
        Access = access;
        AddSlot(new EnchantItemSlot(_enchantSlots, 0, 15, 47));
        AddSlot(new LapisItemSlot(_enchantSlots, 1, 35, 47));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 8 + i * 18, 142));
        AddDataSlots(_data);
    }

    public EnchantmentMenu(int containerId, PlayerInventory inventory)
        : this(containerId, inventory, ContainerLevelAccess.Null)
    {
    }

    public ContainerLevelAccess Access { get; }

    //Costs the three offered enchantment costs, maps to vanilla costs
    public int GetCost(int index) => _data.Get(index);

    public int EnchantmentSeed => _data.Get(3);

    public int GoldCount
    {
        get
        {
            var stack = _enchantSlots.GetItem(1);
            return stack.IsEmpty() ? 0 : stack.GetCount();
        }
    }

    public override bool StillValid(ServerPlayer player) => true;

    public override void SlotsChanged(Container container)
    {
        if (!ReferenceEquals(container, _enchantSlots)) return;
        for (var i = 0; i < 3; i++)
        {
            _data.Set(i, 0);
            _data.Set(4 + i, -1);
            _data.Set(7 + i, -1);
        }
    }

    //ClickMenuButton applies an enchantment, maps to vanilla clickMenuButton; the enchantment registry is not ported
    public override bool ClickMenuButton(ServerPlayer player, int buttonId) => false;

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex is ItemSlot or LapisSlot)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, HotbarSlotEnd, true)) return ItemStack.Empty;
        }
        else if (stack.GetItem().Id.Path == "lapis_lazuli")
        {
            if (!MoveItemStackTo(stack, LapisSlot, LapisSlot + 1, true)) return ItemStack.Empty;
        }
        else if (!GetSlot(ItemSlot).HasItem() && GetSlot(ItemSlot).MayPlace(stack))
        {
            var single = stack.CopyWithCount(1);
            stack.SetCount(stack.GetCount() - 1);
            GetSlot(ItemSlot).Set(single);
        }
        else
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }

    private sealed class EnchantItemSlot : Slot
    {
        public EnchantItemSlot(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override int GetMaxStackSize() => 1;
    }

    private sealed class LapisItemSlot : Slot
    {
        public LapisItemSlot(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override bool MayPlace(ItemStack stack)
            => !stack.IsEmpty() && stack.GetItem().Id.Path == "lapis_lazuli";
    }
}
