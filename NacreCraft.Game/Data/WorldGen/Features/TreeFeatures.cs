using NacreCraft.Codec;
using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Nether;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Trees;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Vegetation;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.State;
using NacreCraft.Util.Random;
using Direction = NacreCraft.Primitives.Direction;
using GameIntProvider = NacreCraft.Game.World.Level.LevelGen.IntProvider;
using RegBlock = NacreCraft.Registry.Block;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//TreeFeatures built-in tree, fungus and mushroom features, maps to vanilla net.minecraft.data.worldgen.features.TreeFeatures
//Blocks resolve by registry name; the leaf litter and mangrove decorations reuse the tree decorator system
public static class TreeFeatures
{
    private static readonly BooleanProperty MushroomUp = new("up");
    private static readonly BooleanProperty MushroomDown = new("down");
    private static readonly BooleanProperty PropaguleHanging = new("hanging");
    private static readonly IntegerProperty PropaguleAge = new("age", 0, 4);

    public static readonly ResourceKey<RegistryConfiguredFeature> CrimsonFungus =
        FeatureUtils.CreateKey("crimson_fungus");
    public static readonly ResourceKey<RegistryConfiguredFeature> CrimsonFungusPlanted =
        FeatureUtils.CreateKey("crimson_fungus_planted");
    public static readonly ResourceKey<RegistryConfiguredFeature> WarpedFungus =
        FeatureUtils.CreateKey("warped_fungus");
    public static readonly ResourceKey<RegistryConfiguredFeature> WarpedFungusPlanted =
        FeatureUtils.CreateKey("warped_fungus_planted");
    public static readonly ResourceKey<RegistryConfiguredFeature> HugeBrownMushroom =
        FeatureUtils.CreateKey("huge_brown_mushroom");
    public static readonly ResourceKey<RegistryConfiguredFeature> HugeRedMushroom =
        FeatureUtils.CreateKey("huge_red_mushroom");
    public static readonly ResourceKey<RegistryConfiguredFeature> Oak = FeatureUtils.CreateKey("oak");
    public static readonly ResourceKey<RegistryConfiguredFeature> DarkOak = FeatureUtils.CreateKey("dark_oak");
    public static readonly ResourceKey<RegistryConfiguredFeature> PaleOak = FeatureUtils.CreateKey("pale_oak");
    public static readonly ResourceKey<RegistryConfiguredFeature> PaleOakBonemeal =
        FeatureUtils.CreateKey("pale_oak_bonemeal");
    public static readonly ResourceKey<RegistryConfiguredFeature> PaleOakCreaking =
        FeatureUtils.CreateKey("pale_oak_creaking");
    public static readonly ResourceKey<RegistryConfiguredFeature> Birch = FeatureUtils.CreateKey("birch");
    public static readonly ResourceKey<RegistryConfiguredFeature> Acacia = FeatureUtils.CreateKey("acacia");
    public static readonly ResourceKey<RegistryConfiguredFeature> Spruce = FeatureUtils.CreateKey("spruce");
    public static readonly ResourceKey<RegistryConfiguredFeature> Pine = FeatureUtils.CreateKey("pine");
    public static readonly ResourceKey<RegistryConfiguredFeature> JungleTree =
        FeatureUtils.CreateKey("jungle_tree");
    public static readonly ResourceKey<RegistryConfiguredFeature> FancyOak = FeatureUtils.CreateKey("fancy_oak");
    public static readonly ResourceKey<RegistryConfiguredFeature> JungleTreeNoVine =
        FeatureUtils.CreateKey("jungle_tree_no_vine");
    public static readonly ResourceKey<RegistryConfiguredFeature> MegaJungleTree =
        FeatureUtils.CreateKey("mega_jungle_tree");
    public static readonly ResourceKey<RegistryConfiguredFeature> MegaSpruce =
        FeatureUtils.CreateKey("mega_spruce");
    public static readonly ResourceKey<RegistryConfiguredFeature> MegaPine = FeatureUtils.CreateKey("mega_pine");
    public static readonly ResourceKey<RegistryConfiguredFeature> SuperBirchBees0002 =
        FeatureUtils.CreateKey("super_birch_bees_0002");
    public static readonly ResourceKey<RegistryConfiguredFeature> SuperBirchBees =
        FeatureUtils.CreateKey("super_birch_bees");
    public static readonly ResourceKey<RegistryConfiguredFeature> SwampOak = FeatureUtils.CreateKey("swamp_oak");
    public static readonly ResourceKey<RegistryConfiguredFeature> JungleBush =
        FeatureUtils.CreateKey("jungle_bush");
    public static readonly ResourceKey<RegistryConfiguredFeature> AzaleaTree =
        FeatureUtils.CreateKey("azalea_tree");
    public static readonly ResourceKey<RegistryConfiguredFeature> Mangrove = FeatureUtils.CreateKey("mangrove");
    public static readonly ResourceKey<RegistryConfiguredFeature> TallMangrove =
        FeatureUtils.CreateKey("tall_mangrove");
    public static readonly ResourceKey<RegistryConfiguredFeature> Cherry = FeatureUtils.CreateKey("cherry");
    public static readonly ResourceKey<RegistryConfiguredFeature> OakBees0002LeafLitter =
        FeatureUtils.CreateKey("oak_bees_0002_leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> OakBees002 =
        FeatureUtils.CreateKey("oak_bees_002");
    public static readonly ResourceKey<RegistryConfiguredFeature> OakBees005 =
        FeatureUtils.CreateKey("oak_bees_005");
    public static readonly ResourceKey<RegistryConfiguredFeature> BirchBees0002 =
        FeatureUtils.CreateKey("birch_bees_0002");
    public static readonly ResourceKey<RegistryConfiguredFeature> BirchBees0002LeafLitter =
        FeatureUtils.CreateKey("birch_bees_0002_leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> BirchBees002 =
        FeatureUtils.CreateKey("birch_bees_002");
    public static readonly ResourceKey<RegistryConfiguredFeature> BirchBees005 =
        FeatureUtils.CreateKey("birch_bees_005");
    public static readonly ResourceKey<RegistryConfiguredFeature> FancyOakBees0002LeafLitter =
        FeatureUtils.CreateKey("fancy_oak_bees_0002_leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> FancyOakBees002 =
        FeatureUtils.CreateKey("fancy_oak_bees_002");
    public static readonly ResourceKey<RegistryConfiguredFeature> FancyOakBees005 =
        FeatureUtils.CreateKey("fancy_oak_bees_005");
    public static readonly ResourceKey<RegistryConfiguredFeature> FancyOakBees =
        FeatureUtils.CreateKey("fancy_oak_bees");
    public static readonly ResourceKey<RegistryConfiguredFeature> CherryBees005 =
        FeatureUtils.CreateKey("cherry_bees_005");
    public static readonly ResourceKey<RegistryConfiguredFeature> OakLeafLitter =
        FeatureUtils.CreateKey("oak_leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> DarkOakLeafLitter =
        FeatureUtils.CreateKey("dark_oak_leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> BirchLeafLitter =
        FeatureUtils.CreateKey("birch_leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> FancyOakLeafLitter =
        FeatureUtils.CreateKey("fancy_oak_leaf_litter");
    public static readonly ResourceKey<RegistryConfiguredFeature> FallenOakTree =
        FeatureUtils.CreateKey("fallen_oak_tree");
    public static readonly ResourceKey<RegistryConfiguredFeature> FallenJungleTree =
        FeatureUtils.CreateKey("fallen_jungle_tree");
    public static readonly ResourceKey<RegistryConfiguredFeature> FallenSpruceTree =
        FeatureUtils.CreateKey("fallen_spruce_tree");
    public static readonly ResourceKey<RegistryConfiguredFeature> FallenBirchTree =
        FeatureUtils.CreateKey("fallen_birch_tree");
    public static readonly ResourceKey<RegistryConfiguredFeature> FallenSuperBirchTree =
        FeatureUtils.CreateKey("fallen_super_birch_tree");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        var stemReplaceable = BlockPredicate.MatchesBlocks(Blocks(
            "oak_sapling", "spruce_sapling", "birch_sapling", "jungle_sapling", "acacia_sapling",
            "cherry_sapling", "dark_oak_sapling", "pale_oak_sapling", "mangrove_propagule", "dandelion",
            "torchflower", "poppy", "blue_orchid", "allium", "azure_bluet", "red_tulip", "orange_tulip",
            "white_tulip", "pink_tulip", "oxeye_daisy", "cornflower", "wither_rose", "lily_of_the_valley",
            "brown_mushroom", "red_mushroom", "wheat", "sugar_cane", "attached_pumpkin_stem",
            "attached_melon_stem", "pumpkin_stem", "melon_stem", "lily_pad", "nether_wart", "cocoa", "carrots",
            "potatoes", "chorus_plant", "chorus_flower", "torchflower_crop", "pitcher_crop", "beetroots",
            "sweet_berry_bush", "warped_fungus", "crimson_fungus", "weeping_vines", "weeping_vines_plant",
            "twisting_vines", "twisting_vines_plant", "cave_vines", "cave_vines_plant", "spore_blossom",
            "azalea", "flowering_azalea", "moss_carpet", "pink_petals", "wildflowers", "big_dripleaf",
            "big_dripleaf_stem", "small_dripleaf"));
        foreach (var planted in new[] { false, true })
        {
            FeatureUtils.Register(context, planted ? CrimsonFungusPlanted : CrimsonFungus, HugeFungusFeature.Instance,
                new HugeFungusConfiguration(WorldGenDataSupport.State("crimson_nylium"),
                    WorldGenDataSupport.State("crimson_stem"), WorldGenDataSupport.State("nether_wart_block"),
                    WorldGenDataSupport.State("shroomlight"), stemReplaceable, planted));
            FeatureUtils.Register(context, planted ? WarpedFungusPlanted : WarpedFungus, HugeFungusFeature.Instance,
                new HugeFungusConfiguration(WorldGenDataSupport.State("warped_nylium"),
                    WorldGenDataSupport.State("warped_stem"), WorldGenDataSupport.State("warped_wart_block"),
                    WorldGenDataSupport.State("shroomlight"), stemReplaceable, planted));
        }
        var mushroomStem = BlockStateProvider.Simple(
            WorldGenDataSupport.State("mushroom_stem").TrySetValue(MushroomUp, false).TrySetValue(MushroomDown, false));
        FeatureUtils.Register(context, HugeBrownMushroom, HugeBrownMushroomFeature.Instance,
            new HugeMushroomFeatureConfiguration(
                BlockStateProvider.Simple(WorldGenDataSupport.State("brown_mushroom_block")
                    .TrySetValue(MushroomUp, true).TrySetValue(MushroomDown, false)),
                mushroomStem, 3, BlockPredicate.MatchesTag(BlockTags.HugeBrownMushroomCanPlaceOn)));
        FeatureUtils.Register(context, HugeRedMushroom, HugeRedMushroomFeature.Instance,
            new HugeMushroomFeatureConfiguration(
                BlockStateProvider.Simple(WorldGenDataSupport.State("red_mushroom_block")
                    .TrySetValue(MushroomDown, false)),
                mushroomStem, 2, BlockPredicate.MatchesTag(BlockTags.HugeRedMushroomCanPlaceOn)));

        var beehive0002 = new BeehiveDecorator(0.002f);
        var beehive001 = new BeehiveDecorator(0.01f);
        var beehive002 = new BeehiveDecorator(0.02f);
        var beehive005 = new BeehiveDecorator(0.05f);
        var beehive = new BeehiveDecorator(1.0f);
        var sparseLeafLitter = new PlaceOnGroundDecorator(96, 4, 2,
            new WeightedStateProvider(VegetationFeatures.LeafLitterPatchBuilder(1, 3)));
        var thickLeafLitter = new PlaceOnGroundDecorator(150, 2, 2,
            new WeightedStateProvider(VegetationFeatures.LeafLitterPatchBuilder(1, 4)));
        var belowTrunk = DefaultPlaceBelowTreeTrunkProvider();

        FeatureUtils.Register(context, Oak, TreeFeature.Instance, CreateOak(belowTrunk).Build());
        FeatureUtils.Register(context, DarkOak, TreeFeature.Instance,
            CreateDarkOak(belowTrunk).IgnoreVines().Build());
        var paleOak = new TreeBuilder(Provider("pale_oak_log"), new DarkOakTrunkPlacer(6, 2, 1),
            Provider("pale_oak_leaves"), new DarkOakFoliagePlacer(ConstantInt.Of(0), ConstantInt.Of(0)),
            new ThreeLayersFeatureSize(1, 1, 0, 1, 2, null), belowTrunk);
        FeatureUtils.Register(context, PaleOak, TreeFeature.Instance,
            paleOak.Decorators(new TreeDecorator[] { new PaleMossDecorator(0.15f, 0.4f, 0.8f) })
                .IgnoreVines().Build());
        FeatureUtils.Register(context, PaleOakBonemeal, TreeFeature.Instance, paleOak.IgnoreVines().Build());
        FeatureUtils.Register(context, PaleOakCreaking, TreeFeature.Instance,
            paleOak.Decorators(new TreeDecorator[]
            {
                new PaleMossDecorator(0.15f, 0.4f, 0.8f), new CreakingHeartDecorator(1.0f)
            }).IgnoreVines().Build());
        FeatureUtils.Register(context, Birch, TreeFeature.Instance, CreateBirch(belowTrunk).Build());
        FeatureUtils.Register(context, Acacia, TreeFeature.Instance, new TreeBuilder(
            Provider("acacia_log"), new ForkingTrunkPlacer(5, 2, 2), Provider("acacia_leaves"),
            new AcaciaFoliagePlacer(ConstantInt.Of(2), ConstantInt.Of(0)), new TwoLayersFeatureSize(1, 0, 2, null),
            belowTrunk).IgnoreVines().Build());
        FeatureUtils.Register(context, Cherry, TreeFeature.Instance, CreateCherry(belowTrunk).Build());
        FeatureUtils.Register(context, CherryBees005, TreeFeature.Instance,
            CreateCherry(belowTrunk).Decorators(new TreeDecorator[] { beehive005 }).Build());
        FeatureUtils.Register(context, Spruce, TreeFeature.Instance, new TreeBuilder(
            Provider("spruce_log"), new StraightTrunkPlacer(5, 2, 1), Provider("spruce_leaves"),
            new SpruceFoliagePlacer(UniformInt.Of(2, 3), UniformInt.Of(0, 2), UniformInt.Of(1, 2)),
            new TwoLayersFeatureSize(2, 0, 2, null), belowTrunk).IgnoreVines().Build());
        FeatureUtils.Register(context, Pine, TreeFeature.Instance, new TreeBuilder(
            Provider("spruce_log"), new StraightTrunkPlacer(6, 4, 0), Provider("spruce_leaves"),
            new PineFoliagePlacer(ConstantInt.Of(1), ConstantInt.Of(1), UniformInt.Of(3, 4)),
            new TwoLayersFeatureSize(2, 0, 2, null), belowTrunk).IgnoreVines().Build());
        FeatureUtils.Register(context, JungleTree, TreeFeature.Instance,
            CreateJungleTree(belowTrunk).Decorators(new TreeDecorator[]
            {
                new CocoaDecorator(0.2f), TrunkVineDecorator.Instance, new LeaveVineDecorator(0.25f)
            }).IgnoreVines().Build());
        FeatureUtils.Register(context, FancyOak, TreeFeature.Instance, CreateFancyOak(belowTrunk).Build());
        FeatureUtils.Register(context, JungleTreeNoVine, TreeFeature.Instance,
            CreateJungleTree(belowTrunk).IgnoreVines().Build());
        FeatureUtils.Register(context, MegaJungleTree, TreeFeature.Instance, new TreeBuilder(
            Provider("jungle_log"), new MegaJungleTrunkPlacer(10, 2, 19), Provider("jungle_leaves"),
            new MegaJungleFoliagePlacer(ConstantInt.Of(2), ConstantInt.Of(0), 2),
            new TwoLayersFeatureSize(1, 1, 2, null), belowTrunk).Decorators(new TreeDecorator[]
        {
            TrunkVineDecorator.Instance, new LeaveVineDecorator(0.25f)
        }).Build());
        var podzol = AlterGroundDecorator(RuleBasedStateProvider.IfTrueThenProvide(
            BlockPredicate.MatchesTag(BlockTags.BeneathTreePodzolReplaceable), WorldGenDataSupport.Block("podzol")));
        FeatureUtils.Register(context, MegaSpruce, TreeFeature.Instance, new TreeBuilder(
            Provider("spruce_log"), new GiantTrunkPlacer(13, 2, 14), Provider("spruce_leaves"),
            new MegaPineFoliagePlacer(ConstantInt.Of(0), ConstantInt.Of(0), UniformInt.Of(13, 17)),
            new TwoLayersFeatureSize(1, 1, 2, null), belowTrunk)
            .Decorators(new TreeDecorator[] { podzol }).Build());
        FeatureUtils.Register(context, MegaPine, TreeFeature.Instance, new TreeBuilder(
            Provider("spruce_log"), new GiantTrunkPlacer(13, 2, 14), Provider("spruce_leaves"),
            new MegaPineFoliagePlacer(ConstantInt.Of(0), ConstantInt.Of(0), UniformInt.Of(3, 7)),
            new TwoLayersFeatureSize(1, 1, 2, null), belowTrunk)
            .Decorators(new TreeDecorator[] { podzol }).Build());
        FeatureUtils.Register(context, SuperBirchBees0002, TreeFeature.Instance,
            CreateSuperBirch(belowTrunk).Decorators(new TreeDecorator[] { beehive0002 }).Build());
        FeatureUtils.Register(context, SuperBirchBees, TreeFeature.Instance,
            CreateSuperBirch(belowTrunk).Decorators(new TreeDecorator[] { beehive }).Build());
        FeatureUtils.Register(context, SwampOak, TreeFeature.Instance,
            CreateStraightBlobTree("oak_log", "oak_leaves", 5, 3, 0, 3, belowTrunk)
                .Decorators(new TreeDecorator[] { new LeaveVineDecorator(0.25f) }).Build());
        FeatureUtils.Register(context, JungleBush, TreeFeature.Instance, new TreeBuilder(
            Provider("jungle_log"), new StraightTrunkPlacer(1, 0, 0), Provider("oak_leaves"),
            new BushFoliagePlacer(ConstantInt.Of(2), ConstantInt.Of(1), 2), new TwoLayersFeatureSize(0, 0, 0, null),
            belowTrunk).Build());
        FeatureUtils.Register(context, AzaleaTree, TreeFeature.Instance, new TreeBuilder(
            Provider("oak_log"), new BendingTrunkPlacer(4, 2, 0, 3, UniformInt.Of(1, 2)),
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(WorldGenDataSupport.State("azalea_leaves"), 3)
                .Add(WorldGenDataSupport.State("flowering_azalea_leaves"), 1).Build()),
            new RandomSpreadFoliagePlacer(ConstantInt.Of(3), ConstantInt.Of(0), ConstantInt.Of(2), 50),
            new TwoLayersFeatureSize(1, 0, 1, null), belowTrunk)
            .BelowTrunkProvider(Provider("rooted_dirt")).Build());
        FeatureUtils.Register(context, Mangrove, TreeFeature.Instance, MangroveTree(2, 1, 4, UniformInt.Of(1, 4),
            new TwoLayersFeatureSize(2, 0, 2, null), 1, 3, beehive001).Build());
        FeatureUtils.Register(context, TallMangrove, TreeFeature.Instance, MangroveTree(4, 1, 9, UniformInt.Of(1, 6),
            new TwoLayersFeatureSize(3, 0, 2, null), 3, 7, beehive001).Build());
        FeatureUtils.Register(context, OakBees0002LeafLitter, TreeFeature.Instance, CreateOak(belowTrunk)
            .Decorators(new TreeDecorator[] { beehive0002, sparseLeafLitter, thickLeafLitter }).Build());
        FeatureUtils.Register(context, OakBees002, TreeFeature.Instance,
            CreateOak(belowTrunk).Decorators(new TreeDecorator[] { beehive002 }).Build());
        FeatureUtils.Register(context, OakBees005, TreeFeature.Instance,
            CreateOak(belowTrunk).Decorators(new TreeDecorator[] { beehive005 }).Build());
        FeatureUtils.Register(context, BirchBees0002, TreeFeature.Instance,
            CreateBirch(belowTrunk).Decorators(new TreeDecorator[] { beehive0002 }).Build());
        FeatureUtils.Register(context, BirchBees0002LeafLitter, TreeFeature.Instance, CreateBirch(belowTrunk)
            .Decorators(new TreeDecorator[] { beehive0002, sparseLeafLitter, thickLeafLitter }).Build());
        FeatureUtils.Register(context, BirchBees002, TreeFeature.Instance,
            CreateBirch(belowTrunk).Decorators(new TreeDecorator[] { beehive002 }).Build());
        FeatureUtils.Register(context, BirchBees005, TreeFeature.Instance,
            CreateBirch(belowTrunk).Decorators(new TreeDecorator[] { beehive005 }).Build());
        FeatureUtils.Register(context, FancyOakBees0002LeafLitter, TreeFeature.Instance, CreateFancyOak(belowTrunk)
            .Decorators(new TreeDecorator[] { beehive0002, sparseLeafLitter, thickLeafLitter }).Build());
        FeatureUtils.Register(context, FancyOakBees002, TreeFeature.Instance,
            CreateFancyOak(belowTrunk).Decorators(new TreeDecorator[] { beehive002 }).Build());
        FeatureUtils.Register(context, FancyOakBees005, TreeFeature.Instance,
            CreateFancyOak(belowTrunk).Decorators(new TreeDecorator[] { beehive005 }).Build());
        FeatureUtils.Register(context, FancyOakBees, TreeFeature.Instance,
            CreateFancyOak(belowTrunk).Decorators(new TreeDecorator[] { beehive }).Build());
        FeatureUtils.Register(context, OakLeafLitter, TreeFeature.Instance,
            CreateOak(belowTrunk).Decorators(new TreeDecorator[] { sparseLeafLitter, thickLeafLitter }).Build());
        FeatureUtils.Register(context, DarkOakLeafLitter, TreeFeature.Instance, CreateDarkOak(belowTrunk)
            .IgnoreVines().Decorators(new TreeDecorator[] { sparseLeafLitter, thickLeafLitter }).Build());
        FeatureUtils.Register(context, BirchLeafLitter, TreeFeature.Instance,
            CreateBirch(belowTrunk).Decorators(new TreeDecorator[] { sparseLeafLitter, thickLeafLitter }).Build());
        FeatureUtils.Register(context, FancyOakLeafLitter, TreeFeature.Instance,
            CreateFancyOak(belowTrunk).Decorators(new TreeDecorator[] { sparseLeafLitter, thickLeafLitter }).Build());
        FeatureUtils.Register(context, FallenOakTree, FallenTreeFeature.Instance, Fallen("oak_log", 4, 7));
        FeatureUtils.Register(context, FallenBirchTree, FallenTreeFeature.Instance, Fallen("birch_log", 5, 8));
        FeatureUtils.Register(context, FallenSuperBirchTree, FallenTreeFeature.Instance, Fallen("birch_log", 5, 15));
        FeatureUtils.Register(context, FallenJungleTree, FallenTreeFeature.Instance, Fallen("jungle_log", 4, 11));
        FeatureUtils.Register(context, FallenSpruceTree, FallenTreeFeature.Instance, Fallen("spruce_log", 6, 10));
    }

    //Provider simple block state provider by registry path
    private static BlockStateProvider Provider(string path) => BlockStateProvider.Simple(WorldGenDataSupport.Block(path));

    //Blocks resolver for a block array
    private static RegBlock[] Blocks(params string[] paths) => paths.Select(WorldGenDataSupport.Block).ToArray();

    //DefaultPlaceBelowTreeTrunkProvider dirt below the trunk unless the block forbids replacement, maps to vanilla defaultPlaceBelowTreeTrunkProvider
    private static BlockStateProvider DefaultPlaceBelowTreeTrunkProvider()
        => RuleBasedStateProvider.IfTrueThenProvide(
            BlockPredicate.Not(BlockPredicate.MatchesTag(BlockTags.CannotReplaceBelowTreeTrunk)),
            WorldGenDataSupport.Block("dirt"));

    //AlterGroundDecorator ground replacer, maps to the one-provider alter ground form
    private static AlterGroundDecorator AlterGroundDecorator(BlockStateProvider provider) => new(provider);

    //CreateStraightBlobTree straight trunk with a blob canopy, maps to vanilla helper of the same name
    private static TreeBuilder CreateStraightBlobTree(string log, string leaves, int baseHeight, int heightRandA,
        int heightRandB, int blobRadius, BlockStateProvider belowTrunkProvider)
        => new(Provider(log), new StraightTrunkPlacer(baseHeight, heightRandA, heightRandB), Provider(leaves),
            new BlobFoliagePlacer(ConstantInt.Of(blobRadius), ConstantInt.Of(0), 3),
            new TwoLayersFeatureSize(1, 0, 1, null), belowTrunkProvider);

    private static TreeBuilder CreateOak(BlockStateProvider belowTrunkProvider)
        => CreateStraightBlobTree("oak_log", "oak_leaves", 4, 2, 0, 2, belowTrunkProvider).IgnoreVines();

    private static TreeBuilder CreateDarkOak(BlockStateProvider belowTrunkProvider)
        => new(Provider("dark_oak_log"), new DarkOakTrunkPlacer(6, 2, 1), Provider("dark_oak_leaves"),
            new DarkOakFoliagePlacer(ConstantInt.Of(0), ConstantInt.Of(0)),
            new ThreeLayersFeatureSize(1, 1, 0, 1, 2, null), belowTrunkProvider);

    private static TreeBuilder CreateBirch(BlockStateProvider belowTrunkProvider)
        => CreateStraightBlobTree("birch_log", "birch_leaves", 5, 2, 0, 2, belowTrunkProvider).IgnoreVines();

    private static TreeBuilder CreateSuperBirch(BlockStateProvider belowTrunkProvider)
        => CreateStraightBlobTree("birch_log", "birch_leaves", 5, 2, 6, 2, belowTrunkProvider).IgnoreVines();

    private static TreeBuilder CreateJungleTree(BlockStateProvider belowTrunkProvider)
        => CreateStraightBlobTree("jungle_log", "jungle_leaves", 4, 8, 0, 2, belowTrunkProvider);

    private static TreeBuilder CreateFancyOak(BlockStateProvider belowTrunkProvider)
        => new TreeBuilder(Provider("oak_log"), new FancyTrunkPlacer(3, 11, 0), Provider("oak_leaves"),
            new FancyFoliagePlacer(ConstantInt.Of(2), ConstantInt.Of(4), 4),
            new TwoLayersFeatureSize(0, 0, 0, 4), belowTrunkProvider).IgnoreVines();

    private static TreeBuilder CreateCherry(BlockStateProvider belowTrunkProvider)
        => new TreeBuilder(Provider("cherry_log"), new CherryTrunkPlacer(7, 1, 0,
                new WeightedListInt(WeightedList<GameIntProvider>.Builder()
                    .Add(ConstantInt.Of(1), 1).Add(ConstantInt.Of(2), 1).Add(ConstantInt.Of(3), 1).Build()),
                UniformInt.Of(2, 4), UniformInt.Of(-4, -3), UniformInt.Of(-1, 0)),
            Provider("cherry_leaves"),
            new CherryFoliagePlacer(ConstantInt.Of(4), ConstantInt.Of(0), ConstantInt.Of(5), 0.25f, 0.5f,
                0.16666667f, 0.33333334f),
            new TwoLayersFeatureSize(1, 0, 2, null), belowTrunkProvider).IgnoreVines();

    //MangroveTree the mangrove trunk with its root placer and propagule decorations
    private static TreeBuilder MangroveTree(int baseHeight, int heightRandA, int heightRandB, GameIntProvider branchCount,
        FeatureSize size, int minRoot, int maxRoot, BeehiveDecorator beehive001)
    {
        var logsCanGrowThrough = BuiltInRegistries.BLOCK.GetOrCreate(BlockTags.MangroveLogsCanGrowThrough);
        var rootsCanGrowThrough = BuiltInRegistries.BLOCK.GetOrCreate(BlockTags.MangroveRootsCanGrowThrough);
        var muddyRoots = new DirectHolderSet<RegBlock>(new[]
        {
            BuiltInRegistries.BLOCK.WrapAsHolder(WorldGenDataSupport.Block("mud")),
            BuiltInRegistries.BLOCK.WrapAsHolder(WorldGenDataSupport.Block("muddy_mangrove_roots"))
        });
        var rootPlacer = new MangroveRootPlacer(UniformInt.Of(minRoot, maxRoot), Provider("mangrove_roots"),
            Optional<AboveRootPlacement>.Of(new AboveRootPlacement(Provider("moss_carpet"), 0.5f)),
            new MangroveRootPlacement(rootsCanGrowThrough, muddyRoots, Provider("muddy_mangrove_roots"), 8, 15, 0.2f));
        var propagule = new RandomizedIntStateProvider(
            BlockStateProvider.Simple(WorldGenDataSupport.State("mangrove_propagule")
                .TrySetValue(PropaguleHanging, true)), "age", UniformInt.Of(0, 4));
        return new TreeBuilder(Provider("mangrove_log"),
            new UpwardsBranchingTrunkPlacer(baseHeight, heightRandA, heightRandB, branchCount, 0.5f,
                UniformInt.Of(0, 1), logsCanGrowThrough),
            Provider("mangrove_leaves"),
            new RandomSpreadFoliagePlacer(ConstantInt.Of(3), ConstantInt.Of(0), ConstantInt.Of(2), 70),
            Optional<RootPlacer>.Of(rootPlacer), size, DefaultPlaceBelowTreeTrunkProvider())
            .Decorators(new TreeDecorator[]
            {
                new LeaveVineDecorator(0.125f),
                new AttachedToLeavesDecorator(0.14f, 1, 0, propagule, 2, new[] { Direction.Down }),
                beehive001
            })
            .IgnoreVines();
    }

    //Fallen fallen tree configuration; the stump and log decorator lists are parsed but not executed by this project
    private static FallenTreeConfiguration Fallen(string log, int minLength, int maxLength)
        => new(Provider(log), UniformInt.Of(minLength, maxLength), Array.Empty<IgnoredJsonValue>(),
            Array.Empty<IgnoredJsonValue>());

    //TreeBuilder the vanilla tree configuration builder form, reconstructed locally because the NC configuration has no builder
    private sealed class TreeBuilder
    {
        private readonly BlockStateProvider _trunkProvider;
        private readonly TrunkPlacer _trunkPlacer;
        private readonly BlockStateProvider _foliageProvider;
        private readonly FoliagePlacer _foliagePlacer;
        private readonly Optional<RootPlacer> _rootPlacer;
        private readonly FeatureSize _minimumSize;
        private BlockStateProvider _belowTrunkProvider;
        private IReadOnlyList<TreeDecorator> _decorators = Array.Empty<TreeDecorator>();
        private bool _ignoreVines;

        public TreeBuilder(BlockStateProvider trunkProvider, TrunkPlacer trunkPlacer,
            BlockStateProvider foliageProvider, FoliagePlacer foliagePlacer, FeatureSize minimumSize,
            BlockStateProvider belowTrunkProvider)
            : this(trunkProvider, trunkPlacer, foliageProvider, foliagePlacer, Optional<RootPlacer>.Empty(),
                minimumSize, belowTrunkProvider)
        {
        }

        public TreeBuilder(BlockStateProvider trunkProvider, TrunkPlacer trunkPlacer,
            BlockStateProvider foliageProvider, FoliagePlacer foliagePlacer, Optional<RootPlacer> rootPlacer,
            FeatureSize minimumSize, BlockStateProvider belowTrunkProvider)
        {
            _trunkProvider = trunkProvider;
            _trunkPlacer = trunkPlacer;
            _foliageProvider = foliageProvider;
            _foliagePlacer = foliagePlacer;
            _rootPlacer = rootPlacer;
            _minimumSize = minimumSize;
            _belowTrunkProvider = belowTrunkProvider;
        }

        public TreeBuilder BelowTrunkProvider(BlockStateProvider provider)
        {
            _belowTrunkProvider = provider;
            return this;
        }

        public TreeBuilder Decorators(IReadOnlyList<TreeDecorator> decorators)
        {
            _decorators = decorators;
            return this;
        }

        public TreeBuilder IgnoreVines()
        {
            _ignoreVines = true;
            return this;
        }

        public TreeConfiguration Build()
            => new(_trunkProvider, _trunkPlacer, _foliageProvider, _foliagePlacer, _rootPlacer, _minimumSize,
                _decorators, _ignoreVines, _belowTrunkProvider);
    }
}
