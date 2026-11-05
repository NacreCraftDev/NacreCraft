using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Inventory;

//FurnaceFuelSlot fuel slot of a furnace menu, maps to vanilla net.minecraft.world.inventory.FurnaceFuelSlot
//Accepts only items the menu reports as fuel plus empty buckets, and a bucket never stacks
public sealed class FurnaceFuelSlot : Slot
{
    private readonly Func<ItemStack, bool> _isFuel;

    public FurnaceFuelSlot(Func<ItemStack, bool> isFuel, Container container, int slotIndex, int x, int y)
        : base(container, slotIndex, x, y) => _isFuel = isFuel;

    public override bool MayPlace(ItemStack stack) => _isFuel(stack) || IsBucket(stack);

    public override int GetMaxStackSize() => IsBucket(GetItem()) ? 1 : base.GetMaxStackSize();

    public static bool IsBucket(ItemStack stack)
    {
        if (stack.IsEmpty()) return false;
        var id = Identifier.WithDefaultNamespace("bucket");
        if (!BuiltInRegistries.ITEM.ContainsKey(id)) return false;
        return ReferenceEquals(stack.GetItem(), BuiltInRegistries.ITEM.GetValue(id));
    }
}
