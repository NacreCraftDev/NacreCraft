using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using Direction = NacreCraft.Primitives.Direction;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//VegetationPlacements built-in placed vegetation and biome tree mixes, maps to vanilla net.minecraft.data.worldgen.placement.VegetationPlacements
//The patch entries chain the same count/spread/survival filters vanilla uses; tree entries share the surface-water threshold
public static class VegetationPlacements
{
    //TreeThreshold trees only on dry surface, maps to vanilla TREE_THRESHOLD
    private static readonly PlacementModifier TreeThreshold = SurfaceWaterDepthFilter.ForMaxDepth(0);

    public static readonly ResourceKey<RegistryPlacedFeature> BambooLight =
        PlacementUtils.CreateKey("bamboo_light");
    public static readonly ResourceKey<RegistryPlacedFeature> Bamboo = PlacementUtils.CreateKey("bamboo");
    public static readonly ResourceKey<RegistryPlacedFeature> Vines = PlacementUtils.CreateKey("vines");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchSunflower =
        PlacementUtils.CreateKey("patch_sunflower");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchPumpkin =
        PlacementUtils.CreateKey("patch_pumpkin");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassPlain =
        PlacementUtils.CreateKey("patch_grass_plain");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassMeadow =
        PlacementUtils.CreateKey("patch_grass_meadow");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassForest =
        PlacementUtils.CreateKey("patch_grass_forest");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassBadlands =
        PlacementUtils.CreateKey("patch_grass_badlands");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassSavanna =
        PlacementUtils.CreateKey("patch_grass_savanna");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassNormal =
        PlacementUtils.CreateKey("patch_grass_normal");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassTaiga2 =
        PlacementUtils.CreateKey("patch_grass_taiga_2");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassTaiga =
        PlacementUtils.CreateKey("patch_grass_taiga");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchGrassJungle =
        PlacementUtils.CreateKey("patch_grass_jungle");
    public static readonly ResourceKey<RegistryPlacedFeature> GrassBonemeal =
        PlacementUtils.CreateKey("grass_bonemeal");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchDeadBush2 =
        PlacementUtils.CreateKey("patch_dead_bush_2");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchDeadBush =
        PlacementUtils.CreateKey("patch_dead_bush");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchDeadBushBadlands =
        PlacementUtils.CreateKey("patch_dead_bush_badlands");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchDryGrassBadlands =
        PlacementUtils.CreateKey("patch_dry_grass_badlands");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchDryGrassDesert =
        PlacementUtils.CreateKey("patch_dry_grass_desert");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchMelon =
        PlacementUtils.CreateKey("patch_melon");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchMelonSparse =
        PlacementUtils.CreateKey("patch_melon_sparse");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchBerryCommon =
        PlacementUtils.CreateKey("patch_berry_common");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchBerryRare =
        PlacementUtils.CreateKey("patch_berry_rare");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchWaterlily =
        PlacementUtils.CreateKey("patch_waterlily");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchTallGrass2 =
        PlacementUtils.CreateKey("patch_tall_grass_2");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchTallGrass =
        PlacementUtils.CreateKey("patch_tall_grass");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchLargeFern =
        PlacementUtils.CreateKey("patch_large_fern");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchBush =
        PlacementUtils.CreateKey("patch_bush");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchLeafLitter =
        PlacementUtils.CreateKey("patch_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchCactusDesert =
        PlacementUtils.CreateKey("patch_cactus_desert");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchCactusDecorated =
        PlacementUtils.CreateKey("patch_cactus_decorated");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchSugarCaneSwamp =
        PlacementUtils.CreateKey("patch_sugar_cane_swamp");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchSugarCaneDesert =
        PlacementUtils.CreateKey("patch_sugar_cane_desert");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchSugarCaneBadlands =
        PlacementUtils.CreateKey("patch_sugar_cane_badlands");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchSugarCane =
        PlacementUtils.CreateKey("patch_sugar_cane");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchFireflyBushSwamp =
        PlacementUtils.CreateKey("patch_firefly_bush_swamp");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchFireflyBushNearWaterSwamp =
        PlacementUtils.CreateKey("patch_firefly_bush_near_water_swamp");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchFireflyBushNearWater =
        PlacementUtils.CreateKey("patch_firefly_bush_near_water");
    public static readonly ResourceKey<RegistryPlacedFeature> BrownMushroomNether =
        PlacementUtils.CreateKey("brown_mushroom_nether");
    public static readonly ResourceKey<RegistryPlacedFeature> RedMushroomNether =
        PlacementUtils.CreateKey("red_mushroom_nether");
    public static readonly ResourceKey<RegistryPlacedFeature> BrownMushroomNormal =
        PlacementUtils.CreateKey("brown_mushroom_normal");
    public static readonly ResourceKey<RegistryPlacedFeature> RedMushroomNormal =
        PlacementUtils.CreateKey("red_mushroom_normal");
    public static readonly ResourceKey<RegistryPlacedFeature> BrownMushroomTaiga =
        PlacementUtils.CreateKey("brown_mushroom_taiga");
    public static readonly ResourceKey<RegistryPlacedFeature> RedMushroomTaiga =
        PlacementUtils.CreateKey("red_mushroom_taiga");
    public static readonly ResourceKey<RegistryPlacedFeature> BrownMushroomOldGrowth =
        PlacementUtils.CreateKey("brown_mushroom_old_growth");
    public static readonly ResourceKey<RegistryPlacedFeature> RedMushroomOldGrowth =
        PlacementUtils.CreateKey("red_mushroom_old_growth");
    public static readonly ResourceKey<RegistryPlacedFeature> BrownMushroomSwamp =
        PlacementUtils.CreateKey("brown_mushroom_swamp");
    public static readonly ResourceKey<RegistryPlacedFeature> RedMushroomSwamp =
        PlacementUtils.CreateKey("red_mushroom_swamp");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerWarm =
        PlacementUtils.CreateKey("flower_warm");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerDefault =
        PlacementUtils.CreateKey("flower_default");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerFlowerForest =
        PlacementUtils.CreateKey("flower_flower_forest");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerSwamp =
        PlacementUtils.CreateKey("flower_swamp");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerPlains =
        PlacementUtils.CreateKey("flower_plains");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerMeadow =
        PlacementUtils.CreateKey("flower_meadow");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerCherry =
        PlacementUtils.CreateKey("flower_cherry");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerPaleGarden =
        PlacementUtils.CreateKey("flower_pale_garden");
    public static readonly ResourceKey<RegistryPlacedFeature> WildflowersBirchForest =
        PlacementUtils.CreateKey("wildflowers_birch_forest");
    public static readonly ResourceKey<RegistryPlacedFeature> WildflowersMeadow =
        PlacementUtils.CreateKey("wildflowers_meadow");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesPlains =
        PlacementUtils.CreateKey("trees_plains");
    public static readonly ResourceKey<RegistryPlacedFeature> DarkForestVegetation =
        PlacementUtils.CreateKey("dark_forest_vegetation");
    public static readonly ResourceKey<RegistryPlacedFeature> PaleGardenVegetation =
        PlacementUtils.CreateKey("pale_garden_vegetation");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerForestFlowers =
        PlacementUtils.CreateKey("flower_forest_flowers");
    public static readonly ResourceKey<RegistryPlacedFeature> ForestFlowers =
        PlacementUtils.CreateKey("forest_flowers");
    public static readonly ResourceKey<RegistryPlacedFeature> PaleGardenFlowers =
        PlacementUtils.CreateKey("pale_garden_flowers");
    public static readonly ResourceKey<RegistryPlacedFeature> PaleMossPatch =
        PlacementUtils.CreateKey("pale_moss_patch");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesFlowerForest =
        PlacementUtils.CreateKey("trees_flower_forest");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesMeadow =
        PlacementUtils.CreateKey("trees_meadow");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesCherry =
        PlacementUtils.CreateKey("trees_cherry");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesTaiga =
        PlacementUtils.CreateKey("trees_taiga");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesGrove =
        PlacementUtils.CreateKey("trees_grove");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesBadlands =
        PlacementUtils.CreateKey("trees_badlands");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesSnowy =
        PlacementUtils.CreateKey("trees_snowy");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesSwamp =
        PlacementUtils.CreateKey("trees_swamp");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesWindsweptSavanna =
        PlacementUtils.CreateKey("trees_windswept_savanna");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesSavanna =
        PlacementUtils.CreateKey("trees_savanna");
    public static readonly ResourceKey<RegistryPlacedFeature> BirchTall =
        PlacementUtils.CreateKey("birch_tall");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesBirch =
        PlacementUtils.CreateKey("trees_birch");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesWindsweptForest =
        PlacementUtils.CreateKey("trees_windswept_forest");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesWindsweptHills =
        PlacementUtils.CreateKey("trees_windswept_hills");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesWater =
        PlacementUtils.CreateKey("trees_water");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesBirchAndOakLeafLitter =
        PlacementUtils.CreateKey("trees_birch_and_oak_leaf_litter");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesSparseJungle =
        PlacementUtils.CreateKey("trees_sparse_jungle");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesOldGrowthSpruceTaiga =
        PlacementUtils.CreateKey("trees_old_growth_spruce_taiga");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesOldGrowthPineTaiga =
        PlacementUtils.CreateKey("trees_old_growth_pine_taiga");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesJungle =
        PlacementUtils.CreateKey("trees_jungle");
    public static readonly ResourceKey<RegistryPlacedFeature> BambooVegetation =
        PlacementUtils.CreateKey("bamboo_vegetation");
    public static readonly ResourceKey<RegistryPlacedFeature> MushroomIslandVegetation =
        PlacementUtils.CreateKey("mushroom_island_vegetation");
    public static readonly ResourceKey<RegistryPlacedFeature> TreesMangrove =
        PlacementUtils.CreateKey("trees_mangrove");

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var bambooNoPodzol = features.GetOrThrow(VegetationFeatures.BambooNoPodzol);
        var bambooSomePodzol = features.GetOrThrow(VegetationFeatures.BambooSomePodzol);
        var vines = features.GetOrThrow(VegetationFeatures.Vines);
        var sunflower = features.GetOrThrow(VegetationFeatures.Sunflower);
        var pumpkin = features.GetOrThrow(VegetationFeatures.Pumpkin);
        var grass = features.GetOrThrow(VegetationFeatures.Grass);
        var leafLitter = features.GetOrThrow(VegetationFeatures.LeafLitter);
        var taigaGrass = features.GetOrThrow(VegetationFeatures.TaigaGrass);
        var grassJungle = features.GetOrThrow(VegetationFeatures.GrassJungle);
        var deadBush = features.GetOrThrow(VegetationFeatures.DeadBush);
        var dryGrass = features.GetOrThrow(VegetationFeatures.DryGrass);
        var fireflyBush = features.GetOrThrow(VegetationFeatures.FireflyBush);
        var melon = features.GetOrThrow(VegetationFeatures.Melon);
        var berryBush = features.GetOrThrow(VegetationFeatures.BerryBush);
        var waterlily = features.GetOrThrow(VegetationFeatures.Waterlily);
        var tallGrass = features.GetOrThrow(VegetationFeatures.TallGrass);
        var largeFern = features.GetOrThrow(VegetationFeatures.LargeFern);
        var bush = features.GetOrThrow(VegetationFeatures.Bush);
        var cactus = features.GetOrThrow(VegetationFeatures.Cactus);
        var sugarCane = features.GetOrThrow(VegetationFeatures.SugarCane);
        var brownMushroom = features.GetOrThrow(VegetationFeatures.BrownMushroom);
        var redMushroom = features.GetOrThrow(VegetationFeatures.RedMushroom);
        var flowerDefault = features.GetOrThrow(VegetationFeatures.FlowerDefault);
        var flowerFlowerForest = features.GetOrThrow(VegetationFeatures.FlowerFlowerForest);
        var flowerSwamp = features.GetOrThrow(VegetationFeatures.FlowerSwamp);
        var flowerPlain = features.GetOrThrow(VegetationFeatures.FlowerPlain);
        var flowerMeadow = features.GetOrThrow(VegetationFeatures.FlowerMeadow);
        var flowerCherry = features.GetOrThrow(VegetationFeatures.FlowerCherry);
        var flowerPaleGarden = features.GetOrThrow(VegetationFeatures.FlowerPaleGarden);
        var wildflower = features.GetOrThrow(VegetationFeatures.Wildflower);
        var treesPlains = features.GetOrThrow(VegetationFeatures.TreesPlains);
        var darkForestVegetation = features.GetOrThrow(VegetationFeatures.DarkForestVegetation);
        var paleGardenVegetation = features.GetOrThrow(VegetationFeatures.PaleGardenVegetation);
        var forestFlowers = features.GetOrThrow(VegetationFeatures.ForestFlowers);
        var paleForestFlower = features.GetOrThrow(VegetationFeatures.PaleForestFlower);
        var paleMossPatch = features.GetOrThrow(VegetationFeatures.PaleMossPatch);
        var treesFlowerForest = features.GetOrThrow(VegetationFeatures.TreesFlowerForest);
        var meadowTrees = features.GetOrThrow(VegetationFeatures.MeadowTrees);
        var treesTaiga = features.GetOrThrow(VegetationFeatures.TreesTaiga);
        var treesBadlands = features.GetOrThrow(VegetationFeatures.TreesBadlands);
        var treesGrove = features.GetOrThrow(VegetationFeatures.TreesGrove);
        var treesSnowy = features.GetOrThrow(VegetationFeatures.TreesSnowy);
        var cherryBees005 = features.GetOrThrow(TreeFeatures.CherryBees005);
        var swampOak = features.GetOrThrow(TreeFeatures.SwampOak);
        var treesSavanna = features.GetOrThrow(VegetationFeatures.TreesSavanna);
        var birchTall = features.GetOrThrow(VegetationFeatures.BirchTall);
        var treesBirch = features.GetOrThrow(VegetationFeatures.TreesBirch);
        var treesWindsweptHills = features.GetOrThrow(VegetationFeatures.TreesWindsweptHills);
        var treesWater = features.GetOrThrow(VegetationFeatures.TreesWater);
        var treesBirchAndOakLeafLitter = features.GetOrThrow(VegetationFeatures.TreesBirchAndOakLeafLitter);
        var treesSparseJungle = features.GetOrThrow(VegetationFeatures.TreesSparseJungle);
        var treesOldGrowthSpruceTaiga = features.GetOrThrow(VegetationFeatures.TreesOldGrowthSpruceTaiga);
        var treesOldGrowthPineTaiga = features.GetOrThrow(VegetationFeatures.TreesOldGrowthPineTaiga);
        var treesJungle = features.GetOrThrow(VegetationFeatures.TreesJungle);
        var bambooVegetation = features.GetOrThrow(VegetationFeatures.BambooVegetation);
        var mushroomIslandVegetation = features.GetOrThrow(VegetationFeatures.MushroomIslandVegetation);
        var mangroveVegetation = features.GetOrThrow(VegetationFeatures.MangroveVegetation);

        var onlyAir = BlockPredicateFilter.ForPredicate(BlockPredicate.OnlyInAir);
        var grassBelow = BlockPredicateFilter.ForPredicate(BlockPredicate.AllOf(BlockPredicate.OnlyInAir,
            BlockPredicate.MatchesBlocks(new Vec3i(0, -1, 0), WorldGenDataSupport.Block("grass_block"))));
        var melonPredicate = BlockPredicate.AllOf(BlockPredicate.Replaceable(), BlockPredicate.NoFluid(),
            BlockPredicate.MatchesBlocks(new Vec3i(0, -1, 0), WorldGenDataSupport.Block("grass_block")));

        PlacementUtils.Register(context, BambooLight, bambooNoPodzol, RarityFilter.OnAverageOnceEvery(4),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome());
        PlacementUtils.Register(context, Bamboo, bambooSomePodzol, NoiseBasedCountPlacement.Of(160, 80.0, 0.3),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome());
        PlacementUtils.Register(context, Vines, vines, CountPlacement.Of(127), InSquarePlacement.Spread(),
            HeightRangePlacement.Uniform(VerticalAnchor.Absolute(64), VerticalAnchor.Absolute(100)),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, PatchSunflower, sunflower, RarityFilter.OnAverageOnceEvery(3),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchPumpkin, pumpkin, RarityFilter.OnAverageOnceEvery(300),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3), grassBelow);
        PlacementUtils.Register(context, PatchGrassPlain, grass, NoiseThresholdCountPlacement.Of(-0.8, 5, 10),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome(),
            CountPlacement.Of(32), RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchGrassMeadow, grass, NoiseThresholdCountPlacement.Of(-0.8, 5, 10),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome(),
            CountPlacement.Of(16), RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchGrassForest, grass,
            Add(WorldSurfaceSquaredWithCount(2), CountPlacement.Of(32), RandomOffsetPlacement.OfTriangle(7, 3), onlyAir));
        PlacementUtils.Register(context, PatchLeafLitter, leafLitter,
            Add(WorldSurfaceSquaredWithCount(2), CountPlacement.Of(32), RandomOffsetPlacement.OfTriangle(7, 3),
                grassBelow));
        PlacementUtils.Register(context, PatchGrassBadlands, grass, InSquarePlacement.Spread(),
            PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome(), CountPlacement.Of(32),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchGrassSavanna, grass,
            Add(WorldSurfaceSquaredWithCount(20), CountPlacement.Of(32), RandomOffsetPlacement.OfTriangle(7, 3),
                onlyAir));
        PlacementUtils.Register(context, PatchGrassNormal, grass,
            Add(WorldSurfaceSquaredWithCount(5), CountPlacement.Of(32), RandomOffsetPlacement.OfTriangle(7, 3),
                onlyAir));
        PlacementUtils.Register(context, PatchGrassTaiga2, taigaGrass, InSquarePlacement.Spread(),
            PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome(), CountPlacement.Of(32),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchGrassTaiga, taigaGrass,
            Add(WorldSurfaceSquaredWithCount(7), CountPlacement.Of(32), RandomOffsetPlacement.OfTriangle(7, 3),
                onlyAir));
        PlacementUtils.Register(context, PatchGrassJungle, grassJungle,
            Add(WorldSurfaceSquaredWithCount(25), CountPlacement.Of(32), RandomOffsetPlacement.OfTriangle(7, 3),
                BlockPredicateFilter.ForPredicate(BlockPredicate.AllOf(BlockPredicate.OnlyInAir,
                    BlockPredicate.Not(BlockPredicate.MatchesBlocks(new Vec3i(0, -1, 0),
                        WorldGenDataSupport.Block("podzol")))))));
        PlacementUtils.Register(context, GrassBonemeal, grass, PlacementUtils.IsEmpty());
        PlacementUtils.Register(context, PatchDeadBush2, deadBush,
            Add(WorldSurfaceSquaredWithCount(2), CountPlacement.Of(4), RandomOffsetPlacement.OfTriangle(7, 3), onlyAir));
        PlacementUtils.Register(context, PatchDeadBush, deadBush, InSquarePlacement.Spread(),
            PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome(), CountPlacement.Of(4),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchDeadBushBadlands, deadBush,
            Add(WorldSurfaceSquaredWithCount(20), CountPlacement.Of(4), RandomOffsetPlacement.OfTriangle(7, 3),
                onlyAir));
        PlacementUtils.Register(context, PatchDryGrassBadlands, dryGrass, RarityFilter.OnAverageOnceEvery(6),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(64),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchDryGrassDesert, dryGrass, RarityFilter.OnAverageOnceEvery(3),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(64),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        var melonFilter = BlockPredicateFilter.ForPredicate(melonPredicate);
        PlacementUtils.Register(context, PatchMelon, melon, RarityFilter.OnAverageOnceEvery(6),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(64),
            RandomOffsetPlacement.OfTriangle(7, 3), melonFilter);
        PlacementUtils.Register(context, PatchMelonSparse, melon, RarityFilter.OnAverageOnceEvery(64),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(64),
            RandomOffsetPlacement.OfTriangle(7, 3), melonFilter);
        PlacementUtils.Register(context, PatchBerryCommon, berryBush, RarityFilter.OnAverageOnceEvery(32),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome(),
            CountPlacement.Of(96), RandomOffsetPlacement.OfTriangle(7, 3), grassBelow);
        PlacementUtils.Register(context, PatchBerryRare, berryBush, RarityFilter.OnAverageOnceEvery(384),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome(),
            CountPlacement.Of(96), RandomOffsetPlacement.OfTriangle(7, 3), grassBelow);
        PlacementUtils.Register(context, PatchWaterlily, waterlily,
            Add(WorldSurfaceSquaredWithCount(4), CountPlacement.Of(10), RandomOffsetPlacement.OfTriangle(7, 3),
                onlyAir));
        PlacementUtils.Register(context, PatchTallGrass2, tallGrass, NoiseThresholdCountPlacement.Of(-0.8, 0, 7),
            RarityFilter.OnAverageOnceEvery(32), InSquarePlacement.Spread(), PlacementUtils.Heightmap,
            BiomeFilter.Biome(), CountPlacement.Of(96), RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchTallGrass, tallGrass, RarityFilter.OnAverageOnceEvery(5),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchLargeFern, largeFern, RarityFilter.OnAverageOnceEvery(5),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PatchBush, bush, RarityFilter.OnAverageOnceEvery(4),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(24),
            RandomOffsetPlacement.OfTriangle(5, 3), onlyAir);
        var cactusFilter = BlockPredicateFilter.ForPredicate(BlockPredicate.AllOf(BlockPredicate.OnlyInAir,
            BlockPredicate.WouldSurvive(WorldGenDataSupport.State("cactus"), Vec3i.Zero)));
        PlacementUtils.Register(context, PatchCactusDesert, cactus, RarityFilter.OnAverageOnceEvery(6),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(10),
            RandomOffsetPlacement.OfTriangle(7, 3), cactusFilter);
        PlacementUtils.Register(context, PatchCactusDecorated, cactus, RarityFilter.OnAverageOnceEvery(13),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(10),
            RandomOffsetPlacement.OfTriangle(7, 3), cactusFilter);
        var sugarCaneFilter = VegetationFeatures.NearWaterPredicate(WorldGenDataSupport.Block("sugar_cane"));
        PlacementUtils.Register(context, PatchSugarCaneSwamp, sugarCane, RarityFilter.OnAverageOnceEvery(3),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(20),
            RandomOffsetPlacement.OfTriangle(4, 0), sugarCaneFilter);
        PlacementUtils.Register(context, PatchSugarCaneDesert, sugarCane, InSquarePlacement.Spread(),
            PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(20),
            RandomOffsetPlacement.OfTriangle(4, 0), sugarCaneFilter);
        PlacementUtils.Register(context, PatchSugarCaneBadlands, sugarCane, RarityFilter.OnAverageOnceEvery(5),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(20),
            RandomOffsetPlacement.OfTriangle(4, 0), sugarCaneFilter);
        PlacementUtils.Register(context, PatchSugarCane, sugarCane, RarityFilter.OnAverageOnceEvery(6),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(20),
            RandomOffsetPlacement.OfTriangle(4, 0), sugarCaneFilter);
        var fireflyNearWater = VegetationFeatures.NearWaterPredicate(WorldGenDataSupport.Block("firefly_bush"));
        PlacementUtils.Register(context, PatchFireflyBushNearWater, fireflyBush, CountPlacement.Of(2),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapNoLeaves, BiomeFilter.Biome(), fireflyNearWater,
            CountPlacement.Of(20), RandomOffsetPlacement.OfTriangle(4, 3), onlyAir);
        PlacementUtils.Register(context, PatchFireflyBushNearWaterSwamp, fireflyBush, CountPlacement.Of(3),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), fireflyNearWater,
            CountPlacement.Of(20), RandomOffsetPlacement.OfTriangle(4, 3), onlyAir);
        PlacementUtils.Register(context, PatchFireflyBushSwamp, fireflyBush, RarityFilter.OnAverageOnceEvery(8),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(20),
            RandomOffsetPlacement.OfTriangle(4, 3), onlyAir);
        PlacementUtils.Register(context, BrownMushroomNether, brownMushroom, RarityFilter.OnAverageOnceEvery(2),
            InSquarePlacement.Spread(), PlacementUtils.FullRange, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, RedMushroomNether, redMushroom, RarityFilter.OnAverageOnceEvery(2),
            InSquarePlacement.Spread(), PlacementUtils.FullRange, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, BrownMushroomNormal, brownMushroom, MushroomPlacement(256, null));
        PlacementUtils.Register(context, RedMushroomNormal, redMushroom, MushroomPlacement(512, null));
        PlacementUtils.Register(context, BrownMushroomTaiga, brownMushroom, MushroomPlacement(4, null));
        PlacementUtils.Register(context, RedMushroomTaiga, redMushroom, MushroomPlacement(256, null));
        PlacementUtils.Register(context, BrownMushroomOldGrowth, brownMushroom,
            MushroomPlacement(4, CountPlacement.Of(3)));
        PlacementUtils.Register(context, RedMushroomOldGrowth, redMushroom, MushroomPlacement(171, null));
        PlacementUtils.Register(context, BrownMushroomSwamp, brownMushroom,
            MushroomPlacement(0, CountPlacement.Of(2)));
        PlacementUtils.Register(context, RedMushroomSwamp, redMushroom, MushroomPlacement(64, null));
        PlacementUtils.Register(context, FlowerWarm, flowerDefault, RarityFilter.OnAverageOnceEvery(16),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(64),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, FlowerDefault, flowerDefault, RarityFilter.OnAverageOnceEvery(32),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(64),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, FlowerFlowerForest, flowerFlowerForest, CountPlacement.Of(3),
            RarityFilter.OnAverageOnceEvery(2), InSquarePlacement.Spread(), PlacementUtils.Heightmap,
            BiomeFilter.Biome(), CountPlacement.Of(96), RandomOffsetPlacement.OfTriangle(6, 2), onlyAir);
        PlacementUtils.Register(context, FlowerSwamp, flowerSwamp, RarityFilter.OnAverageOnceEvery(32),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(64),
            RandomOffsetPlacement.OfTriangle(6, 2), onlyAir);
        PlacementUtils.Register(context, FlowerPlains, flowerPlain, NoiseThresholdCountPlacement.Of(-0.8, 15, 4),
            RarityFilter.OnAverageOnceEvery(32), InSquarePlacement.Spread(), PlacementUtils.Heightmap,
            BiomeFilter.Biome(), CountPlacement.Of(64), RandomOffsetPlacement.OfTriangle(6, 2), onlyAir);
        PlacementUtils.Register(context, FlowerCherry, flowerCherry, NoiseThresholdCountPlacement.Of(-0.8, 5, 10),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(6, 2), onlyAir);
        PlacementUtils.Register(context, FlowerMeadow, flowerMeadow, InSquarePlacement.Spread(),
            PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(6, 2), onlyAir);
        PlacementUtils.Register(context, FlowerPaleGarden, flowerPaleGarden, RarityFilter.OnAverageOnceEvery(32),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome());
        PlacementUtils.Register(context, WildflowersBirchForest, wildflower, CountPlacement.Of(3),
            RarityFilter.OnAverageOnceEvery(2), InSquarePlacement.Spread(), PlacementUtils.Heightmap,
            BiomeFilter.Biome(), CountPlacement.Of(64), RandomOffsetPlacement.OfTriangle(6, 2), onlyAir);
        PlacementUtils.Register(context, WildflowersMeadow, wildflower, NoiseThresholdCountPlacement.Of(-0.8, 5, 10),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome(), CountPlacement.Of(8),
            RandomOffsetPlacement.OfTriangle(6, 2), onlyAir);
        PlacementUtils.Register(context, TreesPlains, treesPlains, PlacementUtils.CountExtra(0, 0.05f, 1),
            InSquarePlacement.Spread(), TreeThreshold, PlacementUtils.HeightmapOceanFloor,
            BlockPredicateFilter.ForPredicate(BlockPredicate.WouldSurvive(
                WorldGenDataSupport.State("oak_sapling"), Vec3i.Zero)), BiomeFilter.Biome());
        PlacementUtils.Register(context, DarkForestVegetation, darkForestVegetation, CountPlacement.Of(16),
            InSquarePlacement.Spread(), TreeThreshold, PlacementUtils.HeightmapOceanFloor, BiomeFilter.Biome());
        PlacementUtils.Register(context, PaleGardenVegetation, paleGardenVegetation, CountPlacement.Of(16),
            InSquarePlacement.Spread(), TreeThreshold, PlacementUtils.HeightmapOceanFloor, BiomeFilter.Biome());
        PlacementUtils.Register(context, FlowerForestFlowers, forestFlowers, RarityFilter.OnAverageOnceEvery(7),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap,
            CountPlacement.Of(ClampedInt.Of(UniformInt.Of(-1, 3), 0, 3)), BiomeFilter.Biome());
        PlacementUtils.Register(context, ForestFlowers, forestFlowers, RarityFilter.OnAverageOnceEvery(7),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap,
            CountPlacement.Of(ClampedInt.Of(UniformInt.Of(-3, 1), 0, 1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, PaleGardenFlowers, paleForestFlower, RarityFilter.OnAverageOnceEvery(8),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapNoLeaves, BiomeFilter.Biome(), CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3), onlyAir);
        PlacementUtils.Register(context, PaleMossPatch, paleMossPatch, CountPlacement.Of(1),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapNoLeaves, BiomeFilter.Biome());
        PlacementUtils.Register(context, TreesFlowerForest, treesFlowerForest,
            TreePlacement(PlacementUtils.CountExtra(6, 0.1f, 1)));
        PlacementUtils.Register(context, TreesMeadow, meadowTrees,
            TreePlacement(RarityFilter.OnAverageOnceEvery(100)));
        PlacementUtils.Register(context, TreesCherry, cherryBees005,
            TreePlacement(PlacementUtils.CountExtra(10, 0.1f, 1), "cherry_sapling"));
        PlacementUtils.Register(context, TreesTaiga, treesTaiga,
            TreePlacement(PlacementUtils.CountExtra(10, 0.1f, 1)));
        PlacementUtils.Register(context, TreesGrove, treesGrove,
            TreePlacement(PlacementUtils.CountExtra(10, 0.1f, 1)));
        PlacementUtils.Register(context, TreesBadlands, treesBadlands,
            TreePlacement(PlacementUtils.CountExtra(5, 0.1f, 1), "oak_sapling"));
        PlacementUtils.Register(context, TreesSnowy, treesSnowy,
            TreePlacement(PlacementUtils.CountExtra(0, 0.1f, 1), "spruce_sapling"));
        PlacementUtils.Register(context, TreesSwamp, swampOak, PlacementUtils.CountExtra(2, 0.1f, 1),
            InSquarePlacement.Spread(), SurfaceWaterDepthFilter.ForMaxDepth(2), PlacementUtils.HeightmapOceanFloor,
            BiomeFilter.Biome(),
            BlockPredicateFilter.ForPredicate(BlockPredicate.WouldSurvive(
                WorldGenDataSupport.State("oak_sapling"), Vec3i.Zero)));
        PlacementUtils.Register(context, TreesWindsweptSavanna, treesSavanna,
            TreePlacement(PlacementUtils.CountExtra(2, 0.1f, 1)));
        PlacementUtils.Register(context, TreesSavanna, treesSavanna,
            TreePlacement(PlacementUtils.CountExtra(1, 0.1f, 1)));
        PlacementUtils.Register(context, BirchTall, birchTall,
            TreePlacement(PlacementUtils.CountExtra(10, 0.1f, 1)));
        PlacementUtils.Register(context, TreesBirch, treesBirch,
            TreePlacement(PlacementUtils.CountExtra(10, 0.1f, 1), "birch_sapling"));
        PlacementUtils.Register(context, TreesWindsweptForest, treesWindsweptHills,
            TreePlacement(PlacementUtils.CountExtra(3, 0.1f, 1)));
        PlacementUtils.Register(context, TreesWindsweptHills, treesWindsweptHills,
            TreePlacement(PlacementUtils.CountExtra(0, 0.1f, 1)));
        PlacementUtils.Register(context, TreesWater, treesWater,
            TreePlacement(PlacementUtils.CountExtra(0, 0.1f, 1)));
        PlacementUtils.Register(context, TreesBirchAndOakLeafLitter, treesBirchAndOakLeafLitter,
            TreePlacement(PlacementUtils.CountExtra(10, 0.1f, 1)));
        PlacementUtils.Register(context, TreesSparseJungle, treesSparseJungle,
            TreePlacement(PlacementUtils.CountExtra(2, 0.1f, 1)));
        PlacementUtils.Register(context, TreesOldGrowthSpruceTaiga, treesOldGrowthSpruceTaiga,
            TreePlacement(PlacementUtils.CountExtra(10, 0.1f, 1)));
        PlacementUtils.Register(context, TreesOldGrowthPineTaiga, treesOldGrowthPineTaiga,
            TreePlacement(PlacementUtils.CountExtra(10, 0.1f, 1)));
        PlacementUtils.Register(context, TreesJungle, treesJungle,
            TreePlacement(PlacementUtils.CountExtra(50, 0.1f, 1)));
        PlacementUtils.Register(context, BambooVegetation, bambooVegetation,
            TreePlacement(PlacementUtils.CountExtra(30, 0.1f, 1)));
        PlacementUtils.Register(context, MushroomIslandVegetation, mushroomIslandVegetation,
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome());
        PlacementUtils.Register(context, TreesMangrove, mangroveVegetation, CountPlacement.Of(25),
            InSquarePlacement.Spread(), SurfaceWaterDepthFilter.ForMaxDepth(5), PlacementUtils.HeightmapOceanFloor,
            BiomeFilter.Biome());
    }

