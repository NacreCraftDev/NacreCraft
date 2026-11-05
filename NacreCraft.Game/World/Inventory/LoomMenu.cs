using NacreCraft.Game.Server;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//LoomMenu loom menu, maps to vanilla net.minecraft.world.inventory.LoomMenu
//Slot layout: 0 banner, 1 dye, 2 pattern, 3 result, 4-30 main inventory, 31-39 hotbar
//Banner pattern data is not ported, so the selectable pattern list stays empty and no result is produced
public sealed class LoomMenu : AbstractContainerMenu
{
    private const int InvSlotStart = 4;
    private const int InvSlotEnd = 31;
    private const int HotbarSlotEnd = 40;
    private const int PatternNotSet = -1;

    private readonly SimpleContainer _inputContainer = new(3);
    private readonly SimpleContainer _outputContainer = new(1);
    private readonly DataSlot _selectedBannerPatternIndex = DataSlot.Standalone();
    private readonly Slot _bannerSlot;
    private readonly Slot _dyeSlot;
    private readonly Slot _patternSlot;
    private readonly Slot _resultSlot;

    public LoomMenu(int containerId, PlayerInventory inventory, ContainerLevelAccess access)
        : base(MenuTypes.LOOM, containerId)
    {
        OwnerInventory = inventory;
        Access = access;
        _bannerSlot = AddSlot(new BannerItemSlot(_inputContainer, 0, 13, 26));
        _dyeSlot = AddSlot(new DyeItemSlot(_inputContainer, 1, 33, 26));
        _patternSlot = AddSlot(new PatternItemSlot(_inputContainer, 2, 23, 45));
        _resultSlot = AddSlot(new LoomResultSlot(this, _outputContainer, 0, 143, 57));
        for (var i = 0; i < PlayerInventory.MainSlots; i++)
            AddSlot(new Slot(inventory, PlayerInventory.HotbarSlots + i, 8 + i % 9 * 18, 84 + i / 9 * 18));
        for (var i = 0; i < PlayerInventory.HotbarSlots; i++)
            AddSlot(new Slot(inventory, i, 8 + i * 18, 142));
        AddDataSlot(_selectedBannerPatternIndex);
    }

    public LoomMenu(int containerId, PlayerInventory inventory)
        : this(containerId, inventory, ContainerLevelAccess.Null)
    {
    }

    public ContainerLevelAccess Access { get; }

    public int SelectedBannerPatternIndex => _selectedBannerPatternIndex.Get(0);

    public IReadOnlyList<object> SelectablePatterns => Array.Empty<object>();

    public Slot BannerSlot => _bannerSlot;

    public Slot DyeSlot => _dyeSlot;

    public Slot PatternSlot => _patternSlot;

    public Slot ResultSlot => _resultSlot;

    public override bool StillValid(ServerPlayer player) => true;

    //ClickMenuButton selects a banner pattern, maps to vanilla clickMenuButton; no pattern registry is ported
    public override bool ClickMenuButton(ServerPlayer player, int buttonId) => false;

    public override void SlotsChanged(Container container)
    {
        if (!ReferenceEquals(container, _inputContainer)) return;
        _selectedBannerPatternIndex.Set(0, PatternNotSet);
        _outputContainer.SetItem(0, ItemStack.Empty);
    }

    //ConsumePayment consumes one banner and one dye after the result is taken, maps to vanilla onTake
    internal void ConsumePayment()
    {
        _bannerSlot.Remove(1);
        _dyeSlot.Remove(1);
        if (!_bannerSlot.HasItem() || !_dyeSlot.HasItem())
            _selectedBannerPatternIndex.Set(0, PatternNotSet);
    }

    public override ItemStack QuickMoveStack(ServerPlayer player, int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (!slot.HasItem()) return ItemStack.Empty;
        var moved = slot.GetItem().Copy();
        var stack = slot.GetItem();
        if (slotIndex < InvSlotStart)
        {
            if (!MoveItemStackTo(stack, InvSlotStart, HotbarSlotEnd, true)) return ItemStack.Empty;
        }
        else if (BannerItemSlot.Matches(stack))
        {
            if (!MoveItemStackTo(stack, _bannerSlot.Index, _bannerSlot.Index + 1, false)) return ItemStack.Empty;
        }
        else if (DyeItemSlot.Matches(stack))
        {
            if (!MoveItemStackTo(stack, _dyeSlot.Index, _dyeSlot.Index + 1, false)) return ItemStack.Empty;
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

    private sealed class BannerItemSlot : Slot
    {
        public BannerItemSlot(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override bool MayPlace(ItemStack stack) => Matches(stack);

        //Matches whether the item is a banner, maps to vanilla BannerItem instanceof check
        public static bool Matches(ItemStack stack)
            => !stack.IsEmpty() && stack.GetItem().Id.Path.EndsWith("_banner", StringComparison.Ordinal);
    }

    private sealed class DyeItemSlot : Slot
    {
        public DyeItemSlot(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override bool MayPlace(ItemStack stack) => Matches(stack);

        public static bool Matches(ItemStack stack)
            => !stack.IsEmpty() && stack.GetItem().Id.Path.EndsWith("_dye", StringComparison.Ordinal);
    }

    private sealed class PatternItemSlot : Slot
    {
        public PatternItemSlot(Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y)
        {
        }

        public override bool MayPlace(ItemStack stack)
            => !stack.IsEmpty() && stack.GetItem().Id.Path.EndsWith("_banner_pattern", StringComparison.Ordinal);
    }

    private sealed class LoomResultSlot : Slot
    {
        private readonly LoomMenu _menu;

        public LoomResultSlot(LoomMenu menu, Container container, int slotIndex, int x, int y)
            : base(container, slotIndex, x, y) => _menu = menu;

        public override bool MayPlace(ItemStack stack) => false;

        public override ItemStack Remove(int count)
        {
            var taken = base.Remove(count);
            if (!taken.IsEmpty()) _menu.ConsumePayment();
            return taken;
        }
    }
}
