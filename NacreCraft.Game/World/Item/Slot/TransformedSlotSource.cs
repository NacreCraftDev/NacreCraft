using NacreCraft.Codec;
using NacreCraft.Game.World.Loot;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//TransformedSlotSource applies one transform to another source, maps to vanilla net.minecraft.world.item.slot.TransformedSlotSource
public abstract class TransformedSlotSource : SlotSource
{
    protected readonly SlotSource SlotSourceValue;

    protected TransformedSlotSource(SlotSource slotSource) => SlotSourceValue = slotSource;

    public abstract MapCodec<RegistrySlotSource> Codec();

    protected abstract SlotCollection Transform(SlotCollection slots);

    public SlotCollection Provide(LootContext context) => Transform(SlotSourceValue.Provide(context));
}
