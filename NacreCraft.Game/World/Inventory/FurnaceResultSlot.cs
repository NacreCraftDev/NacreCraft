using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//FurnaceResultSlot smelting result slot, maps to vanilla net.minecraft.world.inventory.FurnaceResultSlot
//Nothing can be placed; taking counts the removed items so the block entity can award experience
//The award hook needs the server player and the furnace block entity, neither is known here, so take counting is all that is ported
public sealed class FurnaceResultSlot : Slot
{
    private int _removeCount;

    public FurnaceResultSlot(Container container, int slotIndex, int x, int y)
        : base(container, slotIndex, x, y)
    {
    }

    public override bool MayPlace(ItemStack stack) => false;

    public override ItemStack Remove(int count)
    {
        if (HasItem()) _removeCount += Math.Min(count, GetItem().GetCount());
        return base.Remove(count);
    }

    //RemoveCount number of items taken since the last reset
    public int RemoveCount => _removeCount;

    public void ResetRemoveCount() => _removeCount = 0;
}
