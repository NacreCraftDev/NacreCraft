using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//OreFeatures built-in ore veins, maps to vanilla net.minecraft.data.worldgen.features.OreFeatures
//Blocks resolve by registry name through WorldGenDataSupport, so the table registers while ores are still being filled in
public static class OreFeatures
{
    public static readonly ResourceKey<RegistryConfiguredFeature> OreMagma = FeatureUtils.CreateKey("ore_magma");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreSoulSand = FeatureUtils.CreateKey("ore_soul_sand");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreNetherGold =
        FeatureUtils.CreateKey("ore_nether_gold");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreQuartz = FeatureUtils.CreateKey("ore_quartz");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreGravelNether =
        FeatureUtils.CreateKey("ore_gravel_nether");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreBlackstone =
        FeatureUtils.CreateKey("ore_blackstone");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreDirt = FeatureUtils.CreateKey("ore_dirt");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreGravel = FeatureUtils.CreateKey("ore_gravel");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreGranite = FeatureUtils.CreateKey("ore_granite");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreDiorite = FeatureUtils.CreateKey("ore_diorite");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreAndesite = FeatureUtils.CreateKey("ore_andesite");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreTuff = FeatureUtils.CreateKey("ore_tuff");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreCoal = FeatureUtils.CreateKey("ore_coal");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreCoalBuried =
        FeatureUtils.CreateKey("ore_coal_buried");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreIron = FeatureUtils.CreateKey("ore_iron");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreIronSmall =
        FeatureUtils.CreateKey("ore_iron_small");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreGold = FeatureUtils.CreateKey("ore_gold");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreGoldBuried =
        FeatureUtils.CreateKey("ore_gold_buried");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreRedstone =
        FeatureUtils.CreateKey("ore_redstone");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreDiamondSmall =
        FeatureUtils.CreateKey("ore_diamond_small");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreDiamondMedium =
        FeatureUtils.CreateKey("ore_diamond_medium");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreDiamondLarge =
        FeatureUtils.CreateKey("ore_diamond_large");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreDiamondBuried =
        FeatureUtils.CreateKey("ore_diamond_buried");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreLapis = FeatureUtils.CreateKey("ore_lapis");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreLapisBuried =
        FeatureUtils.CreateKey("ore_lapis_buried");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreInfested =
        FeatureUtils.CreateKey("ore_infested");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreEmerald = FeatureUtils.CreateKey("ore_emerald");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreAncientDebrisLarge =
        FeatureUtils.CreateKey("ore_ancient_debris_large");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreAncientDebrisSmall =
        FeatureUtils.CreateKey("ore_ancient_debris_small");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreCopperSmall =
        FeatureUtils.CreateKey("ore_copper_small");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreCopperLarge =
        FeatureUtils.CreateKey("ore_copper_large");
    public static readonly ResourceKey<RegistryConfiguredFeature> OreClay = FeatureUtils.CreateKey("ore_clay");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        RuleTest naturalStone = new TagMatchTest(BlockTags.BaseStoneOverworld);
        RuleTest stoneOreReplaceables = new TagMatchTest(BlockTags.StoneOreReplaceables);
        RuleTest deepslateOreReplaceables = new TagMatchTest(BlockTags.DeepslateOreReplaceables);
        RuleTest netherrack = new BlockMatchTest(WorldGenDataSupport.Block("netherrack"));
        RuleTest netherOreReplaceables = new TagMatchTest(BlockTags.BaseStoneNether);
        var oreIronTargets = Targets(stoneOreReplaceables, "iron_ore", deepslateOreReplaceables, "deepslate_iron_ore");
        var oreGoldTargets = Targets(stoneOreReplaceables, "gold_ore", deepslateOreReplaceables, "deepslate_gold_ore");
        var oreDiamondTargets = Targets(stoneOreReplaceables, "diamond_ore", deepslateOreReplaceables,
            "deepslate_diamond_ore");
        var oreLapisTargets = Targets(stoneOreReplaceables, "lapis_ore", deepslateOreReplaceables, "deepslate_lapis_ore");
        var oreCopperTargets = Targets(stoneOreReplaceables, "copper_ore", deepslateOreReplaceables,
            "deepslate_copper_ore");
        var oreCoalTargets = Targets(stoneOreReplaceables, "coal_ore", deepslateOreReplaceables, "deepslate_coal_ore");
        FeatureUtils.Register(context, OreMagma, OreFeature.Instance,
            Single(netherrack, "magma_block", 33));
        FeatureUtils.Register(context, OreSoulSand, OreFeature.Instance,
            Single(netherrack, "soul_sand", 12));
        FeatureUtils.Register(context, OreNetherGold, OreFeature.Instance,
            Single(netherrack, "nether_gold_ore", 10));
        FeatureUtils.Register(context, OreQuartz, OreFeature.Instance,
            Single(netherrack, "nether_quartz_ore", 14));
        FeatureUtils.Register(context, OreGravelNether, OreFeature.Instance,
            Single(netherrack, "gravel", 33));
        FeatureUtils.Register(context, OreBlackstone, OreFeature.Instance,
            Single(netherrack, "blackstone", 33));
        FeatureUtils.Register(context, OreDirt, OreFeature.Instance, Single(naturalStone, "dirt", 33));
        FeatureUtils.Register(context, OreGravel, OreFeature.Instance, Single(naturalStone, "gravel", 33));
        FeatureUtils.Register(context, OreGranite, OreFeature.Instance, Single(naturalStone, "granite", 64));
        FeatureUtils.Register(context, OreDiorite, OreFeature.Instance, Single(naturalStone, "diorite", 64));
        FeatureUtils.Register(context, OreAndesite, OreFeature.Instance, Single(naturalStone, "andesite", 64));
        FeatureUtils.Register(context, OreTuff, OreFeature.Instance, Single(naturalStone, "tuff", 64));
        FeatureUtils.Register(context, OreCoal, OreFeature.Instance, new OreConfiguration(oreCoalTargets, 17, 0.0f));
        FeatureUtils.Register(context, OreCoalBuried, OreFeature.Instance,
            new OreConfiguration(oreCoalTargets, 17, 0.5f));
        FeatureUtils.Register(context, OreIron, OreFeature.Instance, new OreConfiguration(oreIronTargets, 9, 0.0f));
        FeatureUtils.Register(context, OreIronSmall, OreFeature.Instance, new OreConfiguration(oreIronTargets, 4, 0.0f));
        FeatureUtils.Register(context, OreGold, OreFeature.Instance, new OreConfiguration(oreGoldTargets, 9, 0.0f));
        FeatureUtils.Register(context, OreGoldBuried, OreFeature.Instance,
            new OreConfiguration(oreGoldTargets, 9, 0.5f));
        FeatureUtils.Register(context, OreRedstone, OreFeature.Instance,
            new OreConfiguration(Targets(stoneOreReplaceables, "redstone_ore", deepslateOreReplaceables,
                "deepslate_redstone_ore"), 8, 0.0f));
        FeatureUtils.Register(context, OreDiamondSmall, OreFeature.Instance,
            new OreConfiguration(oreDiamondTargets, 4, 0.5f));
        FeatureUtils.Register(context, OreDiamondLarge, OreFeature.Instance,
            new OreConfiguration(oreDiamondTargets, 12, 0.7f));
        FeatureUtils.Register(context, OreDiamondBuried, OreFeature.Instance,
            new OreConfiguration(oreDiamondTargets, 8, 1.0f));
        FeatureUtils.Register(context, OreDiamondMedium, OreFeature.Instance,
            new OreConfiguration(oreDiamondTargets, 8, 0.5f));
        FeatureUtils.Register(context, OreLapis, OreFeature.Instance, new OreConfiguration(oreLapisTargets, 7, 0.0f));
        FeatureUtils.Register(context, OreLapisBuried, OreFeature.Instance,
            new OreConfiguration(oreLapisTargets, 7, 1.0f));
        FeatureUtils.Register(context, OreInfested, OreFeature.Instance,
            new OreConfiguration(Targets(stoneOreReplaceables, "infested_stone", deepslateOreReplaceables,
                "infested_deepslate"), 9, 0.0f));
        FeatureUtils.Register(context, OreEmerald, OreFeature.Instance,
            new OreConfiguration(Targets(stoneOreReplaceables, "emerald_ore", deepslateOreReplaceables,
                "deepslate_emerald_ore"), 3, 0.0f));
        FeatureUtils.Register(context, OreAncientDebrisLarge, ScatteredOreFeature.Instance,
            Single(netherOreReplaceables, "ancient_debris", 3, 1.0f));
        FeatureUtils.Register(context, OreAncientDebrisSmall, ScatteredOreFeature.Instance,
            Single(netherOreReplaceables, "ancient_debris", 2, 1.0f));
        FeatureUtils.Register(context, OreCopperSmall, OreFeature.Instance,
            new OreConfiguration(oreCopperTargets, 10, 0.0f));
        FeatureUtils.Register(context, OreCopperLarge, OreFeature.Instance,
            new OreConfiguration(oreCopperTargets, 20, 0.0f));
        FeatureUtils.Register(context, OreClay, OreFeature.Instance, Single(naturalStone, "clay", 33));
    }

    //Single one rule test with one target state, maps to vanilla new OreConfiguration(ruleTest, state, size)
    private static OreConfiguration Single(RuleTest ruleTest, string block, int size, float discardChance = 0.0f)
        => new(new[] { new OreTargetState(ruleTest, WorldGenDataSupport.State(block)) }, size, discardChance);

    //Targets two rule tests each with one target state, maps to the stone/deepslate target list form
    private static IReadOnlyList<OreTargetState> Targets(RuleTest first, string firstBlock,
        RuleTest second, string secondBlock)
        => new[]
        {
            new OreTargetState(first, WorldGenDataSupport.State(firstBlock)),
            new OreTargetState(second, WorldGenDataSupport.State(secondBlock))
        };
}
