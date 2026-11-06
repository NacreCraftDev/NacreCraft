using NacreCraft.Codec;
using NacreCraft.Util;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//LimitSlotSource caps the number of provided slots, maps to vanilla net.minecraft.world.item.slot.LimitSlotSource
public sealed class LimitSlotSource : TransformedSlotSource
{
    public static readonly MapCodec<LimitSlotSource> MAP_CODEC = RecordCodecBuilder.Of2(
        SlotSources.CODEC.FieldOf("slot_source").ForGetter((LimitSlotSource source) => source.SlotSourceValue),
        ExtraCodecs.PositiveInt.FieldOf("limit").ForGetter((LimitSlotSource source) => source.Limit),
        (slotSource, limit) => new LimitSlotSource(slotSource, limit));

    public static readonly MapCodec<RegistrySlotSource> ControlCodec =
        new XmapMapCodec<LimitSlotSource, RegistrySlotSource>(MAP_CODEC, value => value,
            source => (LimitSlotSource)source);

    private LimitSlotSource(SlotSource slotSource, int limit) : base(slotSource) => Limit = limit;

    public int Limit { get; }

    public override MapCodec<RegistrySlotSource> Codec() => ControlCodec;

    protected override SlotCollection Transform(SlotCollection slots) => slots.Limit(Limit);
}
