using NacreCraft.Codec;
using NacreCraft.Game.World.Loot;
using NacreCraft.Registry;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//SlotSource produces a slot collection from a loot context, maps to vanilla net.minecraft.world.item.slot.SlotSource
//Vanilla extends LootContextUser for validation; this project has no validatable loot layer yet
public interface SlotSource : RegistrySlotSource
{
    MapCodec<RegistrySlotSource> Codec();

    SlotCollection Provide(LootContext context);
}
