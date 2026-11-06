using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Game.World.Level.Material;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using Direction = NacreCraft.Primitives.Direction;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//MiscOverworldPlacements built-in placed overworld decorations, maps to vanilla net.minecraft.data.worldgen.placement.MiscOverworldPlacements
public static class MiscOverworldPlacements
{
    public static readonly ResourceKey<RegistryPlacedFeature> IceSpike = PlacementUtils.CreateKey("ice_spike");
    public static readonly ResourceKey<RegistryPlacedFeature> IcePatch = PlacementUtils.CreateKey("ice_patch");
    public static readonly ResourceKey<RegistryPlacedFeature> ForestRock = PlacementUtils.CreateKey("forest_rock");
    public static readonly ResourceKey<RegistryPlacedFeature> IcebergPacked =
        PlacementUtils.CreateKey("iceberg_packed");
    public static readonly ResourceKey<RegistryPlacedFeature> IcebergBlue = PlacementUtils.CreateKey("iceberg_blue");
    public static readonly ResourceKey<RegistryPlacedFeature> BlueIce = PlacementUtils.CreateKey("blue_ice");
    public static readonly ResourceKey<RegistryPlacedFeature> LakeLavaUnderground =
        PlacementUtils.CreateKey("lake_lava_underground");
    public static readonly ResourceKey<RegistryPlacedFeature> LakeLavaSurface =
        PlacementUtils.CreateKey("lake_lava_surface");
    public static readonly ResourceKey<RegistryPlacedFeature> SulfurPool = PlacementUtils.CreateKey("sulfur_pool");
    public static readonly ResourceKey<RegistryPlacedFeature> DiskClay = PlacementUtils.CreateKey("disk_clay");
    public static readonly ResourceKey<RegistryPlacedFeature> DiskGravel = PlacementUtils.CreateKey("disk_gravel");
    public static readonly ResourceKey<RegistryPlacedFeature> DiskSand = PlacementUtils.CreateKey("disk_sand");
    public static readonly ResourceKey<RegistryPlacedFeature> DiskGrass = PlacementUtils.CreateKey("disk_grass");
    public static readonly ResourceKey<RegistryPlacedFeature> FreezeTopLayer =
        PlacementUtils.CreateKey("freeze_top_layer");
    public static readonly ResourceKey<RegistryPlacedFeature> VoidStartPlatform =
        PlacementUtils.CreateKey("void_start_platform");
    public static readonly ResourceKey<RegistryPlacedFeature> DesertWell = PlacementUtils.CreateKey("desert_well");
    public static readonly ResourceKey<RegistryPlacedFeature> SpringLava = PlacementUtils.CreateKey("spring_lava");
    public static readonly ResourceKey<RegistryPlacedFeature> SpringLavaFrozen =
        PlacementUtils.CreateKey("spring_lava_frozen");
    public static readonly ResourceKey<RegistryPlacedFeature> SpringWater = PlacementUtils.CreateKey("spring_water");

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var iceSpike = features.GetOrThrow(MiscOverworldFeatures.IceSpike);
        var icePatch = features.GetOrThrow(MiscOverworldFeatures.IcePatch);
        var forestRock = features.GetOrThrow(MiscOverworldFeatures.ForestRock);
        var icebergPacked = features.GetOrThrow(MiscOverworldFeatures.IcebergPacked);
        var icebergBlue = features.GetOrThrow(MiscOverworldFeatures.IcebergBlue);
        var blueIce = features.GetOrThrow(MiscOverworldFeatures.BlueIce);
        var lakeLava = features.GetOrThrow(MiscOverworldFeatures.LakeLava);
        var sulfurPool = features.GetOrThrow(MiscOverworldFeatures.SulfurPool);
        var diskClay = features.GetOrThrow(MiscOverworldFeatures.DiskClay);
        var diskGravel = features.GetOrThrow(MiscOverworldFeatures.DiskGravel);
        var diskSand = features.GetOrThrow(MiscOverworldFeatures.DiskSand);
        var diskGrass = features.GetOrThrow(MiscOverworldFeatures.DiskGrass);
        var freezeTopLayer = features.GetOrThrow(MiscOverworldFeatures.FreezeTopLayer);
        var voidStartPlatform = features.GetOrThrow(MiscOverworldFeatures.VoidStartPlatform);
        var desertWell = features.GetOrThrow(MiscOverworldFeatures.DesertWell);
        var springLavaOverworld = features.GetOrThrow(MiscOverworldFeatures.SpringLavaOverworld);
        var springLavaFrozen = features.GetOrThrow(MiscOverworldFeatures.SpringLavaFrozen);
        var springWater = features.GetOrThrow(MiscOverworldFeatures.SpringWater);
        PlacementUtils.Register(context, IceSpike, iceSpike, CountPlacement.Of(3), InSquarePlacement.Spread(),
            PlacementUtils.Heightmap, BiomeFilter.Biome());
        PlacementUtils.Register(context, IcePatch, icePatch, CountPlacement.Of(2), InSquarePlacement.Spread(),
            PlacementUtils.Heightmap, RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)),
            BlockPredicateFilter.ForPredicate(BlockPredicate.MatchesBlocks(WorldGenDataSupport.Block("snow_block"))),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, ForestRock, forestRock, CountPlacement.Of(2), InSquarePlacement.Spread(),
            PlacementUtils.Heightmap, BiomeFilter.Biome());
        PlacementUtils.Register(context, IcebergBlue, icebergBlue, RarityFilter.OnAverageOnceEvery(200),
            InSquarePlacement.Spread(), BiomeFilter.Biome());
        PlacementUtils.Register(context, IcebergPacked, icebergPacked, RarityFilter.OnAverageOnceEvery(16),
            InSquarePlacement.Spread(), BiomeFilter.Biome());
        PlacementUtils.Register(context, BlueIce, blueIce, CountPlacement.Of(UniformInt.Of(0, 19)),
            InSquarePlacement.Spread(), HeightRangePlacement.Uniform(VerticalAnchor.Absolute(30), VerticalAnchor.Absolute(61)),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, LakeLavaUnderground, lakeLava, RarityFilter.OnAverageOnceEvery(9),
            InSquarePlacement.Spread(),
            HeightRangePlacement.Of(UniformHeight.Of(VerticalAnchor.Absolute(0), VerticalAnchor.BelowTop(0))),
            EnvironmentScanPlacement.ScanningFor(Direction.Down,
                BlockPredicate.AllOf(BlockPredicate.Not(BlockPredicate.OnlyInAir),
                    BlockPredicate.InsideWorld(new Vec3i(0, -5, 0))), 32),
            SurfaceRelativeThresholdFilter.Of(Heightmap.Types.OceanFloorWg, int.MinValue, -5), BiomeFilter.Biome());
        PlacementUtils.Register(context, LakeLavaSurface, lakeLava, RarityFilter.OnAverageOnceEvery(200),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapWorldSurface, BiomeFilter.Biome());
        PlacementUtils.Register(context, SulfurPool, sulfurPool, CountPlacement.Of(256), InSquarePlacement.Spread(),
            PlacementUtils.RangeBottomToMaxTerrainHeight,
            BlockPredicateFilter.ForPredicate(BlockPredicate.Solid()),
            EnvironmentScanPlacement.ScanningFor(Direction.Up, BlockPredicate.OnlyInAir, 32),
            RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)),
            BlockPredicateFilter.ForPredicate(BlockPredicate.MatchesBlocks(WorldGenDataSupport.Block("sulfur"))),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, DiskClay, diskClay, InSquarePlacement.Spread(),
            PlacementUtils.HeightmapTopSolid, BlockPredicateFilter.ForPredicate(BlockPredicate.MatchesFluids(Fluids.Water)),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, DiskGravel, diskGravel, InSquarePlacement.Spread(),
            PlacementUtils.HeightmapTopSolid, BlockPredicateFilter.ForPredicate(BlockPredicate.MatchesFluids(Fluids.Water)),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, DiskSand, diskSand, CountPlacement.Of(3), InSquarePlacement.Spread(),
            PlacementUtils.HeightmapTopSolid, BlockPredicateFilter.ForPredicate(BlockPredicate.MatchesFluids(Fluids.Water)),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, DiskGrass, diskGrass, CountPlacement.Of(1), InSquarePlacement.Spread(),
            PlacementUtils.HeightmapTopSolid, RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)),
            BlockPredicateFilter.ForPredicate(BlockPredicate.MatchesBlocks(WorldGenDataSupport.Block("mud"))),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, FreezeTopLayer, freezeTopLayer, BiomeFilter.Biome());
        PlacementUtils.Register(context, VoidStartPlatform, voidStartPlatform, BiomeFilter.Biome());
        PlacementUtils.Register(context, DesertWell, desertWell, RarityFilter.OnAverageOnceEvery(1000),
            InSquarePlacement.Spread(), PlacementUtils.Heightmap, BiomeFilter.Biome());
        var veryBiased = HeightRangePlacement.Of(
            VeryBiasedToBottomHeight.Of(VerticalAnchor.AboveBottom(0), VerticalAnchor.BelowTop(8), 8));
        PlacementUtils.Register(context, SpringLava, springLavaOverworld, CountPlacement.Of(20),
            InSquarePlacement.Spread(), veryBiased, BiomeFilter.Biome());
        PlacementUtils.Register(context, SpringLavaFrozen, springLavaFrozen, CountPlacement.Of(20),
            InSquarePlacement.Spread(), veryBiased, BiomeFilter.Biome());
        PlacementUtils.Register(context, SpringWater, springWater, CountPlacement.Of(25),
            InSquarePlacement.Spread(),
            HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(192)),
            BiomeFilter.Biome());
    }
}
