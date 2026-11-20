using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using AttributeModifier = NacreCraft.Registry.EntityAttribute.AttributeModifier;
using AttributeOperation = NacreCraft.Registry.EntityAttribute.AttributeOperation;
using Attributes = NacreCraft.Registry.EntityAttribute.Attributes;

namespace NacreCraft.Game.World.Items.Equipment;

//ArmorMaterial an armor material definition, maps to vanilla net.minecraft.world.item.equipment.ArmorMaterial
//Holds the durability multiplier, per-piece defense points, enchantability, equip sound, toughness, knockback resistance,
//repair tag and equipment asset key; CreateAttributes turns it into the armor attribute modifiers of one piece
public sealed class ArmorMaterial : IEquatable<ArmorMaterial>
{
    public ArmorMaterial(
        int durability,
        IReadOnlyDictionary<ArmorType, int> defense,
        int enchantmentValue,
        SoundEvent equipSound,
        float toughness,
        float knockbackResistance,
        TagKey<Item> repairIngredient,
        ResourceKey<EquipmentAsset> assetId)
    {
        Durability = durability;
        Defense = defense;
        EnchantmentValue = enchantmentValue;
        EquipSound = equipSound;
        Toughness = toughness;
        KnockbackResistance = knockbackResistance;
        RepairIngredient = repairIngredient;
        AssetId = assetId;
    }

    //Durability durability multiplier, maps to vanilla durability()
    public int Durability { get; }

    //Defense defense points per armor type, maps to vanilla defense()
    public IReadOnlyDictionary<ArmorType, int> Defense { get; }

    //EnchantmentValue enchantability, maps to vanilla enchantmentValue()
    public int EnchantmentValue { get; }

    //EquipSound sound played when equipping a piece, maps to vanilla equipSound()
    //NC sounds are values rather than registry holders
    public SoundEvent EquipSound { get; }

    //Toughness armor toughness, maps to vanilla toughness()
    public float Toughness { get; }

    //KnockbackResistance knockback resistance, maps to vanilla knockbackResistance()
    public float KnockbackResistance { get; }

    //RepairIngredient item tag that repairs the armor, maps to vanilla repairIngredient()
    public TagKey<Item> RepairIngredient { get; }

    //AssetId equipment asset key, maps to vanilla assetId()
    public ResourceKey<EquipmentAsset> AssetId { get; }

    //CreateAttributes builds the armor and toughness modifiers for one piece, maps to vanilla createAttributes
    //Knockback resistance is only added when the material has any, matching vanilla
    public ItemAttributeModifiers CreateAttributes(ArmorType type)
    {
        var defense = Defense.TryGetValue(type, out var points) ? points : 0;
        var modifiers = ItemAttributeModifiers.CreateBuilder();
        var slotGroup = EquipmentSlotGroups.BySlot(type.GetSlot());
        var modifierId = Identifier.WithDefaultNamespace("armor." + type.GetName());
        modifiers.Add(BuiltInRegistries.ATTRIBUTE.WrapAsHolder(Attributes.Armor),
            new AttributeModifier(modifierId, defense, AttributeOperation.AddValue), slotGroup);
        modifiers.Add(BuiltInRegistries.ATTRIBUTE.WrapAsHolder(Attributes.ArmorToughness),
            new AttributeModifier(modifierId, Toughness, AttributeOperation.AddValue), slotGroup);
        if (KnockbackResistance > 0f)
            modifiers.Add(BuiltInRegistries.ATTRIBUTE.WrapAsHolder(Attributes.KnockbackResistance),
                new AttributeModifier(modifierId, KnockbackResistance, AttributeOperation.AddValue), slotGroup);
        return modifiers.Build();
    }

    public bool Equals(ArmorMaterial? other)
        => other is not null
           && Durability == other.Durability
           && EnchantmentValue == other.EnchantmentValue
           && EquipSound.Equals(other.EquipSound)
           && Toughness.Equals(other.Toughness)
           && KnockbackResistance.Equals(other.KnockbackResistance)
           && RepairIngredient.Equals(other.RepairIngredient)
           && AssetId.Equals(other.AssetId)
           && Defense.Count == other.Defense.Count
           && Defense.All(pair => other.Defense.TryGetValue(pair.Key, out var value) && value == pair.Value);

    public override bool Equals(object? obj) => Equals(obj as ArmorMaterial);

    public override int GetHashCode()
        => HashCode.Combine(Durability, EnchantmentValue, EquipSound, Toughness, KnockbackResistance, AssetId);

    public override string ToString()
        => $"ArmorMaterial[durability={Durability}, enchantment={EnchantmentValue}, toughness={Toughness}]";
}
