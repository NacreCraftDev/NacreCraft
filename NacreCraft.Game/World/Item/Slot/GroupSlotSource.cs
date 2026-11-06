using NacreCraft.Codec;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//GroupSlotSource concatenates several sources, maps to vanilla net.minecraft.world.item.slot.GroupSlotSource
public sealed class GroupSlotSource : CompositeSlotSource
{
    public static readonly MapCodec<GroupSlotSource> MAP_CODEC = CreateCodec(terms => new GroupSlotSource(terms));

    public static readonly Codec<GroupSlotSource> INLINE_CODEC = SlotSources.CODEC.ListOf().ComapFlatMap(
        terms => DataResult<GroupSlotSource>.Success(new GroupSlotSource(terms)),
        source => source.Terms);

    public static readonly MapCodec<RegistrySlotSource> ControlCodec =
        new XmapMapCodec<GroupSlotSource, RegistrySlotSource>(MAP_CODEC, value => value,
            source => (GroupSlotSource)source);

    private GroupSlotSource(IReadOnlyList<SlotSource> terms) : base(terms)
    {
    }

    public override MapCodec<RegistrySlotSource> Codec() => ControlCodec;
}
