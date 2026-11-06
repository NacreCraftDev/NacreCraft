using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Misc;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Nether;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Vegetation;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Game.World.Level.Material;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Util.Random;
using Direction = NacreCraft.Primitives.Direction;
using RegBlock = NacreCraft.Registry.Block;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//MiscOverworldFeatures built-in overworld decorations, maps to vanilla net.minecraft.data.worldgen.features.MiscOverworldFeatures
//The sulfur pool and spring entries reference the 26.2 potent sulfur block, which is not in the block table yet, so its state resolves by name
public static class MiscOverworldFeatures
{
    public static readonly ResourceKey<RegistryConfiguredFeature> IceSpike = FeatureUtils.CreateKey("ice_spike");
    public static readonly ResourceKey<RegistryConfiguredFeature> IcePatch = FeatureUtils.CreateKey("ice_patch");
    public static readonly ResourceKey<RegistryConfiguredFeature> ForestRock = FeatureUtils.CreateKey("forest_rock");
    public static readonly ResourceKey<RegistryConfiguredFeature> IcebergPacked =
        FeatureUtils.CreateKey("iceberg_packed");
    public static readonly ResourceKey<RegistryConfiguredFeature> IcebergBlue =
        FeatureUtils.CreateKey("iceberg_blue");
    public static readonly ResourceKey<RegistryConfiguredFeature> BlueIce = FeatureUtils.CreateKey("blue_ice");
    public static readonly ResourceKey<RegistryConfiguredFeature> LakeLava = FeatureUtils.CreateKey("lake_lava");
    public static readonly ResourceKey<RegistryConfiguredFeature> SulfurPool =
        FeatureUtils.CreateKey("sulfur_pool");
    public static readonly ResourceKey<RegistryConfiguredFeature> SulfurSpring =
        FeatureUtils.CreateKey("sulfur_spring");
    public static readonly ResourceKey<RegistryConfiguredFeature> DiskClay = FeatureUtils.CreateKey("disk_clay");
    public static readonly ResourceKey<RegistryConfiguredFeature> DiskGravel = FeatureUtils.CreateKey("disk_gravel");
    public static readonly ResourceKey<RegistryConfiguredFeature> DiskSand = FeatureUtils.CreateKey("disk_sand");
    public static readonly ResourceKey<RegistryConfiguredFeature> FreezeTopLayer =
        FeatureUtils.CreateKey("freeze_top_layer");
    public static readonly ResourceKey<RegistryConfiguredFeature> DiskGrass = FeatureUtils.CreateKey("disk_grass");
    public static readonly ResourceKey<RegistryConfiguredFeature> BonusChest =
        FeatureUtils.CreateKey("bonus_chest");
    public static readonly ResourceKey<RegistryConfiguredFeature> VoidStartPlatform =
        FeatureUtils.CreateKey("void_start_platform");
    public static readonly ResourceKey<RegistryConfiguredFeature> DesertWell =
        FeatureUtils.CreateKey("desert_well");
    public static readonly ResourceKey<RegistryConfiguredFeature> SpringLavaOverworld =
        FeatureUtils.CreateKey("spring_lava_overworld");
    public static readonly ResourceKey<RegistryConfiguredFeature> SpringLavaFrozen =
        FeatureUtils.CreateKey("spring_lava_frozen");
    public static readonly ResourceKey<RegistryConfiguredFeature> SpringWater =
        FeatureUtils.CreateKey("spring_water");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        FeatureUtils.Register(context, IceSpike, SpikeFeature.Instance, new SpikeConfiguration(
            WorldGenDataSupport.State("packed_ice"), BlockPredicate.MatchesBlocks(WorldGenDataSupport.Block("snow_block")),
            BlockPredicate.MatchesTag(BlockTags.IceSpikeReplaceable)));
        FeatureUtils.Register(context, IcePatch, DiskFeature.Instance, new DiskConfiguration(
            BlockStateProvider.Simple(WorldGenDataSupport.Block("packed_ice")),
            BlockPredicate.MatchesBlocks(Blocks("dirt", "grass_block", "podzol", "coarse_dirt", "mycelium",
                "snow_block", "ice")),
            UniformInt.Of(2, 3), 1));
        FeatureUtils.Register(context, ForestRock, BlockBlobFeature.Instance, new BlockBlobConfiguration(
            WorldGenDataSupport.State("mossy_cobblestone"),
            BlockPredicate.MatchesTag(BlockTags.ForestRockCanPlaceOn)));
        FeatureUtils.Register(context, IcebergPacked, IcebergFeature.Instance,
            new BlockStateConfiguration(WorldGenDataSupport.State("packed_ice")));
        FeatureUtils.Register(context, IcebergBlue, IcebergFeature.Instance,
            new BlockStateConfiguration(WorldGenDataSupport.State("blue_ice")));
        FeatureUtils.Register(context, BlueIce, BlueIceFeature.Instance);
        FeatureUtils.Register(context, LakeLava, LakeFeature.Instance, new LakeConfiguration(
            BlockStateProvider.Simple(WorldGenDataSupport.Block("lava")),
            BlockStateProvider.Simple(WorldGenDataSupport.Block("stone")),
            BlockPredicate.AlwaysTrue(),
            BlockPredicate.Not(BlockPredicate.MatchesTag(BlockTags.FeaturesCannotReplace)),
            BlockPredicate.Not(BlockPredicate.MatchesTag(BlockTags.LavaPoolStoneCannotReplace))));
        //Sulfur pool: a water lake in a sulfur shell with one potent sulfur block placed above the water
        //PotentSulfurBlock.STATE is not registered yet, so the default state stands in for WET
        var sulfurPoolLake = PlacementUtils.InlinePlaced(LakeFeature.Instance, new LakeConfiguration(
            BlockStateProvider.Simple(WorldGenDataSupport.Block("water")),
            BlockStateProvider.Simple(WorldGenDataSupport.Block("sulfur")),
            BlockPredicate.Not(BlockPredicate.MatchesBlocks(WorldGenDataSupport.Block("sulfur_spike"))),
            BlockPredicate.Not(BlockPredicate.MatchesTag(BlockTags.FeaturesCannotReplace)),
            BlockPredicate.Not(BlockPredicate.MatchesTag(BlockTags.LavaPoolStoneCannotReplace))));
        var sulfurPoolBlock = PlacementUtils.InlinePlaced(SimpleBlockFeature.Instance,
            new SimpleBlockConfiguration(BlockStateProvider.Simple(WorldGenDataSupport.Block("potent_sulfur")), false),
            EnvironmentScanPlacement.ScanningFor(Direction.Down,
                BlockPredicate.AllOf(BlockPredicate.Solid(), BlockPredicate.MatchesFluids(new Vec3i(0, 1, 0), Fluids.Water)),
                4));
        FeatureUtils.Register(context, SulfurPool, SequenceFeature.Instance,
            new CompositeFeatureConfiguration(new DirectHolderSet<RegistryPlacedFeature>(new[] { sulfurPoolLake, sulfurPoolBlock })));
        FeatureUtils.Register(context, SulfurSpring, WeightedRandomSelectorFeature.Instance,
            new WeightedRandomFeatureConfiguration(WeightedList<Holder<RegistryPlacedFeature>>.Builder()
                .Add(SulfurSpringSequence(64, 7, "spring/sulfur_spring_small_1", "spring/sulfur_spring_small_2",
                    "spring/sulfur_spring_small_3", "spring/sulfur_spring_small_4"), 200)
                .Add(SulfurSpringSequence(80, 8, "spring/sulfur_spring_medium_1", "spring/sulfur_spring_medium_2",
                    "spring/sulfur_spring_medium_3"), 90)
                .Add(SulfurSpringSequence(96, 9, "spring/sulfur_spring_large_1", "spring/sulfur_spring_large_2"), 20)
                .Add(SulfurSpringSequence(128, 10, "spring/sulfur_spring_extra_large_1"), 5)
                .Build()));
        FeatureUtils.Register(context, DiskClay, DiskFeature.Instance, new DiskConfiguration(
            BlockStateProvider.Simple(WorldGenDataSupport.Block("clay")),
            BlockPredicate.MatchesBlocks(Blocks("dirt", "clay")), UniformInt.Of(2, 3), 1));
        FeatureUtils.Register(context, DiskGravel, DiskFeature.Instance, new DiskConfiguration(
            BlockStateProvider.Simple(WorldGenDataSupport.Block("gravel")),
            BlockPredicate.MatchesBlocks(Blocks("dirt", "grass_block")), UniformInt.Of(2, 5), 2));
        FeatureUtils.Register(context, DiskSand, DiskFeature.Instance, new DiskConfiguration(
            new RuleBasedStateProvider(BlockStateProvider.Simple(WorldGenDataSupport.Block("sand")),
                new[]
                {
                    new RuleBasedStateRule(
                        BlockPredicate.MatchesBlocks(new Vec3i(0, -1, 0), WorldGenDataSupport.Block("air")),
                        BlockStateProvider.Simple(WorldGenDataSupport.Block("sandstone")))
                }),
            BlockPredicate.MatchesBlocks(Blocks("dirt", "grass_block")), UniformInt.Of(2, 6), 2));
        FeatureUtils.Register(context, FreezeTopLayer, SnowAndFreezeFeature.Instance);
        FeatureUtils.Register(context, DiskGrass, DiskFeature.Instance, new DiskConfiguration(
            new RuleBasedStateProvider(BlockStateProvider.Simple(WorldGenDataSupport.Block("dirt")),
                new[]
                {
                    new RuleBasedStateRule(
                        BlockPredicate.Not(BlockPredicate.AnyOf(
                            BlockPredicate.Solid(new Vec3i(0, 1, 0)),
                            BlockPredicate.MatchesFluids(new Vec3i(0, 1, 0), Fluids.Water))),
                        BlockStateProvider.Simple(WorldGenDataSupport.Block("grass_block")))
                }),
            BlockPredicate.MatchesBlocks(Blocks("dirt", "mud")), UniformInt.Of(2, 6), 2));
        FeatureUtils.Register(context, BonusChest, BonusChestFeature.Instance);
        FeatureUtils.Register(context, VoidStartPlatform, VoidStartPlatformFeature.Instance);
        FeatureUtils.Register(context, DesertWell, DesertWellFeature.Instance);
        var lava = WorldGenDataSupport.State("lava");
        FeatureUtils.Register(context, SpringLavaOverworld, SpringFeature.Instance,
            new SpringConfiguration(lava, true, 4, 1, DirectBlocks("stone", "granite", "diorite", "andesite",
                "deepslate", "tuff", "calcite", "dirt")));
        FeatureUtils.Register(context, SpringLavaFrozen, SpringFeature.Instance,
            new SpringConfiguration(lava, true, 4, 1, DirectBlocks("snow_block", "powder_snow", "packed_ice")));
        FeatureUtils.Register(context, SpringWater, SpringFeature.Instance,
            new SpringConfiguration(WorldGenDataSupport.State("water"), true, 4, 1,
                DirectBlocks("stone", "granite", "diorite", "andesite", "deepslate", "tuff", "calcite", "dirt",
                    "snow_block", "powder_snow", "packed_ice")));
    }

    //SulfurSpringSequence a tuff cover plus one weighted template spring, maps to the sequence form in vanilla bootstrap
    private static Holder<RegistryPlacedFeature> SulfurSpringSequence(int count, int spread, params string[] templates)
    {
        var tuffCover = PlacementUtils.InlinePlaced(SimpleBlockFeature.Instance,
            new SimpleBlockConfiguration(BlockStateProvider.Simple(WorldGenDataSupport.Block("tuff")), false),
            CountPlacement.Of(count), RandomOffsetPlacement.OfTriangle(spread, 3),
            EnvironmentScanPlacement.ScanningFor(Direction.Down, BlockPredicate.Solid(), 4),
            BlockPredicateFilter.ForPredicate(BlockPredicate.Solid()));
        var entries = WeightedList<TemplateEntry>.Builder();
        foreach (var template in templates) entries.Add(TemplateEntry.Of(Identifier.WithDefaultNamespace(template)));
        var springs = PlacementUtils.InlinePlaced(TemplateFeature.Instance,
            new TemplateFeatureConfiguration(entries.Build()), RandomOffsetPlacement.Vertical(ConstantInt.Of(-7)));
        return PlacementUtils.InlinePlaced(SequenceFeature.Instance,
            new CompositeFeatureConfiguration(new DirectHolderSet<RegistryPlacedFeature>(new[] { tuffCover, springs })));
    }

    private static RegBlock[] Blocks(params string[] paths)
        => paths.Select(WorldGenDataSupport.Block).ToArray();

    private static HolderSet<RegBlock> DirectBlocks(params string[] paths)
        => new DirectHolderSet<RegBlock>(
            paths.Select(path => BuiltInRegistries.BLOCK.WrapAsHolder(WorldGenDataSupport.Block(path))).ToList());
}
