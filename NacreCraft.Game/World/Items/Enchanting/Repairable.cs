using NacreCraft.Codec;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Enchanting;

//Repairable the items an anvil may repair with, maps to vanilla net.minecraft.world.item.enchantment.Repairable
//A per-item component listing the repair materials
public sealed record Repairable(HolderSet<Item> Items)
{
    //Codec persistence codec, field name items, maps to vanilla CODEC
    public static readonly Codec<Repairable> Codec = RecordCodecBuilder.Of1<Repairable, HolderSet<Item>>(
        HolderSetCodecs.ItemSet.FieldOf("items").ForGetter<Repairable, HolderSet<Item>>(repairable => repairable.Items),
        items => new Repairable(items));

    //IsValidRepairItem whether the stack is one of the repair materials, maps to vanilla isValidRepairItem
    public bool IsValidRepairItem(ItemStack stack)
        => stack.GetTypeHolder() is { } holder && Items.Contains(holder);
}
