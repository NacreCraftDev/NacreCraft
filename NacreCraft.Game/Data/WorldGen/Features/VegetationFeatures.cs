using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Vegetation;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Game.World.Level.Material;
using NacreCraft.Game.World.Loot;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.State;
using NacreCraft.Util.Random;
using Direction = NacreCraft.Primitives.Direction;
using GameIntProvider = NacreCraft.Game.World.Level.LevelGen.IntProvider;
using RegBlock = NacreCraft.Registry.Block;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//VegetationFeatures built-in vegetation features and tree mixes, maps to vanilla net.minecraft.data.worldgen.features.VegetationFeatures
//Tree mixes reference the placed trees from TreePlacements and the tree features from TreeFeatures
public static class VegetationFeatures
{
    private static readonly IntegerProperty FlowerBedAmount = new("amount", 1, 4);
    private static readonly IntegerProperty LeafLitterAmount = new("amount", 1, 4);
    private static readonly IntegerProperty SweetBerryAge = new("age", 0, 3);

    public static readonly ResourceKey<RegistryConfiguredFeature> BambooNoPodzol =
        FeatureUtils.CreateKey("bamboo_no_podzol");
    public static readonly ResourceKey<RegistryConfiguredFeature> BambooSomePodzol =
        FeatureUtils.CreateKey("bamboo_some_podzol");
    public static readonly ResourceKey<RegistryConfiguredFeature> Vines = FeatureUtils.CreateKey("vines");
    public static readonly ResourceKey<RegistryConfiguredFeature> BrownMushroom =
        FeatureUtils.CreateKey("brown_mushroom");
    public static readonly ResourceKey<RegistryConfiguredFeature> RedMushroom =
        FeatureUtils.CreateKey("red_mushroom");
    public static readonly ResourceKey<RegistryConfiguredFeature> Sunflower = FeatureUtils.CreateKey("sunflower");
    public static readonly ResourceKey<RegistryConfiguredFeature> Pumpkin = FeatureUtils.CreateKey("pumpkin");
    public static readonly ResourceKey<RegistryConfiguredFeature> BerryBush = FeatureUtils.CreateKey("berry_bush");
    public static readonly ResourceKey<RegistryConfiguredFeature> TaigaGrass = FeatureUtils.CreateKey("taiga_grass");
    public static readonly ResourceKey<RegistryConfiguredFeature> Grass = FeatureUtils.CreateKey("grass");
    public static readonly ResourceKey<RegistryConfiguredFeature> GrassJungle =
        FeatureUtils.CreateKey("grass_jungle");
    public static readonly ResourceKey<RegistryConfiguredFeature> DeadBush = FeatureUtils.CreateKey("dead_bush");
    public static readonly ResourceKey<RegistryConfiguredFeature> DryGrass = FeatureUtils.CreateKey("dry_grass");
    public static readonly ResourceKey<RegistryConfiguredFeature> Melon = FeatureUtils.CreateKey("melon");
    public static readonly ResourceKey<RegistryConfiguredFeature> Waterlily = FeatureUtils.CreateKey("waterlily");
    public static readonly ResourceKey<RegistryConfiguredFeature> TallGrass = FeatureUtils.CreateKey("tall_grass");
    public static readonly ResourceKey<RegistryConfiguredFeature> LargeFern = FeatureUtils.CreateKey("large_fern");
    public static readonly ResourceKey<RegistryConfiguredFeature> Bush = FeatureUtils.CreateKey("bush");
    public static readonly ResourceKey<RegistryConfiguredFeature> LeafLitter =
        FeatureUtils.CreateKey("leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> FireflyBush =
        FeatureUtils.CreateKey("firefly_bush");
    public static readonly ResourceKey<RegistryConfiguredFeature> Cactus = FeatureUtils.CreateKey("cactus");
    public static readonly ResourceKey<RegistryConfiguredFeature> SugarCane =
        FeatureUtils.CreateKey("sugar_cane");
    public static readonly ResourceKey<RegistryConfiguredFeature> FlowerDefault =
        FeatureUtils.CreateKey("flower_default");
    public static readonly ResourceKey<RegistryConfiguredFeature> FlowerFlowerForest =
        FeatureUtils.CreateKey("flower_flower_forest");
    public static readonly ResourceKey<RegistryConfiguredFeature> FlowerSwamp =
        FeatureUtils.CreateKey("flower_swamp");
    public static readonly ResourceKey<RegistryConfiguredFeature> FlowerPlain =
        FeatureUtils.CreateKey("flower_plain");
    public static readonly ResourceKey<RegistryConfiguredFeature> FlowerMeadow =
        FeatureUtils.CreateKey("flower_meadow");
    public static readonly ResourceKey<RegistryConfiguredFeature> FlowerCherry =
        FeatureUtils.CreateKey("flower_cherry");
    public static readonly ResourceKey<RegistryConfiguredFeature> FlowerPaleGarden =
        FeatureUtils.CreateKey("flower_pale_garden");
    public static readonly ResourceKey<RegistryConfiguredFeature> Wildflower =
        FeatureUtils.CreateKey("wildflower");
    public static readonly ResourceKey<RegistryConfiguredFeature> ForestFlowers =
        FeatureUtils.CreateKey("forest_flowers");
    public static readonly ResourceKey<RegistryConfiguredFeature> PaleForestFlower =
        FeatureUtils.CreateKey("pale_forest_flower");
    public static readonly ResourceKey<RegistryConfiguredFeature> DarkForestVegetation =
        FeatureUtils.CreateKey("dark_forest_vegetation");
    public static readonly ResourceKey<RegistryConfiguredFeature> PaleGardenVegetation =
        FeatureUtils.CreateKey("pale_garden_vegetation");
    public static readonly ResourceKey<RegistryConfiguredFeature> PaleMossVegetation =
        FeatureUtils.CreateKey("pale_moss_vegetation");
    public static readonly ResourceKey<RegistryConfiguredFeature> PaleMossPatch =
        FeatureUtils.CreateKey("pale_moss_patch");
    public static readonly ResourceKey<RegistryConfiguredFeature> PaleMossPatchBonemeal =
        FeatureUtils.CreateKey("pale_moss_patch_bonemeal");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesFlowerForest =
        FeatureUtils.CreateKey("trees_flower_forest");
    public static readonly ResourceKey<RegistryConfiguredFeature> MeadowTrees =
        FeatureUtils.CreateKey("meadow_trees");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesTaiga =
        FeatureUtils.CreateKey("trees_taiga");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesBadlands =
        FeatureUtils.CreateKey("trees_badlands");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesGrove =
        FeatureUtils.CreateKey("trees_grove");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesSavanna =
        FeatureUtils.CreateKey("trees_savanna");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesSnowy =
        FeatureUtils.CreateKey("trees_snowy");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesBirch =
        FeatureUtils.CreateKey("trees_birch");
    public static readonly ResourceKey<RegistryConfiguredFeature> BirchTall =
        FeatureUtils.CreateKey("birch_tall");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesWindsweptHills =
        FeatureUtils.CreateKey("trees_windswept_hills");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesWater =
        FeatureUtils.CreateKey("trees_water");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesBirchAndOakLeafLitter =
        FeatureUtils.CreateKey("trees_birch_and_oak_leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesPlains =
        FeatureUtils.CreateKey("trees_plains");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesSparseJungle =
        FeatureUtils.CreateKey("trees_sparse_jungle");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesOldGrowthSpruceTaiga =
        FeatureUtils.CreateKey("trees_old_growth_spruce_taiga");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesOldGrowthPineTaiga =
        FeatureUtils.CreateKey("trees_old_growth_pine_taiga");
    public static readonly ResourceKey<RegistryConfiguredFeature> TreesJungle =
        FeatureUtils.CreateKey("trees_jungle");
    public static readonly ResourceKey<RegistryConfiguredFeature> BambooVegetation =
        FeatureUtils.CreateKey("bamboo_vegetation");
    public static readonly ResourceKey<RegistryConfiguredFeature> MushroomIslandVegetation =
        FeatureUtils.CreateKey("mushroom_island_vegetation");
    public static readonly ResourceKey<RegistryConfiguredFeature> MangroveVegetation =
        FeatureUtils.CreateKey("mangrove_vegetation");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        var placed = context.LookupOrThrow(Registries.PLACED_FEATURE);
        var paleOakChecked = placed.GetOrThrow(TreePlacements.PaleOakChecked);
        var paleOakCreakingChecked = placed.GetOrThrow(TreePlacements.PaleOakCreakingChecked);
        var fancyOakChecked = placed.GetOrThrow(TreePlacements.FancyOakChecked);
        var birchBees002 = placed.GetOrThrow(TreePlacements.BirchBees002);
        var fancyOakBees002 = placed.GetOrThrow(TreePlacements.FancyOakBees002);
        var fancyOakBees = placed.GetOrThrow(TreePlacements.FancyOakBees);
        var pineChecked = placed.GetOrThrow(TreePlacements.PineChecked);
        var spruceChecked = placed.GetOrThrow(TreePlacements.SpruceChecked);
        var pineOnSnow = placed.GetOrThrow(TreePlacements.PineOnSnow);
        var acaciaChecked = placed.GetOrThrow(TreePlacements.AcaciaChecked);
        var superBirchBees0002 = placed.GetOrThrow(TreePlacements.SuperBirchBees0002);
        var birchBees0002Placed = placed.GetOrThrow(TreePlacements.BirchBees0002Placed);
        var birchBees0002LeafLitter = placed.GetOrThrow(TreePlacements.BirchBees0002LeafLitter);
        var fancyOakBees0002LeafLitter = placed.GetOrThrow(TreePlacements.FancyOakBees0002LeafLitter);
        var jungleBush = placed.GetOrThrow(TreePlacements.JungleBush);
        var megaSpruceChecked = placed.GetOrThrow(TreePlacements.MegaSpruceChecked);
        var megaPineChecked = placed.GetOrThrow(TreePlacements.MegaPineChecked);
        var megaJungleTreeChecked = placed.GetOrThrow(TreePlacements.MegaJungleTreeChecked);
        var tallMangroveChecked = placed.GetOrThrow(TreePlacements.TallMangroveChecked);
        var oakChecked = placed.GetOrThrow(TreePlacements.OakChecked);
        var oakBees002 = placed.GetOrThrow(TreePlacements.OakBees002);
        var superBirchBees = placed.GetOrThrow(TreePlacements.SuperBirchBees);
        var spruceOnSnow = placed.GetOrThrow(TreePlacements.SpruceOnSnow);
        var oakBees0002LeafLitter = placed.GetOrThrow(TreePlacements.OakBees0002LeafLitter);
        var jungleTreeChecked = placed.GetOrThrow(TreePlacements.JungleTreeChecked);
        var mangroveChecked = placed.GetOrThrow(TreePlacements.MangroveChecked);
        var oakLeafLitter = placed.GetOrThrow(TreePlacements.OakLeafLitter);
        var darkOakLeafLitter = placed.GetOrThrow(TreePlacements.DarkOakLeafLitter);
        var birchLeafLitter = placed.GetOrThrow(TreePlacements.BirchLeafLitter);
        var fancyOakLeafLitter = placed.GetOrThrow(TreePlacements.FancyOakLeafLitter);
        var fallenOak = placed.GetOrThrow(TreePlacements.FallenOakTree);
        var fallenBirch = placed.GetOrThrow(TreePlacements.FallenBirchTree);
        var fallenSuperBirch = placed.GetOrThrow(TreePlacements.FallenSuperBirchTree);
        var fallenJungle = placed.GetOrThrow(TreePlacements.FallenJungleTree);
        var fallenSpruce = placed.GetOrThrow(TreePlacements.FallenSpruceTree);

        FeatureUtils.Register(context, BambooNoPodzol, BambooFeature.Instance, new ProbabilityFeatureConfiguration(0.0f));
        FeatureUtils.Register(context, BambooSomePodzol, BambooFeature.Instance, new ProbabilityFeatureConfiguration(0.2f));
        FeatureUtils.Register(context, Vines, VinesFeature.Instance);
        FeatureUtils.Register(context, BrownMushroom, SimpleBlockFeature.Instance,
            SimpleBlock("brown_mushroom"));
        FeatureUtils.Register(context, RedMushroom, SimpleBlockFeature.Instance, SimpleBlock("red_mushroom"));
        FeatureUtils.Register(context, Sunflower, SimpleBlockFeature.Instance, SimpleBlock("sunflower"));
        FeatureUtils.Register(context, Pumpkin, SimpleBlockFeature.Instance, SimpleBlock("pumpkin"));
        FeatureUtils.Register(context, BerryBush, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            BlockStateProvider.Simple(WorldGenDataSupport.State("sweet_berry_bush").TrySetValue(SweetBerryAge, 3)),
            false));
        FeatureUtils.Register(context, TaigaGrass, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(WorldGenDataSupport.State("short_grass"), 1)
                .Add(WorldGenDataSupport.State("fern"), 4).Build()), false));
        FeatureUtils.Register(context, Grass, SimpleBlockFeature.Instance, SimpleBlock("short_grass"));
        FeatureUtils.Register(context, LeafLitter, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(LeafLitterPatchBuilder(1, 3)), false));
        FeatureUtils.Register(context, GrassJungle, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(WorldGenDataSupport.State("short_grass"), 3)
                .Add(WorldGenDataSupport.State("fern"), 1).Build()), false));
        FeatureUtils.Register(context, DeadBush, SimpleBlockFeature.Instance, SimpleBlock("dead_bush"));
        FeatureUtils.Register(context, DryGrass, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(WorldGenDataSupport.State("short_dry_grass"), 1)
                .Add(WorldGenDataSupport.State("tall_dry_grass"), 1).Build()), false));
        FeatureUtils.Register(context, Melon, SimpleBlockFeature.Instance, SimpleBlock("melon"));
        FeatureUtils.Register(context, Waterlily, SimpleBlockFeature.Instance, SimpleBlock("lily_pad"));
        FeatureUtils.Register(context, TallGrass, SimpleBlockFeature.Instance, SimpleBlock("tall_grass"));
        FeatureUtils.Register(context, LargeFern, SimpleBlockFeature.Instance, SimpleBlock("large_fern"));
        FeatureUtils.Register(context, Bush, SimpleBlockFeature.Instance, SimpleBlock("bush"));
        FeatureUtils.Register(context, Cactus, BlockColumnFeature.Instance, new BlockColumnConfiguration(
            new BlockColumnConfiguration.Layer[]
            {
                new(BiasedToBottomInt.Of(1, 3), BlockStateProvider.Simple(WorldGenDataSupport.Block("cactus"))),
                new(new WeightedListInt(WeightedList<GameIntProvider>.Builder()
                        .Add(ConstantInt.Of(0), 3).Add(ConstantInt.Of(1), 1).Build()),
                    BlockStateProvider.Simple(WorldGenDataSupport.Block("cactus_flower")))
            }, Direction.Up, BlockPredicate.OnlyInAir, false));
        FeatureUtils.Register(context, SugarCane, BlockColumnFeature.Instance, new BlockColumnConfiguration(
            new BlockColumnConfiguration.Layer[]
            {
                new(BiasedToBottomInt.Of(2, 4), BlockStateProvider.Simple(WorldGenDataSupport.Block("sugar_cane")))
            }, Direction.Up, BlockPredicate.OnlyInAir, false));
        FeatureUtils.Register(context, FireflyBush, SimpleBlockFeature.Instance, SimpleBlock("firefly_bush"));
        FeatureUtils.Register(context, FlowerDefault, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(WorldGenDataSupport.State("poppy"), 2)
                .Add(WorldGenDataSupport.State("dandelion"), 1).Build()), false));
        var noiseless = new NacreCraft.Game.World.Level.LevelGen.Synth.NoiseParameters(0, 1.0);
        FeatureUtils.Register(context, FlowerFlowerForest, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new NoiseProvider(2345L, noiseless, 0.020833334f, States(
                "dandelion", "poppy", "allium", "azure_bluet", "red_tulip", "orange_tulip", "white_tulip",
                "pink_tulip", "oxeye_daisy", "cornflower", "lily_of_the_valley")), false));
        FeatureUtils.Register(context, FlowerSwamp, SimpleBlockFeature.Instance, SimpleBlock("blue_orchid"));
        FeatureUtils.Register(context, FlowerPlain, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new NoiseThresholdProvider(2345L, noiseless, 0.005f, -0.8f, 0.33333334f,
                WorldGenDataSupport.State("dandelion"),
                States("orange_tulip", "red_tulip", "pink_tulip", "white_tulip"),
                States("poppy", "azure_bluet", "oxeye_daisy", "cornflower")), false));
        FeatureUtils.Register(context, FlowerMeadow, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new DualNoiseProvider(new NacreCraft.Game.World.Level.LevelGen.Features.Impl.IntRange(1, 3),
                new NacreCraft.Game.World.Level.LevelGen.Synth.NoiseParameters(-10, 1.0), 1.0f, 2345L,
                new NacreCraft.Game.World.Level.LevelGen.Synth.NoiseParameters(-3, 1.0), 1.0f,
                States("tall_grass", "allium", "poppy", "azure_bluet", "dandelion", "cornflower", "oxeye_daisy",
                    "short_grass")), false));
        FeatureUtils.Register(context, FlowerCherry, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(FlowerBedPatchBuilder("pink_petals")), false));
        FeatureUtils.Register(context, Wildflower, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(FlowerBedPatchBuilder("wildflowers")), false));
        FeatureUtils.Register(context, FlowerPaleGarden, SimpleBlockFeature.Instance,
            SimpleBlock("closed_eyeblossom", scheduleTick: true));
        FeatureUtils.Register(context, ForestFlowers, SimpleRandomSelectorFeature.Instance,
            new CompositeFeatureConfiguration(new DirectHolderSet<RegistryPlacedFeature>(new[]
            {
                TallFlower("lilac"), TallFlower("rose_bush"), TallFlower("peony"), TallFlower("lily_of_the_valley")
            })));
        FeatureUtils.Register(context, PaleForestFlower, SimpleBlockFeature.Instance,
            SimpleBlock("closed_eyeblossom", scheduleTick: true));
        FeatureUtils.Register(context, DarkForestVegetation, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(oakLeafLitter, new[]
            {
                new WeightedPlacedFeature(PlacementUtils.InlinePlaced(HugeMushroom("huge_brown_mushroom")), 0.025f),
                new WeightedPlacedFeature(PlacementUtils.InlinePlaced(HugeMushroom("huge_red_mushroom")), 0.05f),
                new WeightedPlacedFeature(darkOakLeafLitter, 0.6666667f),
                new WeightedPlacedFeature(fallenBirch, 0.0025f),
                new WeightedPlacedFeature(birchLeafLitter, 0.2f),
                new WeightedPlacedFeature(fallenOak, 0.0125f),
                new WeightedPlacedFeature(fancyOakLeafLitter, 0.1f)
            }));
        FeatureUtils.Register(context, PaleGardenVegetation, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(paleOakChecked, new[]
            {
                new WeightedPlacedFeature(paleOakCreakingChecked, 0.1f),
                new WeightedPlacedFeature(paleOakChecked, 0.9f)
            }));
        FeatureUtils.Register(context, PaleMossVegetation, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(WorldGenDataSupport.State("pale_moss_carpet"), 25)
                .Add(WorldGenDataSupport.State("short_grass"), 25)
                .Add(WorldGenDataSupport.State("tall_grass"), 10).Build()), false));
        //The pale moss patch feature is built inline so the moss replaceable tag and the moss vegetation feature stay in one place
        var mossReplaceable = BuiltInRegistries.BLOCK.GetOrCreate(BlockTags.MossReplaceable);
        var paleMossVegetation = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        FeatureUtils.Register(context, PaleMossPatch, VegetationPatchFeature.Instance,
            PaleMossPatchConfig(mossReplaceable, paleMossVegetation, 0.3f, 2, 4));
        FeatureUtils.Register(context, PaleMossPatchBonemeal, VegetationPatchFeature.Instance,
            PaleMossPatchConfig(mossReplaceable, paleMossVegetation, 0.6f, 1, 2));
        FeatureUtils.Register(context, TreesFlowerForest, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(oakBees002, new[]
            {
                new WeightedPlacedFeature(fallenBirch, 0.0025f),
                new WeightedPlacedFeature(birchBees002, 0.2f),
                new WeightedPlacedFeature(fancyOakBees002, 0.1f)
            }));
        FeatureUtils.Register(context, MeadowTrees, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(superBirchBees, new[]
            {
                new WeightedPlacedFeature(fancyOakBees, 0.5f)
            }));
        FeatureUtils.Register(context, TreesTaiga, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(spruceChecked, new[]
            {
                new WeightedPlacedFeature(pineChecked, 0.33333334f),
                new WeightedPlacedFeature(fallenSpruce, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesBadlands, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(oakLeafLitter, new[]
            {
                new WeightedPlacedFeature(fallenOak, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesGrove, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(spruceOnSnow, new[]
            {
                new WeightedPlacedFeature(pineOnSnow, 0.33333334f)
            }));
        FeatureUtils.Register(context, TreesSavanna, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(oakChecked, new[]
            {
                new WeightedPlacedFeature(acaciaChecked, 0.8f),
                new WeightedPlacedFeature(fallenOak, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesSnowy, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(spruceChecked, new[]
            {
                new WeightedPlacedFeature(fallenSpruce, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesBirch, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(birchBees0002Placed, new[]
            {
                new WeightedPlacedFeature(fallenBirch, 0.0125f)
            }));
        FeatureUtils.Register(context, BirchTall, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(birchBees0002Placed, new[]
            {
                new WeightedPlacedFeature(fallenSuperBirch, 0.00625f),
                new WeightedPlacedFeature(superBirchBees0002, 0.5f),
                new WeightedPlacedFeature(fallenBirch, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesWindsweptHills, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(oakChecked, new[]
            {
                new WeightedPlacedFeature(fallenSpruce, 0.008325f),
                new WeightedPlacedFeature(spruceChecked, 0.666f),
                new WeightedPlacedFeature(fancyOakChecked, 0.1f),
                new WeightedPlacedFeature(fallenOak, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesWater, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(oakChecked, new[]
            {
                new WeightedPlacedFeature(fancyOakChecked, 0.1f)
            }));
        FeatureUtils.Register(context, TreesBirchAndOakLeafLitter, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(oakBees0002LeafLitter, new[]
            {
                new WeightedPlacedFeature(fallenBirch, 0.0025f),
                new WeightedPlacedFeature(birchBees0002LeafLitter, 0.2f),
                new WeightedPlacedFeature(fancyOakBees0002LeafLitter, 0.1f),
                new WeightedPlacedFeature(fallenOak, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesPlains, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(PlacementUtils.InlinePlaced(features(context, TreeFeatures.OakBees005)),
                new[]
                {
                    new WeightedPlacedFeature(PlacementUtils.InlinePlaced(features(context, TreeFeatures.FancyOakBees005)), 0.33333334f),
                    new WeightedPlacedFeature(fallenOak, 0.0125f)
                }));
        FeatureUtils.Register(context, TreesSparseJungle, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(jungleTreeChecked, new[]
            {
                new WeightedPlacedFeature(fancyOakChecked, 0.1f),
                new WeightedPlacedFeature(jungleBush, 0.5f),
                new WeightedPlacedFeature(fallenJungle, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesOldGrowthSpruceTaiga, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(spruceChecked, new[]
            {
                new WeightedPlacedFeature(megaSpruceChecked, 0.33333334f),
                new WeightedPlacedFeature(pineChecked, 0.33333334f),
                new WeightedPlacedFeature(fallenSpruce, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesOldGrowthPineTaiga, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(spruceChecked, new[]
            {
                new WeightedPlacedFeature(megaSpruceChecked, 0.025641026f),
                new WeightedPlacedFeature(megaPineChecked, 0.30769232f),
                new WeightedPlacedFeature(pineChecked, 0.33333334f),
                new WeightedPlacedFeature(fallenSpruce, 0.0125f)
            }));
        FeatureUtils.Register(context, TreesJungle, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(jungleTreeChecked, new[]
            {
                new WeightedPlacedFeature(fancyOakChecked, 0.1f),
                new WeightedPlacedFeature(jungleBush, 0.5f),
                new WeightedPlacedFeature(megaJungleTreeChecked, 0.33333334f),
                new WeightedPlacedFeature(fallenJungle, 0.0125f)
            }));
        FeatureUtils.Register(context, BambooVegetation, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(PlacementUtils.InlinePlaced(features(context, GrassJungle),
                    CountPlacement.Of(32), RandomOffsetPlacement.OfTriangle(7, 3),
                    BlockPredicateFilter.ForPredicate(BlockPredicate.AllOf(BlockPredicate.OnlyInAir,
                        BlockPredicate.Not(BlockPredicate.MatchesBlocks(new Vec3i(0, -1, 0),
                            WorldGenDataSupport.Block("podzol")))))),
                new[]
                {
                    new WeightedPlacedFeature(fancyOakChecked, 0.05f),
                    new WeightedPlacedFeature(jungleBush, 0.15f),
                    new WeightedPlacedFeature(megaJungleTreeChecked, 0.7f)
                }));
        FeatureUtils.Register(context, MushroomIslandVegetation, RandomBooleanSelectorFeature.Instance,
            new RandomBooleanFeatureConfiguration(
                PlacementUtils.InlinePlaced(features(context, TreeFeatures.HugeRedMushroom)),
                PlacementUtils.InlinePlaced(features(context, TreeFeatures.HugeBrownMushroom))));
        FeatureUtils.Register(context, MangroveVegetation, RandomSelectorFeature.Instance,
            new RandomFeatureConfiguration(mangroveChecked, new[]
            {
                new WeightedPlacedFeature(tallMangroveChecked, 0.85f)
            }));
    }

    //SimpleBlock the single-state simple block configuration used by most entries
    private static SimpleBlockConfiguration SimpleBlock(string block, bool scheduleTick = false)
        => new(BlockStateProvider.Simple(WorldGenDataSupport.Block(block)), scheduleTick);

    //States resolve several block states by path
    private static IReadOnlyList<BlockState> States(params string[] paths)
        => paths.Select(WorldGenDataSupport.State).ToList();

    //TallFlower an inline placed tall flower with the vanilla count/spread/survival filters
    private static Holder<RegistryPlacedFeature> TallFlower(string block)
        => PlacementUtils.InlinePlaced(SimpleBlockFeature.Instance, SimpleBlock(block), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3), BlockPredicateFilter.ForPredicate(BlockPredicate.OnlyInAir));

    //HugeMushroom an inline placed huge mushroom from its configured feature key
    private static Holder<RegistryConfiguredFeature> HugeMushroom(string path)
        => Holder<RegistryConfiguredFeature>.Direct(new NacreCraft.Game.World.Level.LevelGen.Features.ConfiguredFeature(
            path == "huge_red_mushroom" ? HugeRedMushroomFeature.Instance : HugeBrownMushroomFeature.Instance,
            NoneFeatureConfiguration.Instance));

    //features lookup of a configured feature registered earlier in the same bootstrap
    private static Holder<RegistryConfiguredFeature> features(BootstrapContext<RegistryConfiguredFeature> context,
        ResourceKey<RegistryConfiguredFeature> key)
        => context.LookupOrThrow(Registries.CONFIGURED_FEATURE).GetOrThrow(key);

    //PaleMossPatchConfig the pale moss patch configuration shared by the normal and bonemeal variants
    private static VegetationPatchConfiguration PaleMossPatchConfig(HolderSet<RegBlock> replaceable,
        NacreCraft.Registry.Registry<RegistryConfiguredFeature> features, float vegetationChance, int minRadius,
        int maxRadius)
        => new(replaceable, BlockStateProvider.Simple(WorldGenDataSupport.Block("pale_moss_block")),
            PlacementUtils.InlinePlaced(features.GetOrThrow(PaleMossVegetation)), CaveSurface.Floor,
            ConstantInt.Of(1), 0.0f, 5, vegetationChance, UniformInt.Of(minRadius, maxRadius), 0.75f);

    //FlowerBedPatchBuilder weighted flower bed states across amounts and the four facings, maps to flowerBedPatchBuilder
    private static WeightedList<BlockState> FlowerBedPatchBuilder(string block)
        => SegmentedPatchBuilder(block, 1, 4, FlowerBedAmount);

    //LeafLitterPatchBuilder weighted leaf litter states, maps to leafLitterPatchBuilder
    public static WeightedList<BlockState> LeafLitterPatchBuilder(int minState, int maxState)
        => SegmentedPatchBuilder("leaf_litter", minState, maxState, LeafLitterAmount);

    //NearWaterPredicate air above a spot that survives and has water beside its base, maps to vanilla nearWaterPredicate
    public static BlockPredicateFilter NearWaterPredicate(RegBlock block)
        => BlockPredicateFilter.ForPredicate(BlockPredicate.AllOf(BlockPredicate.OnlyInAir,
            BlockPredicate.WouldSurvive(block.DefaultBlockState, Vec3i.Zero),
            BlockPredicate.AnyOf(
                WaterBelow(1, -1, 0), WaterBelow(-1, -1, 0), WaterBelow(0, -1, 1), WaterBelow(0, -1, -1))));

    //WaterBelow water check at one offset, maps to the matchesFluids form in vanilla nearWaterPredicate
    private static BlockPredicate WaterBelow(int x, int y, int z)
        => BlockPredicate.MatchesFluids(new Vec3i(x, y, z), Fluids.Water, Fluids.FlowingWater);

    private static WeightedList<BlockState> SegmentedPatchBuilder(string block, int minState, int maxState,
        IntegerProperty amountProperty)
    {
        var builder = WeightedList<BlockState>.Builder();
        var state = WorldGenDataSupport.State(block);
        for (var amount = minState; amount <= maxState; amount++)
            foreach (var facing in BlockStateProperties.HorizontalFacing.PossibleValues)
                builder.Add(state.TrySetValue(amountProperty, amount)
                    .TrySetValue(BlockStateProperties.HorizontalFacing, facing), 1);
        return builder.Build();
    }
}
