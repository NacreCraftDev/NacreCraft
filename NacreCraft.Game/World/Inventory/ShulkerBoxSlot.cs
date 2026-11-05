using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//ShulkerBoxSlot shulker box slot, maps to vanilla net.minecraft.world.inventory.ShulkerBoxSlot
//Rejects items that cannot fit inside container items, which keeps shulker boxes from nesting
public sealed class ShulkerBoxSlot : Slot
{
    public ShulkerBoxSlot(Container container, int slotIndex, int x, int y)
        : base(container, slotIndex, x, y)
    {
    }

    public override bool MayPlace(ItemStack stack)
        => !stack.IsEmpty() && stack.GetItem().CanFitInsideContainerItems && base.MayPlace(stack);
}
