using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;

namespace NacreCraft.Game.World.Items.Equipment;

//ArmorType the four humanoid armor pieces plus body armor, maps to vanilla net.minecraft.world.item.equipment.ArmorType
//Each type carries the equipment slot, the unit durability multiplier and its serialized name
public enum ArmorType
{
    Helmet,
    Chestplate,
    Leggings,
    Boots,
    Body
}

//ArmorTypes armor type codec and lookups, mirrors the methods of the vanilla enum
public static class ArmorTypes
{
    //Names serialized names, the index matches the enum value
    private static readonly string[] Names = { "helmet", "chestplate", "leggings", "boots", "body" };

    //UnitDurabilities durability multiplier per type, maps to the vanilla constructor values
    private static readonly int[] UnitDurabilities = { 11, 16, 15, 13, 16 };

    //Codec encodes by serialized name, maps to vanilla ArmorType.CODEC
    public static readonly Codec<ArmorType> Codec = Codecs.String.ComapFlatMap(
        name =>
        {
            for (var i = 0; i < Names.Length; i++)
                if (Names[i] == name) return DataResult<ArmorType>.Success((ArmorType)i);
            return DataResult<ArmorType>.Error(() => $"unknown armor type: {name}");
        },
        type => Names[(int)type]);

    //GetDurability the durability for this piece at the given material multiplier, maps to vanilla getDurability
    public static int GetDurability(this ArmorType type, int multiplier)
        => UnitDurabilities[(int)type] * multiplier;

    //GetSlot the equipment slot of this piece, maps to vanilla getSlot
    public static EquipmentSlot GetSlot(this ArmorType type) => type switch
    {
        ArmorType.Helmet => EquipmentSlot.HEAD,
        ArmorType.Chestplate => EquipmentSlot.CHEST,
        ArmorType.Leggings => EquipmentSlot.LEGS,
        ArmorType.Boots => EquipmentSlot.FEET,
        _ => EquipmentSlot.BODY
    };

    //GetName the serialized name, maps to vanilla getName/getSerializedName
    public static string GetName(this ArmorType type) => Names[(int)type];
}
