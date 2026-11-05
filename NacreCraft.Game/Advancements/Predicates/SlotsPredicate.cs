using NacreCraft.Codec;
using NacreCraft.Game.World.Inventory;

namespace NacreCraft.Game.Advancements.Predicates;

//SlotsPredicate slot contents predicate, maps to vanilla net.minecraft.advancements.predicates.SlotsPredicate
//Each named slot range must contain at least one stack matching its item predicate
//Vanilla matches against SlotProvider; NC only has the menu as a slot source, so menus are matched directly
public sealed record SlotsPredicate(IReadOnlyDictionary<SlotRange, ItemPredicate> Slots)
{
    //Codec slot range to item predicate map, maps to vanilla CODEC
    public static readonly Codec<SlotsPredicate> Codec =
        Codecs.UnboundedMap(SlotRanges.Codec, ItemPredicate.Codec).ComapFlatMap(
            map => DataResult<SlotsPredicate>.Success(new SlotsPredicate(map)),
            predicate => predicate.Slots.ToDictionary(entry => entry.Key, entry => entry.Value));

    //Matches every range must contain a matching stack, maps to vanilla matches
    public bool Matches(AbstractContainerMenu menu)
    {
        foreach (var entry in Slots)
            if (!MatchSlots(menu, entry.Value, entry.Key.Slots))
                return false;
        return true;
    }

    //MatchSlots succeeds when any slot in the range matches the item predicate
    private static bool MatchSlots(AbstractContainerMenu menu, ItemPredicate test, IReadOnlyList<int> slots)
    {
        foreach (var slotId in slots)
        {
            if (!menu.IsValidSlotIndex(slotId)) continue;
            if (!test.Test(menu.GetSlot(slotId).GetItem())) continue;
            return true;
        }
        return false;
    }
}
