using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using Direction = NacreCraft.Primitives.Direction;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//TreePlacements built-in placed trees, maps to vanilla net.minecraft.data.worldgen.placement.TreePlacements
//Each tree is either filtered by sapling survival or wrapped in the snow/propagule predicates
public static class TreePlacements
{
    public static readonly ResourceKey<RegistryPlacedFeature> CrimsonFungi = PlacementUtils.CreateKey("crimson_fungi");
    public static readonly ResourceKey<RegistryPlacedFeature> WarpedFungi = PlacementUtils.CreateKey("warped_fungi");
    public static readonly ResourceKey<RegistryPlacedFeature> OakChecked = PlacementUtils.CreateKey("oak_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> DarkOakChecked = PlacementUtils.CreateKey("dark_oak_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> PaleOakChecked =
        PlacementUtils.CreateKey("pale_oak_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> PaleOakCreakingChecked =
        PlacementUtils.CreateKey("pale_oak_creaking_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> BirchChecked = PlacementUtils.CreateKey("birch_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> AcaciaChecked = PlacementUtils.CreateKey("acacia_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> SpruceChecked = PlacementUtils.CreateKey("spruce_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> MangroveChecked =
        PlacementUtils.CreateKey("mangrove_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> CherryChecked = PlacementUtils.CreateKey("cherry_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> PineOnSnow = PlacementUtils.CreateKey("pine_on_snow");
    public static readonly ResourceKey<RegistryPlacedFeature> SpruceOnSnow = PlacementUtils.CreateKey("spruce_on_snow");
    public static readonly ResourceKey<RegistryPlacedFeature> PineChecked = PlacementUtils.CreateKey("pine_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> JungleTreeChecked = PlacementUtils.CreateKey("jungle_tree");
    public static readonly ResourceKey<RegistryPlacedFeature> FancyOakChecked =
        PlacementUtils.CreateKey("fancy_oak_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> MegaJungleTreeChecked =
        PlacementUtils.CreateKey("mega_jungle_tree_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> MegaSpruceChecked =
        PlacementUtils.CreateKey("mega_spruce_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> MegaPineChecked =
        PlacementUtils.CreateKey("mega_pine_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> TallMangroveChecked =
        PlacementUtils.CreateKey("tall_mangrove_checked");
    public static readonly ResourceKey<RegistryPlacedFeature> JungleBush = PlacementUtils.CreateKey("jungle_bush");
    public static readonly ResourceKey<RegistryPlacedFeature> SuperBirchBees0002 =
        PlacementUtils.CreateKey("super_birch_bees_0002");
    public static readonly ResourceKey<RegistryPlacedFeature> SuperBirchBees =
        PlacementUtils.CreateKey("super_birch_bees");
    public static readonly ResourceKey<RegistryPlacedFeature> OakBees0002LeafLitter =
        PlacementUtils.CreateKey("oak_bees_0002_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> OakBees002 = PlacementUtils.CreateKey("oak_bees_002");
    public static readonly ResourceKey<RegistryPlacedFeature> BirchBees0002Placed =
        PlacementUtils.CreateKey("birch_bees_0002");
    public static readonly ResourceKey<RegistryPlacedFeature> BirchBees0002LeafLitter =
        PlacementUtils.CreateKey("birch_bees_0002_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> BirchBees002 = PlacementUtils.CreateKey("birch_bees_002");
    public static readonly ResourceKey<RegistryPlacedFeature> FancyOakBees0002LeafLitter =
        PlacementUtils.CreateKey("fancy_oak_bees_0002_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> FancyOakBees002 =
        PlacementUtils.CreateKey("fancy_oak_bees_002");
    public static readonly ResourceKey<RegistryPlacedFeature> FancyOakBees = PlacementUtils.CreateKey("fancy_oak_bees");
    public static readonly ResourceKey<RegistryPlacedFeature> CherryBees005 =
        PlacementUtils.CreateKey("cherry_bees_005");
    public static readonly ResourceKey<RegistryPlacedFeature> OakLeafLitter =
        PlacementUtils.CreateKey("oak_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> DarkOakLeafLitter =
        PlacementUtils.CreateKey("dark_oak_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> BirchLeafLitter =
        PlacementUtils.CreateKey("birch_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> FancyOakLeafLitter =
        PlacementUtils.CreateKey("fancy_oak_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> FallenOakTree =
        PlacementUtils.CreateKey("fallen_oak_tree");
    public static readonly ResourceKey<RegistryPlacedFeature> FallenBirchTree =
        PlacementUtils.CreateKey("fallen_birch_tree");
    public static readonly ResourceKey<RegistryPlacedFeature> FallenSuperBirchTree =
        PlacementUtils.CreateKey("fallen_super_birch_tree");
    public static readonly ResourceKey<RegistryPlacedFeature> FallenSpruceTree =
        PlacementUtils.CreateKey("fallen_spruce_tree");
    public static readonly ResourceKey<RegistryPlacedFeature> FallenJungleTree =
        PlacementUtils.CreateKey("fallen_jungle_tree");

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var crimsonFungus = features.GetOrThrow(TreeFeatures.CrimsonFungus);
        var warpedFungus = features.GetOrThrow(TreeFeatures.WarpedFungus);
        var oak = features.GetOrThrow(TreeFeatures.Oak);
        var darkOak = features.GetOrThrow(TreeFeatures.DarkOak);
        var paleOak = features.GetOrThrow(TreeFeatures.PaleOak);
        var paleOakCreaking = features.GetOrThrow(TreeFeatures.PaleOakCreaking);
        var birch = features.GetOrThrow(TreeFeatures.Birch);
        var acacia = features.GetOrThrow(TreeFeatures.Acacia);
        var spruce = features.GetOrThrow(TreeFeatures.Spruce);
        var mangrove = features.GetOrThrow(TreeFeatures.Mangrove);
        var cherry = features.GetOrThrow(TreeFeatures.Cherry);
        var pine = features.GetOrThrow(TreeFeatures.Pine);
        var jungleTree = features.GetOrThrow(TreeFeatures.JungleTree);
        var fancyOak = features.GetOrThrow(TreeFeatures.FancyOak);
        var megaJungleTree = features.GetOrThrow(TreeFeatures.MegaJungleTree);
        var megaSpruce = features.GetOrThrow(TreeFeatures.MegaSpruce);
        var megaPine = features.GetOrThrow(TreeFeatures.MegaPine);
        var tallMangrove = features.GetOrThrow(TreeFeatures.TallMangrove);
        var jungleBush = features.GetOrThrow(TreeFeatures.JungleBush);
        var superBirchBees0002 = features.GetOrThrow(TreeFeatures.SuperBirchBees0002);
        var superBirchBees = features.GetOrThrow(TreeFeatures.SuperBirchBees);
        var oakBees0002LeafLitter = features.GetOrThrow(TreeFeatures.OakBees0002LeafLitter);
        var oakBees002 = features.GetOrThrow(TreeFeatures.OakBees002);
        var birchBees0002 = features.GetOrThrow(TreeFeatures.BirchBees0002);
        var birchBees0002LeafLitter = features.GetOrThrow(TreeFeatures.BirchBees0002LeafLitter);
        var birchBees002 = features.GetOrThrow(TreeFeatures.BirchBees002);
        var fancyOakBees0002LeafLitter = features.GetOrThrow(TreeFeatures.FancyOakBees0002LeafLitter);
        var fancyOakBees002 = features.GetOrThrow(TreeFeatures.FancyOakBees002);
        var fancyOakBees = features.GetOrThrow(TreeFeatures.FancyOakBees);
        var cherryBees005 = features.GetOrThrow(TreeFeatures.CherryBees005);
        var oakLeafLitter = features.GetOrThrow(TreeFeatures.OakLeafLitter);
        var darkOakLeafLitter = features.GetOrThrow(TreeFeatures.DarkOakLeafLitter);
        var birchLeafLitter = features.GetOrThrow(TreeFeatures.BirchLeafLitter);
        var fancyOakLeafLitter = features.GetOrThrow(TreeFeatures.FancyOakLeafLitter);
        var fallenOakTree = features.GetOrThrow(TreeFeatures.FallenOakTree);
        var fallenBirchTree = features.GetOrThrow(TreeFeatures.FallenBirchTree);
        var fallenSuperBirchTree = features.GetOrThrow(TreeFeatures.FallenSuperBirchTree);
        var fallenSpruceTree = features.GetOrThrow(TreeFeatures.FallenSpruceTree);
        var fallenJungleTree = features.GetOrThrow(TreeFeatures.FallenJungleTree);
        PlacementUtils.Register(context, CrimsonFungi, crimsonFungus, CountOnEveryLayerPlacement.Of(8), BiomeFilter.Biome());
        PlacementUtils.Register(context, WarpedFungi, warpedFungus, CountOnEveryLayerPlacement.Of(8), BiomeFilter.Biome());
        PlacementUtils.Register(context, OakChecked, oak, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, DarkOakChecked, darkOak, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("dark_oak_sapling")));
        PlacementUtils.Register(context, PaleOakChecked, paleOak, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("pale_oak_sapling")));
        PlacementUtils.Register(context, PaleOakCreakingChecked, paleOakCreaking, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("pale_oak_sapling")));
        PlacementUtils.Register(context, BirchChecked, birch, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, AcaciaChecked, acacia, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("acacia_sapling")));
        PlacementUtils.Register(context, SpruceChecked, spruce, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("spruce_sapling")));
        PlacementUtils.Register(context, MangroveChecked, mangrove, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("mangrove_propagule")));
        PlacementUtils.Register(context, CherryChecked, cherry, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("cherry_sapling")));
        var snowTreePredicate = BlockPredicate.MatchesBlocks(new Vec3i(0, -1, 0),
            WorldGenDataSupport.Block("snow_block"), WorldGenDataSupport.Block("powder_snow"));
        var snowTreeFilterDecorator = new PlacementModifier[]
        {
            EnvironmentScanPlacement.ScanningFor(Direction.Up,
                BlockPredicate.Not(BlockPredicate.MatchesBlocks(WorldGenDataSupport.Block("powder_snow"))), 8),
            BlockPredicateFilter.ForPredicate(snowTreePredicate)
        };
        PlacementUtils.Register(context, PineOnSnow, pine, snowTreeFilterDecorator);
        PlacementUtils.Register(context, SpruceOnSnow, spruce, snowTreeFilterDecorator);
        PlacementUtils.Register(context, PineChecked, pine, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("spruce_sapling")));
        PlacementUtils.Register(context, JungleTreeChecked, jungleTree, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("jungle_sapling")));
        PlacementUtils.Register(context, FancyOakChecked, fancyOak, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, MegaJungleTreeChecked, megaJungleTree, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("jungle_sapling")));
        PlacementUtils.Register(context, MegaSpruceChecked, megaSpruce, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("spruce_sapling")));
        PlacementUtils.Register(context, MegaPineChecked, megaPine, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("spruce_sapling")));
        PlacementUtils.Register(context, TallMangroveChecked, tallMangrove, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("mangrove_propagule")));
        PlacementUtils.Register(context, JungleBush, jungleBush, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, SuperBirchBees0002, superBirchBees0002, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, SuperBirchBees, superBirchBees, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, OakBees0002LeafLitter, oakBees0002LeafLitter, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, OakBees002, oakBees002, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, BirchBees0002Placed, birchBees0002, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, BirchBees0002LeafLitter, birchBees0002LeafLitter, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, BirchBees002, birchBees002, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, FancyOakBees0002LeafLitter, fancyOakBees0002LeafLitter, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, FancyOakBees002, fancyOakBees002, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, FancyOakBees, fancyOakBees, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, CherryBees005, cherryBees005, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("cherry_sapling")));
        PlacementUtils.Register(context, OakLeafLitter, oakLeafLitter, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, DarkOakLeafLitter, darkOakLeafLitter, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("dark_oak_sapling")));
        PlacementUtils.Register(context, BirchLeafLitter, birchLeafLitter, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, FancyOakLeafLitter, fancyOakLeafLitter, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, FallenOakTree, fallenOakTree, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, FallenBirchTree, fallenBirchTree, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, FallenSuperBirchTree, fallenSuperBirchTree, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("birch_sapling")));
        PlacementUtils.Register(context, FallenSpruceTree, fallenSpruceTree, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("spruce_sapling")));
        PlacementUtils.Register(context, FallenJungleTree, fallenJungleTree, PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("jungle_sapling")));
    }
}
