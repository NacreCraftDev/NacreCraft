using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Enchanting;

//EnchantmentCost the level cost of an enchantment, maps to vanilla Enchantment.Cost
public sealed record EnchantmentCost(int Base, int PerLevelAboveFirst)
{
    public static readonly Codec<EnchantmentCost> Codec = RecordCodecBuilder.Of2(
        Codecs.Int.FieldOf("base").ForGetter((EnchantmentCost cost) => cost.Base),
        Codecs.Int.FieldOf("per_level_above_first").ForGetter((EnchantmentCost cost) => cost.PerLevelAboveFirst),
        (baseValue, perLevelAboveFirst) => new EnchantmentCost(baseValue, perLevelAboveFirst));

    //Calculate the cost at a level, maps to vanilla calculate
    public int Calculate(int level) => Base + PerLevelAboveFirst * (level - 1);
}

//EnchantmentDefinition the static definition of an enchantment, maps to vanilla Enchantment.EnchantmentDefinition
public sealed record EnchantmentDefinition(HolderSet<Item> SupportedItems, Optional<HolderSet<Item>> PrimaryItems,
    int Weight, int MaxLevel, EnchantmentCost MinCost, EnchantmentCost MaxCost, int AnvilCost,
    IReadOnlyList<EquipmentSlotGroup> Slots)
{
    //Codec persistence codec, field names supported_items/primary_items/weight/max_level/min_cost/max_cost/anvil_cost/slots,
    //maps to vanilla CODEC
    public static readonly Codec<EnchantmentDefinition> Codec = RecordCodecBuilder.Of8(
        HolderSetCodecs.ItemSet.FieldOf("supported_items")
            .ForGetter((EnchantmentDefinition definition) => definition.SupportedItems),
        HolderSetCodecs.ItemSet.OptionalFieldOf("primary_items")
            .ForGetter((EnchantmentDefinition definition) => definition.PrimaryItems),
        Codecs.Int.FieldOf("weight").ForGetter((EnchantmentDefinition definition) => definition.Weight),
        Codecs.Int.FieldOf("max_level").ForGetter((EnchantmentDefinition definition) => definition.MaxLevel),
        EnchantmentCost.Codec.FieldOf("min_cost").ForGetter((EnchantmentDefinition definition) => definition.MinCost),
        EnchantmentCost.Codec.FieldOf("max_cost").ForGetter((EnchantmentDefinition definition) => definition.MaxCost),
        Codecs.Int.FieldOf("anvil_cost").ForGetter((EnchantmentDefinition definition) => definition.AnvilCost),
        EquipmentSlotGroups.Codec.ListOf().FieldOf("slots")
            .ForGetter((EnchantmentDefinition definition) => definition.Slots),
        (supportedItems, primaryItems, weight, maxLevel, minCost, maxCost, anvilCost, slots) =>
            new EnchantmentDefinition(supportedItems, primaryItems, weight, maxLevel, minCost, maxCost, anvilCost,
                slots));
}

//Enchantment one enchantment entry, maps to vanilla Enchantment
//The registry layer only declares the marker interface, so this record carries the whole definition and effect table
public sealed record Enchantment(NacreCraft.Network.Chat.Component Description, EnchantmentDefinition Definition,
    HolderSet<NacreCraft.Registry.Enchantment> ExclusiveSet, DataComponentMap Effects)
    : NacreCraft.Registry.Enchantment
{
    //DirectCodec the codec of one enchantment entry, maps to vanilla DIRECT_CODEC
    public static readonly Codec<Enchantment> DirectCodec = RecordCodecBuilder.Of4(
        NacreCraft.Network.Chat.ComponentSerialization.JsonCodec.FieldOf("description")
            .ForGetter((Enchantment enchantment) => enchantment.Description),
        EnchantmentDefinition.Codec.ForGetter((Enchantment enchantment) => enchantment.Definition),
        HolderSetCodecs.EnchantmentSet
            .OptionalFieldOf("exclusive_set", DirectHolderSet<NacreCraft.Registry.Enchantment>.Empty)
            .ForGetter((Enchantment enchantment) => enchantment.ExclusiveSet),
        EnchantmentEffectComponents.CODEC.OptionalFieldOf("effects", DataComponentMap.Empty)
            .ForGetter((Enchantment enchantment) => enchantment.Effects),
        (description, definition, exclusiveSet, effects) =>
            new Enchantment(description, definition, exclusiveSet, effects));

    //ElementCodec the codec the registry loader registers, widened to the registry element type
    public static readonly Codec<NacreCraft.Registry.Enchantment> ElementCodec =
        DirectCodec.Xmap<Enchantment, NacreCraft.Registry.Enchantment>(value => value, value => (Enchantment)value);
}