    //WorldSurfaceSquaredWithCount count on the world surface, maps to vanilla worldSurfaceSquaredWithCount
    public static PlacementModifier[] WorldSurfaceSquaredWithCount(int count) =>
    [
        CountPlacement.Of(count), InSquarePlacement.Spread(), PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome()
    ];

    //Add appends modifiers to a base list, maps to vanilla Util.copyAndAdd
    private static PlacementModifier[] Add(PlacementModifier[] baseList, params PlacementModifier[] extra)
        => [..baseList, ..extra];

    //MushroomPlacement the mushroom patch chain with an optional prefix and rarity, maps to vanilla getMushroomPlacement
    private static PlacementModifier[] MushroomPlacement(int rarity, PlacementModifier? prefix)
    {
        var result = new List<PlacementModifier>();
        if (prefix is not null) result.Add(prefix);
        if (rarity != 0) result.Add(RarityFilter.OnAverageOnceEvery(rarity));
        result.Add(InSquarePlacement.Spread());
        result.Add(PlacementUtils.Heightmap);
        result.Add(BiomeFilter.Biome());
        result.Add(CountPlacement.Of(96));
        result.Add(RandomOffsetPlacement.OfTriangle(7, 3));
        result.Add(BlockPredicateFilter.ForPredicate(BlockPredicate.OnlyInAir));
        return result.ToArray();
    }

    //TreePlacement the tree chain using the dry-surface threshold, maps to vanilla treePlacement
    public static PlacementModifier[] TreePlacement(PlacementModifier frequency) =>
    [
        frequency, InSquarePlacement.Spread(), TreeThreshold, PlacementUtils.HeightmapOceanFloor, BiomeFilter.Biome()
    ];

    public static PlacementModifier[] TreePlacement(PlacementModifier frequency, string sapling) =>
        Add(TreePlacement(frequency), BlockPredicateFilter.ForPredicate(BlockPredicate.WouldSurvive(
            WorldGenDataSupport.State(sapling), Vec3i.Zero)));
}
