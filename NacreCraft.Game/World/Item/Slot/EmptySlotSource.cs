using NacreCraft.Codec;
using NacreCraft.Game.World.Loot;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//EmptySlotSource always provides no slots, maps to vanilla net.minecraft.world.item.slot.EmptySlotSource
public sealed record EmptySlotSource : SlotSource
{
    public static readonly MapCodec<EmptySlotSource> MAP_CODEC = Codecs.Unit(new EmptySlotSource());

    public static readonly MapCodec<RegistrySlotSource> ControlCodec =
        new XmapMapCodec<EmptySlotSource, RegistrySlotSource>(MAP_CODEC, value => value,
            source => (EmptySlotSource)source);

    public MapCodec<RegistrySlotSource> Codec() => ControlCodec;

    public SlotCollection Provide(LootContext context) => SlotCollections.EMPTY;
}
