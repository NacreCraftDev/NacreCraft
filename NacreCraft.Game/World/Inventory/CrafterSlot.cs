using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//CrafterSlot crafter grid slot, maps to vanilla net.minecraft.world.inventory.CrafterSlot
//A disabled slot rejects placement, every change is forwarded to the menu so it can refresh the preview
public sealed class CrafterSlot : Slot
{
    private readonly CrafterMenu _menu;

    public CrafterSlot(Container container, int slotIndex, int x, int y, CrafterMenu menu)
        : base(container, slotIndex, x, y) => _menu = menu;

    public override bool MayPlace(ItemStack stack) => !_menu.IsSlotDisabled(Index) && base.MayPlace(stack);

    public override void SetChanged()
    {
        base.SetChanged();
        _menu.SlotsChanged(Container);
    }
}
