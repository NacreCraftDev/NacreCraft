using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using Direction = NacreCraft.Primitives.Direction;
using GameIntProvider = NacreCraft.Game.World.Level.LevelGen.IntProvider;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//CavePlacements built-in placed cave features, maps to vanilla net.minecraft.data.worldgen.placement.CavePlacements
//The glow lichen lower bound is the vanilla 104 (the decompiler shows it through the demo-hint constant)
public static class CavePlacements
{
    private const int GlowLichenMinCount = 104;

    public static readonly ResourceKey<RegistryPlacedFeature> MonsterRoom =
        PlacementUtils.CreateKey("monster_room");
    public static readonly ResourceKey<RegistryPlacedFeature> MonsterRoomDeep =
        PlacementUtils.CreateKey("monster_room_deep");
    public static readonly ResourceKey<RegistryPlacedFeature> FossilUpper =
        PlacementUtils.CreateKey("fossil_upper");
    public static readonly ResourceKey<RegistryPlacedFeature> FossilLower =
        PlacementUtils.CreateKey("fossil_lower");
    public static readonly ResourceKey<RegistryPlacedFeature> DripstoneCluster =
        PlacementUtils.CreateKey("dripstone_cluster");
    public static readonly ResourceKey<RegistryPlacedFeature> SulfurSpikeCluster =
        PlacementUtils.CreateKey("sulfur_spike_cluster");
    public static readonly ResourceKey<RegistryPlacedFeature> LargeDripstone =
        PlacementUtils.CreateKey("large_dripstone");
    public static readonly ResourceKey<RegistryPlacedFeature> PointedDripstone =
        PlacementUtils.CreateKey("pointed_dripstone");
    public static readonly ResourceKey<RegistryPlacedFeature> SulfurSpike =
        PlacementUtils.CreateKey("sulfur_spike");
    public static readonly ResourceKey<RegistryPlacedFeature> UnderwaterMagma =
        PlacementUtils.CreateKey("underwater_magma");
    public static readonly ResourceKey<RegistryPlacedFeature> GlowLichen =
        PlacementUtils.CreateKey("glow_lichen");
    public static readonly ResourceKey<RegistryPlacedFeature> RootedAzaleaTree =
        PlacementUtils.CreateKey("rooted_azalea_tree");
    public static readonly ResourceKey<RegistryPlacedFeature> RootedSulfurSpring =
        PlacementUtils.CreateKey("rooted_sulfur_spring");
    public static readonly ResourceKey<RegistryPlacedFeature> CaveVines =
        PlacementUtils.CreateKey("cave_vines");
    public static readonly ResourceKey<RegistryPlacedFeature> LushCavesVegetation =
        PlacementUtils.CreateKey("lush_caves_vegetation");
    public static readonly ResourceKey<RegistryPlacedFeature> LushCavesClay =
        PlacementUtils.CreateKey("lush_caves_clay");
    public static readonly ResourceKey<RegistryPlacedFeature> LushCavesCeilingVegetation =
        PlacementUtils.CreateKey("lush_caves_ceiling_vegetation");
    public static readonly ResourceKey<RegistryPlacedFeature> SporeBlossom =
        PlacementUtils.CreateKey("spore_blossom");
    public static readonly ResourceKey<RegistryPlacedFeature> ClassicVines =
        PlacementUtils.CreateKey("classic_vines_cave_feature");
    public static readonly ResourceKey<RegistryPlacedFeature> AmethystGeode =
        PlacementUtils.CreateKey("amethyst_geode");
    public static readonly ResourceKey<RegistryPlacedFeature> SculkPatchDeepDark =
        PlacementUtils.CreateKey("sculk_patch_deep_dark");
    public static readonly ResourceKey<RegistryPlacedFeature> SculkPatchAncientCity =
        PlacementUtils.CreateKey("sculk_patch_ancient_city");
    public static readonly ResourceKey<RegistryPlacedFeature> SculkVein =
        PlacementUtils.CreateKey("sculk_vein");

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var monsterRoom = features.GetOrThrow(CaveFeatures.MonsterRoom);
        var fossilCoal = features.GetOrThrow(CaveFeatures.FossilCoal);
        var fossilDiamonds = features.GetOrThrow(CaveFeatures.FossilDiamonds);
        var dripstoneCluster = features.GetOrThrow(CaveFeatures.DripstoneCluster);
        var sulfurSpikeCluster = features.GetOrThrow(CaveFeatures.SulfurSpikeCluster);
        var largeDripstone = features.GetOrThrow(CaveFeatures.LargeDripstone);
        var pointedDripstone = features.GetOrThrow(CaveFeatures.PointedDripstone);
        var sulfurSpike = features.GetOrThrow(CaveFeatures.SulfurSpike);
        var underwaterMagma = features.GetOrThrow(CaveFeatures.UnderwaterMagma);
        var glowLichen = features.GetOrThrow(CaveFeatures.GlowLichen);
        var rootedAzaleaTree = features.GetOrThrow(CaveFeatures.RootedAzaleaTree);
        var rootedSulfurSpring = features.GetOrThrow(CaveFeatures.RootedSulfurSpring);
        var caveVine = features.GetOrThrow(CaveFeatures.CaveVine);
        var mossPatch = features.GetOrThrow(CaveFeatures.MossPatch);
        var lushCavesClay = features.GetOrThrow(CaveFeatures.LushCavesClay);
        var mossPatchCeiling = features.GetOrThrow(CaveFeatures.MossPatchCeiling);
        var sporeBlossom = features.GetOrThrow(CaveFeatures.SporeBlossom);
        var vines = features.GetOrThrow(VegetationFeatures.Vines);
        var amethystGeode = features.GetOrThrow(CaveFeatures.AmethystGeode);
        var sculkPatchDeepDark = features.GetOrThrow(CaveFeatures.SculkPatchDeepDark);
        var sculkPatchAncientCity = features.GetOrThrow(CaveFeatures.SculkPatchAncientCity);
        var sculkVein = features.GetOrThrow(CaveFeatures.SculkVein);
        PlacementUtils.Register(context, MonsterRoom, monsterRoom, CountPlacement.Of(10), InSquarePlacement.Spread(),
            HeightRangePlacement.Uniform(VerticalAnchor.Absolute(0), VerticalAnchor.BelowTop(0)), BiomeFilter.Biome());
        PlacementUtils.Register(context, MonsterRoomDeep, monsterRoom, CountPlacement.Of(4),
            InSquarePlacement.Spread(),
            HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(6), VerticalAnchor.Absolute(-1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, FossilUpper, fossilCoal, RarityFilter.OnAverageOnceEvery(64),
            InSquarePlacement.Spread(),
            HeightRangePlacement.Uniform(VerticalAnchor.Absolute(0), VerticalAnchor.BelowTop(0)), BiomeFilter.Biome());
        PlacementUtils.Register(context, FossilLower, fossilDiamonds, RarityFilter.OnAverageOnceEvery(64),
            InSquarePlacement.Spread(),
            HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(-8)), BiomeFilter.Biome());
        PlacementUtils.Register(context, DripstoneCluster, dripstoneCluster, CountPlacement.Of(UniformInt.Of(48, 96)),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight, BiomeFilter.Biome());
        PlacementUtils.Register(context, SulfurSpikeCluster, sulfurSpikeCluster,
            CountPlacement.Of(UniformInt.Of(48, 96)), InSquarePlacement.Spread(),
            PlacementUtils.RangeBottomToMaxTerrainHeight, BiomeFilter.Biome());
        PlacementUtils.Register(context, LargeDripstone, largeDripstone, CountPlacement.Of(UniformInt.Of(10, 48)),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight, BiomeFilter.Biome());
        var pointedOffset = RandomOffsetPlacement.Of(
            ClampedNormalInt.Of(0.0f, 3.0f, -10, 10), ClampedNormalInt.Of(0.0f, 0.6f, -2, 2));
        PlacementUtils.Register(context, PointedDripstone, pointedDripstone,
            CountPlacement.Of(UniformInt.Of(192, 256)), InSquarePlacement.Spread(),
            PlacementUtils.RangeBottomToMaxTerrainHeight, CountPlacement.Of(UniformInt.Of(1, 5)), pointedOffset,
            BiomeFilter.Biome());
        PlacementUtils.Register(context, SulfurSpike, sulfurSpike, CountPlacement.Of(UniformInt.Of(192, 256)),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight,
            CountPlacement.Of(UniformInt.Of(1, 5)), pointedOffset, BiomeFilter.Biome());
        PlacementUtils.Register(context, UnderwaterMagma, underwaterMagma, CountPlacement.Of(UniformInt.Of(44, 52)),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight,
            SurfaceRelativeThresholdFilter.Of(Heightmap.Types.OceanFloorWg, int.MinValue, -2), BiomeFilter.Biome());
        PlacementUtils.Register(context, GlowLichen, glowLichen,
            CountPlacement.Of(UniformInt.Of(GlowLichenMinCount, 157)),
            PlacementUtils.RangeBottomToMaxTerrainHeight, InSquarePlacement.Spread(),
            SurfaceRelativeThresholdFilter.Of(Heightmap.Types.OceanFloorWg, int.MinValue, -13), BiomeFilter.Biome());
        PlacementUtils.Register(context, RootedAzaleaTree, rootedAzaleaTree, CountPlacement.Of(UniformInt.Of(1, 2)),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight,
            EnvironmentScanPlacement.ScanningFor(Direction.Up, BlockPredicate.Solid(),
                BlockPredicate.OnlyInAir, 12),
            RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, RootedSulfurSpring, rootedSulfurSpring,
            CountPlacement.Of(UniformInt.Of(1, 2)), InSquarePlacement.Spread(),
            PlacementUtils.RangeBottomToMaxTerrainHeight,
            EnvironmentScanPlacement.ScanningFor(Direction.Up, BlockPredicate.Solid(),
                BlockPredicate.OnlyInAir, 12),
            RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, CaveVines, caveVine, CountPlacement.Of(188), InSquarePlacement.Spread(),
            PlacementUtils.RangeBottomToMaxTerrainHeight,
            EnvironmentScanPlacement.ScanningFor(Direction.Up, BlockPredicate.HasSturdyFace(Direction.Down),
                BlockPredicate.OnlyInAir, 12),
            RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, LushCavesVegetation, mossPatch, CountPlacement.Of(125),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight,
            EnvironmentScanPlacement.ScanningFor(Direction.Down, BlockPredicate.Solid(),
                BlockPredicate.OnlyInAir, 12),
            RandomOffsetPlacement.Vertical(ConstantInt.Of(1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, LushCavesClay, lushCavesClay, CountPlacement.Of(62),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight,
            EnvironmentScanPlacement.ScanningFor(Direction.Down, BlockPredicate.Solid(),
                BlockPredicate.OnlyInAir, 12),
            RandomOffsetPlacement.Vertical(ConstantInt.Of(1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, LushCavesCeilingVegetation, mossPatchCeiling, CountPlacement.Of(125),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight,
            EnvironmentScanPlacement.ScanningFor(Direction.Up, BlockPredicate.Solid(),
                BlockPredicate.OnlyInAir, 12),
            RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, SporeBlossom, sporeBlossom, CountPlacement.Of(25),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight,
            EnvironmentScanPlacement.ScanningFor(Direction.Up, BlockPredicate.Solid(),
                BlockPredicate.OnlyInAir, 12),
            RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)), BiomeFilter.Biome());
        PlacementUtils.Register(context, ClassicVines, vines, CountPlacement.Of(256), InSquarePlacement.Spread(),
            PlacementUtils.RangeBottomToMaxTerrainHeight, BiomeFilter.Biome());
        PlacementUtils.Register(context, AmethystGeode, amethystGeode, RarityFilter.OnAverageOnceEvery(24),
            InSquarePlacement.Spread(),
            HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(6), VerticalAnchor.Absolute(30)), BiomeFilter.Biome());
        PlacementUtils.Register(context, SculkPatchDeepDark, sculkPatchDeepDark,
            CountPlacement.Of(ConstantInt.Of(256)), InSquarePlacement.Spread(),
            PlacementUtils.RangeBottomToMaxTerrainHeight, BiomeFilter.Biome());
        PlacementUtils.Register(context, SculkPatchAncientCity, sculkPatchAncientCity, Array.Empty<PlacementModifier>());
        PlacementUtils.Register(context, SculkVein, sculkVein, CountPlacement.Of(UniformInt.Of(204, 250)),
            InSquarePlacement.Spread(), PlacementUtils.RangeBottomToMaxTerrainHeight, BiomeFilter.Biome());
    }
}
