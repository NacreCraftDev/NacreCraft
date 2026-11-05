using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//NonInteractiveResultSlot result slot the player cannot take from, maps to vanilla net.minecraft.world.inventory.NonInteractiveResultSlot
//The crafter previews its result through it: the client sees the item but only the block entity may consume it
public sealed class NonInteractiveResultSlot : Slot
{
    public NonInteractiveResultSlot(Container container, int slotIndex, int x, int y)
        : base(container, slotIndex, x, y)
    {
    }

    public override bool MayPlace(ItemStack stack) => false;

    public override ItemStack Remove(int count) => ItemStack.Empty;

    public override ItemStack SafeTake(int amount, int maxAmount, Player player) => ItemStack.Empty;

    //IsFake the client renders it but never syncs it as a real slot
    public bool IsFake => true;
}
