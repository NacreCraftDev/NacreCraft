using NacreCraft.Game.World.Items.Equipment;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Equipment;

//ArmorMaterials built-in armor materials, maps to vanilla net.minecraft.world.item.equipment.ArmorMaterials
//Values match the vanilla 26.2 table; the equip sound is a value because NC sounds are not registry entries
public static class ArmorMaterials
{
    public static readonly ArmorMaterial Leather = new(5, MakeDefense(1, 2, 3, 1, 3), 15,
        SoundEvents.ArmorEquipLeather, 0f, 0f, ItemTags.RepairsLeatherArmor, EquipmentAssets.Leather);

    public static readonly ArmorMaterial Copper = new(11, MakeDefense(1, 3, 4, 2, 4), 8,
        SoundEvents.ArmorEquipCopper, 0f, 0f, ItemTags.RepairsCopperArmor, EquipmentAssets.Copper);

    public static readonly ArmorMaterial Chainmail = new(15, MakeDefense(1, 4, 5, 2, 4), 12,
        SoundEvents.ArmorEquipChain, 0f, 0f, ItemTags.RepairsChainArmor, EquipmentAssets.Chainmail);

    public static readonly ArmorMaterial Iron = new(15, MakeDefense(2, 5, 6, 2, 5), 9,
        SoundEvents.ArmorEquipIron, 0f, 0f, ItemTags.RepairsIronArmor, EquipmentAssets.Iron);

    public static readonly ArmorMaterial Gold = new(7, MakeDefense(1, 3, 5, 2, 7), 25,
        SoundEvents.ArmorEquipGold, 0f, 0f, ItemTags.RepairsGoldArmor, EquipmentAssets.Gold);

    public static readonly ArmorMaterial Diamond = new(33, MakeDefense(3, 6, 8, 3, 11), 10,
        SoundEvents.ArmorEquipDiamond, 2f, 0f, ItemTags.RepairsDiamondArmor, EquipmentAssets.Diamond);

    public static readonly ArmorMaterial TurtleScute = new(25, MakeDefense(2, 5, 6, 2, 5), 9,
        SoundEvents.ArmorEquipTurtle, 0f, 0f, ItemTags.RepairsTurtleHelmet, EquipmentAssets.TurtleScute);

    public static readonly ArmorMaterial Netherite = new(37, MakeDefense(3, 6, 8, 3, 19), 15,
        SoundEvents.ArmorEquipNetherite, 3f, 0.1f, ItemTags.RepairsNetheriteArmor, EquipmentAssets.Netherite);

    public static readonly ArmorMaterial ArmadilloScute = new(4, MakeDefense(3, 6, 8, 3, 11), 10,
        SoundEvents.ArmorEquipWolf, 0f, 0f, ItemTags.RepairsWolfArmor, EquipmentAssets.ArmadilloScute);

    //MakeDefense builds the per-piece defense map, argument order matches vanilla makeDefense(boots, legs, chest, helm, body)
    private static IReadOnlyDictionary<ArmorType, int> MakeDefense(int boots, int legs, int chest, int helm, int body)
        => new Dictionary<ArmorType, int>
        {
            [ArmorType.Boots] = boots,
            [ArmorType.Leggings] = legs,
            [ArmorType.Chestplate] = chest,
            [ArmorType.Helmet] = helm,
            [ArmorType.Body] = body,
        };
}
