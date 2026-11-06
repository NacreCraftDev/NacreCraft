using NacreCraft.Codec;
using NacreCraft.Game.Advancements.Predicates;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//FilteredSlotSource keeps only the items matching the predicate, maps to vanilla net.minecraft.world.item.slot.FilteredSlotSource
public sealed class FilteredSlotSource : TransformedSlotSource
{
    public static readonly MapCodec<FilteredSlotSource> MAP_CODEC = RecordCodecBuilder.Of2(
        SlotSources.CODEC.FieldOf("slot_source").ForGetter((FilteredSlotSource source) => source.SlotSourceValue),
        ItemPredicate.Codec.FieldOf("item_filter").ForGetter((FilteredSlotSource source) => source.Filter),
        (slotSource, filter) => new FilteredSlotSource(slotSource, filter));

    public static readonly MapCodec<RegistrySlotSource> ControlCodec =
        new XmapMapCodec<FilteredSlotSource, RegistrySlotSource>(MAP_CODEC, value => value,
            source => (FilteredSlotSource)source);

    private FilteredSlotSource(SlotSource slotSource, ItemPredicate filter) : base(slotSource) => Filter = filter;

    public ItemPredicate Filter { get; }

    public override MapCodec<RegistrySlotSource> Codec() => ControlCodec;

    protected override SlotCollection Transform(SlotCollection slots)
        => slots.Filter(stack => Filter.Test(stack));
}
