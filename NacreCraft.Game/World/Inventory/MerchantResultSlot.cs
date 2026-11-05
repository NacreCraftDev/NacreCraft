using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//MerchantResultSlot trade result slot, maps to vanilla net.minecraft.world.inventory.MerchantResultSlot
//Nothing can be placed; taking it consumes the payments and grants the merchant experience
//The trade bookkeeping needs the Merchant type, which is not ported, so only the take counting is kept
public sealed class MerchantResultSlot : Slot
{
    private int _removeCount;

    public MerchantResultSlot(MerchantContainer slots, int slotIndex, int x, int y)
        : base(slots, slotIndex, x, y)
    {
    }

    public override bool MayPlace(ItemStack stack) => false;

    public override ItemStack Remove(int count)
    {
        if (HasItem()) _removeCount += Math.Min(count, GetItem().GetCount());
        return base.Remove(count);
    }

    public int RemoveCount => _removeCount;

    public void ResetRemoveCount() => _removeCount = 0;
}
