using NacreCraft.Game.World.Inventory;
using NacreCraft.Game.World.Items;

namespace NacreCraft.Game.World.Items.Slots;

//SlotCollection an iterable set of item copies, maps to vanilla net.minecraft.world.item.slot.SlotCollection
public interface SlotCollection
{
    IEnumerable<ItemStack> ItemCopies();

    SlotCollection Filter(Func<ItemStack, bool> predicate) => new Filtered(this, predicate);

    SlotCollection FlatMap(Func<ItemStack, SlotCollection> mapper) => new FlatMapped(this, mapper);

    SlotCollection Limit(int limit) => new Limited(this, limit);

    public sealed record Filtered(SlotCollection Slots, Func<ItemStack, bool> Predicate) : SlotCollection
    {
        public IEnumerable<ItemStack> ItemCopies() => Slots.ItemCopies().Where(Predicate);

        public SlotCollection Filter(Func<ItemStack, bool> predicate)
            => new Filtered(Slots, stack => Predicate(stack) && predicate(stack));
    }

    public sealed record FlatMapped(SlotCollection Slots, Func<ItemStack, SlotCollection> Mapper) : SlotCollection
    {
        public IEnumerable<ItemStack> ItemCopies() => Slots.ItemCopies().SelectMany(stack => Mapper(stack).ItemCopies());
    }

    public sealed record Limited(SlotCollection Slots, int Max) : SlotCollection
    {
        public IEnumerable<ItemStack> ItemCopies() => Slots.ItemCopies().Take(Max);

        public SlotCollection Limit(int limit) => new Limited(Slots, Math.Min(Max, limit));
    }
}

//SlotCollections the static constructors of SlotCollection, maps to the static members of the vanilla interface
public static class SlotCollections
{
    public static readonly SlotCollection EMPTY = new EmptyCollection();

    public static SlotCollection Of(SlotAccess slotAccess) => new SingleCollection(slotAccess);

    public static SlotCollection Of(IReadOnlyCollection<SlotAccess> slots) => slots.Count switch
    {
        0 => EMPTY,
        1 => Of(slots.First()),
        _ => new MultiCollection(slots)
    };

    public static SlotCollection Concat(SlotCollection first, SlotCollection second)
        => new ConcatCollection(new[] { first, second });

    public static SlotCollection Concat(IReadOnlyList<SlotCollection> terms) => terms.Count switch
    {
        0 => EMPTY,
        1 => terms[0],
        2 => Concat(terms[0], terms[1]),
        _ => new ConcatCollection(terms)
    };

    private sealed class EmptyCollection : SlotCollection
    {
        public IEnumerable<ItemStack> ItemCopies() => Enumerable.Empty<ItemStack>();
    }

    private sealed class SingleCollection(SlotAccess slotAccess) : SlotCollection
    {
        public IEnumerable<ItemStack> ItemCopies() => new[] { slotAccess.Get().Copy() };
    }

    private sealed class MultiCollection(IReadOnlyCollection<SlotAccess> slots) : SlotCollection
    {
        public IEnumerable<ItemStack> ItemCopies() => slots.Select(slot => slot.Get().Copy());
    }

    private sealed class ConcatCollection(IReadOnlyList<SlotCollection> terms) : SlotCollection
    {
        public IEnumerable<ItemStack> ItemCopies() => terms.SelectMany(term => term.ItemCopies());
    }
}
