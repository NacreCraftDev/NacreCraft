using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Inventory;

//StackedItemContents item count accumulator, maps to vanilla net.minecraft.world.entity.player.StackedItemContents
//The recipe book uses it to tell which ingredients are available; only the simple accounting path is ported
public sealed class StackedItemContents
{
    private readonly Dictionary<NacreCraft.Registry.Item, int> _counts = new();

    //AccountSimpleStack adds a stack's count, maps to vanilla accountSimpleStack
    public void AccountSimpleStack(ItemStack stack)
    {
        if (stack.IsEmpty()) return;
        var item = stack.GetItem();
        _counts[item] = _counts.GetValueOrDefault(item) + stack.GetCount();
    }

    //GetCount number of items of the given type collected so far
    public int GetCount(NacreCraft.Registry.Item item) => _counts.GetValueOrDefault(item);

    public void Clear() => _counts.Clear();
}
