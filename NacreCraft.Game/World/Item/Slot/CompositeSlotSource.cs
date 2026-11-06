using NacreCraft.Codec;
using NacreCraft.Game.World.Loot;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//CompositeSlotSource a source built from several terms, maps to vanilla net.minecraft.world.item.slot.CompositeSlotSource
public abstract class CompositeSlotSource : SlotSource
{
    protected readonly IReadOnlyList<SlotSource> Terms;
    private readonly Func<LootContext, SlotCollection> _compositeSlotSource;

    protected CompositeSlotSource(IReadOnlyList<SlotSource> terms)
    {
        Terms = terms;
        _compositeSlotSource = SlotSources.Group(terms);
    }

    protected static MapCodec<T> CreateCodec<T>(Func<IReadOnlyList<SlotSource>, T> factory)
        where T : CompositeSlotSource
        => RecordCodecBuilder.Of1(
            SlotSources.CODEC.ListOf().FieldOf("terms").ForGetter((T source) => source.Terms),
            factory);

    public abstract MapCodec<RegistrySlotSource> Codec();

    public SlotCollection Provide(LootContext context) => _compositeSlotSource(context);
}
