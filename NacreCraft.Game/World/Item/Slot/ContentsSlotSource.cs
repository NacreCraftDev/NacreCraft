using NacreCraft.Codec;
using NacreCraft.Game.World.Items.Component;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//ContentsSlotSource expands the items stored inside one container component, maps to vanilla net.minecraft.world.item.slot.ContentsSlotSource
public sealed class ContentsSlotSource : TransformedSlotSource
{
    public static readonly MapCodec<ContentsSlotSource> MAP_CODEC = RecordCodecBuilder.Of2(
        SlotSources.CODEC.FieldOf("slot_source").ForGetter((ContentsSlotSource source) => source.SlotSourceValue),
        ContainerComponentManipulators.CODEC.FieldOf("component")
            .ForGetter((ContentsSlotSource source) => source.Component),
        (slotSource, component) => new ContentsSlotSource(slotSource, component));

    public static readonly MapCodec<RegistrySlotSource> ControlCodec =
        new XmapMapCodec<ContentsSlotSource, RegistrySlotSource>(MAP_CODEC, value => value,
            source => (ContentsSlotSource)source);

    private ContentsSlotSource(SlotSource slotSource, ContainerComponentManipulator component) : base(slotSource)
        => Component = component;

    public ContainerComponentManipulator Component { get; }

    public override MapCodec<RegistrySlotSource> Codec() => ControlCodec;

    protected override SlotCollection Transform(SlotCollection slots)
        => slots.FlatMap(stack => new ContentsCollection(Component.GetContents(stack)));

    private sealed class ContentsCollection(IEnumerable<NacreCraft.Game.World.Items.ItemStack> items) : SlotCollection
    {
        public IEnumerable<NacreCraft.Game.World.Items.ItemStack> ItemCopies() => items;
    }
}
