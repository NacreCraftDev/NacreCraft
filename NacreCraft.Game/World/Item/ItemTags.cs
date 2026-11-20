using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items;

//ItemTags item tag keys, maps to the repair tags of vanilla net.minecraft.tags.ItemTags
//Only the tags the armor materials reference are declared here; more are added as their consumers land
public static class ItemTags
{
    //Create builds a default-namespace item tag key
    private static TagKey<Item> Create(string path)
        => TagKey<Item>.Create(Registries.ITEM, Identifier.WithDefaultNamespace(path));

    public static readonly TagKey<Item> RepairsLeatherArmor = Create("repairs_leather_armor");
    public static readonly TagKey<Item> RepairsCopperArmor = Create("repairs_copper_armor");
    public static readonly TagKey<Item> RepairsChainArmor = Create("repairs_chain_armor");
    public static readonly TagKey<Item> RepairsIronArmor = Create("repairs_iron_armor");
    public static readonly TagKey<Item> RepairsGoldArmor = Create("repairs_gold_armor");
    public static readonly TagKey<Item> RepairsDiamondArmor = Create("repairs_diamond_armor");
    public static readonly TagKey<Item> RepairsTurtleHelmet = Create("repairs_turtle_helmet");
    public static readonly TagKey<Item> RepairsNetheriteArmor = Create("repairs_netherite_armor");
    public static readonly TagKey<Item> RepairsWolfArmor = Create("repairs_wolf_armor");
}
