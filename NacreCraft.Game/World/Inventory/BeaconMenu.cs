using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;
using MobEffect = NacreCraft.Game.World.Effect.MobEffect;

namespace NacreCraft.Game.World.Inventory;

//BeaconMenu beacon menu, maps to vanilla net.minecraft.world.inventory.BeaconMenu
//One payment slot and three data slots (level, primary effect, secondary effect)
//The effect registry lookup and the beacon effect validation are not ported, so effect selection stays disabled
public sealed class BeaconMenu : AbstractContainerMenu
{
    public const int PaymentSlotIndex = 0;

    private readonly Container _beacon;
    private readonly ContainerData _beaconData;

    public BeaconMenu(int containerId, PlayerInventory inventory, ContainerData beaconData)
        : this(containerId, inventory, beaconData, ContainerLevelAccess.Null)
    {
    }

    public BeaconMenu(int containerId, PlayerInventory inventory, ContainerData beaconData,
        ContainerLevelAccess access)
        : base(MenuTypes.BEACON, containerId)
    {
        OwnerInventory = inventory;
        Access = access;
        _beaconData = beaconData;
        _beacon = new PaymentContainer();
        AddSlot(new PaymentSlot(_beacon, PaymentSlotIndex, 136, 110));
        AddDataSlots(beaconData);
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 36 + i % 9 * 18, 137 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 36 + i * 18, 195));
    }

    public ContainerLevelAccess Access { get; }

    public int Levels => _beaconData.Get(0);

    public bool HasPayment => !_beacon.GetItem(0).IsEmpty();

    public override bool StillValid(ServerPlayer player) => true;

    //UpdateEffects effect selection, maps to vanilla updateEffects; the effect registry is not ported
    public bool UpdateEffects(Holder<NacreCraft.Game.World.Effect.MobEffect>? primary,
        Holder<NacreCraft.Game.World.Effect.MobEffect>? secondary) => false;

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex == PaymentSlotIndex)
        {
            if (!MoveItemStackTo(stack, 1, Slots.Count, true)) return ItemStack.Empty;
        }
        else if (!MoveItemStackTo(stack, 1, Slots.Count, false))
        {
            return ItemStack.Empty;
        }
        if (stack.IsEmpty()) slot.Set(ItemStack.Empty);
        else slot.SetChanged();
        return moved;
    }

    //PaymentContainer one-slot payment container, maps to the anonymous container in vanilla BeaconMenu
    private sealed class PaymentContainer : Container
    {
        private ItemStack _item = ItemStack.Empty;

        public int Size => 1;

        public ItemStack GetItem(int slot) => _item;

        public void SetItem(int slot, ItemStack stack) => _item = stack ?? ItemStack.Empty;

        public ItemStack RemoveItem(int slot, int count)
        {
            if (count <= 0 || _item.IsEmpty()) return ItemStack.Empty;
            var taken = _item.GetCount() <= count ? _item : _item.CopyWithCount(count);
            var remain = _item.GetCount() - taken.GetCount();
            _item = remain <= 0 ? ItemStack.Empty : _item.CopyWithCount(remain);
            return taken;
        }

        public ItemStack RemoveItemNoUpdate(int slot)
        {
            var stack = _item;
            _item = ItemStack.Empty;
            return stack;
        }

        public bool IsEmpty() => _item.IsEmpty();

        public void ClearContent() => _item = ItemStack.Empty;

        public bool CanPlaceItem(int slot, ItemStack stack) => true;

        public void SetChanged() { }
    }

    private sealed class PaymentSlot : Slot
    {
        public PaymentSlot(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override int GetMaxStackSize() => 1;
    }
}
