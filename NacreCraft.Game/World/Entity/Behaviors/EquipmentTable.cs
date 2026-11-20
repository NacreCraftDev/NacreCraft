using LootTableType = NacreCraft.Game.World.Loot.LootTable;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//EquipmentTable a loot table plus per-slot drop chances for mob equipment, maps to vanilla net.minecraft.world.entity.EquipmentTable
//NC has no key codec for loot tables yet, so the table is a plain data holder; the loot framework resolves the key at spawn time
public sealed record EquipmentTable(ResourceKey<LootTableType> Table, IReadOnlyDictionary<EquipmentSlot, float> SlotDropChances)
{
    public EquipmentTable(ResourceKey<LootTableType> table, float dropChance)
        : this(table, Enum.GetValues<EquipmentSlot>().ToDictionary(slot => slot, _ => dropChance))
    {
    }

    //Vanilla-style accessors
    public ResourceKey<LootTableType> LootTable => Table;
    public IReadOnlyDictionary<EquipmentSlot, float> SlotDropChancesMap => SlotDropChances;
}
