using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Misc;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Vegetation;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Util.Random;
using Direction = NacreCraft.Primitives.Direction;
using GameIntProvider = NacreCraft.Game.World.Level.LevelGen.IntProvider;
using RegBlock = NacreCraft.Registry.Block;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;
using RegistryProcessorList = NacreCraft.Registry.StructureProcessorList;

namespace NacreCraft.Game.Data.WorldGen.Features;

//CaveFeatures built-in cave features, maps to vanilla net.minecraft.data.worldgen.features.CaveFeatures
//The cave vine age/berries properties are local instances matched by name; blocks resolve by registry name
public static class CaveFeatures
{
    private static readonly IntegerProperty CaveVineAge = new("age", 0, 25);
    private static readonly BooleanProperty CaveVineBerries = new("berries");

    public static readonly ResourceKey<RegistryConfiguredFeature> MonsterRoom = FeatureUtils.CreateKey("monster_room");
    public static readonly ResourceKey<RegistryConfiguredFeature> FossilCoal = FeatureUtils.CreateKey("fossil_coal");
    public static readonly ResourceKey<RegistryConfiguredFeature> FossilDiamonds =
        FeatureUtils.CreateKey("fossil_diamonds");
    public static readonly ResourceKey<RegistryConfiguredFeature> DripstoneCluster =
        FeatureUtils.CreateKey("dripstone_cluster");
    public static readonly ResourceKey<RegistryConfiguredFeature> SulfurSpikeCluster =
        FeatureUtils.CreateKey("sulfur_spike_cluster");
    public static readonly ResourceKey<RegistryConfiguredFeature> LargeDripstone =
        FeatureUtils.CreateKey("large_dripstone");
    public static readonly ResourceKey<RegistryConfiguredFeature> PointedDripstone =
        FeatureUtils.CreateKey("pointed_dripstone");
    public static readonly ResourceKey<RegistryConfiguredFeature> SulfurSpike = FeatureUtils.CreateKey("sulfur_spike");
    public static readonly ResourceKey<RegistryConfiguredFeature> UnderwaterMagma =
        FeatureUtils.CreateKey("underwater_magma");
    public static readonly ResourceKey<RegistryConfiguredFeature> GlowLichen = FeatureUtils.CreateKey("glow_lichen");
    public static readonly ResourceKey<RegistryConfiguredFeature> RootedAzaleaTree =
        FeatureUtils.CreateKey("rooted_azalea_tree");
    public static readonly ResourceKey<RegistryConfiguredFeature> RootedSulfurSpring =
        FeatureUtils.CreateKey("rooted_sulfur_spring");
    public static readonly ResourceKey<RegistryConfiguredFeature> CaveVine = FeatureUtils.CreateKey("cave_vine");
    public static readonly ResourceKey<RegistryConfiguredFeature> CaveVineInMoss =
        FeatureUtils.CreateKey("cave_vine_in_moss");
    public static readonly ResourceKey<RegistryConfiguredFeature> MossVegetation =
        FeatureUtils.CreateKey("moss_vegetation");
    public static readonly ResourceKey<RegistryConfiguredFeature> MossPatch = FeatureUtils.CreateKey("moss_patch");
    public static readonly ResourceKey<RegistryConfiguredFeature> MossPatchBonemeal =
        FeatureUtils.CreateKey("moss_patch_bonemeal");
    public static readonly ResourceKey<RegistryConfiguredFeature> Dripleaf = FeatureUtils.CreateKey("dripleaf");
    public static readonly ResourceKey<RegistryConfiguredFeature> ClayWithDripleaves =
        FeatureUtils.CreateKey("clay_with_dripleaves");
    public static readonly ResourceKey<RegistryConfiguredFeature> ClayPoolWithDripleaves =
        FeatureUtils.CreateKey("clay_pool_with_dripleaves");
    public static readonly ResourceKey<RegistryConfiguredFeature> LushCavesClay =
        FeatureUtils.CreateKey("lush_caves_clay");
    public static readonly ResourceKey<RegistryConfiguredFeature> MossPatchCeiling =
        FeatureUtils.CreateKey("moss_patch_ceiling");
    public static readonly ResourceKey<RegistryConfiguredFeature> SporeBlossom =
        FeatureUtils.CreateKey("spore_blossom");
    public static readonly ResourceKey<RegistryConfiguredFeature> AmethystGeode =
        FeatureUtils.CreateKey("amethyst_geode");
    public static readonly ResourceKey<RegistryConfiguredFeature> SculkPatchDeepDark =
        FeatureUtils.CreateKey("sculk_patch_deep_dark");
    public static readonly ResourceKey<RegistryConfiguredFeature> SculkPatchAncientCity =
        FeatureUtils.CreateKey("sculk_patch_ancient_city");
    public static readonly ResourceKey<RegistryConfiguredFeature> SculkVein = FeatureUtils.CreateKey("sculk_vein");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var processors = context.LookupOrThrow(Registries.PROCESSOR_LIST);
        var fossilRot = processors.GetOrThrow(ProcessorLists.FossilRot);
        var fossilCoal = processors.GetOrThrow(ProcessorLists.FossilCoal);
        var fossilDiamonds = processors.GetOrThrow(ProcessorLists.FossilDiamonds);
        var blockTag = (Func<TagKey<RegBlock>, HolderSet<RegBlock>>)BuiltInRegistries.BLOCK.GetOrCreate;
        FeatureUtils.Register(context, MonsterRoom, MonsterRoomFeature.Instance);
        var fossilStructures = Ids("fossil/spine_1", "fossil/spine_2", "fossil/spine_3", "fossil/spine_4",
            "fossil/skull_1", "fossil/skull_2", "fossil/skull_3", "fossil/skull_4");
        var fossilCoalStructures = Ids("fossil/spine_1_coal", "fossil/spine_2_coal", "fossil/spine_3_coal",
            "fossil/spine_4_coal", "fossil/skull_1_coal", "fossil/skull_2_coal", "fossil/skull_3_coal",
            "fossil/skull_4_coal");
        FeatureUtils.Register(context, FossilCoal, FossilFeature.Instance,
            new FossilFeatureConfiguration(fossilStructures, fossilCoalStructures, fossilRot, fossilCoal, 4));
        FeatureUtils.Register(context, FossilDiamonds, FossilFeature.Instance,
            new FossilFeatureConfiguration(fossilStructures, fossilCoalStructures, fossilRot, fossilDiamonds, 4));
        FeatureUtils.Register(context, DripstoneCluster, SpeleothemClusterFeature.Instance,
            new SpeleothemClusterConfiguration(S("dripstone_block"), S("pointed_dripstone"),
                blockTag(BlockTags.DripstoneReplaceable), 12, UniformInt.Of(3, 6), UniformInt.Of(2, 8), 1, 3,
                UniformInt.Of(2, 4), UniformFloat.Of(0.3f, 0.7f), NacreCraft.Game.World.Level.LevelGen.ClampedNormalFloat.Of(0.1f, 0.3f, 0.1f, 0.9f),
                0.1f, 3, 8));
        FeatureUtils.Register(context, LargeDripstone, LargeDripstoneFeature.Instance,
            new LargeDripstoneConfiguration(blockTag(BlockTags.DripstoneReplaceable), 30,
                ClampedInt.Of(UniformInt.Of(3, 19), 3, 16), UniformFloat.Of(0.4f, 2.0f), 0.33f,
                UniformFloat.Of(0.3f, 0.9f), UniformFloat.Of(0.4f, 1.0f), UniformFloat.Of(0.0f, 0.3f), 4, 0.6f));
        FeatureUtils.Register(context, PointedDripstone, SimpleRandomSelectorFeature.Instance,
            new CompositeFeatureConfiguration(new DirectHolderSet<RegistryPlacedFeature>(new[]
            {
                PlacementUtils.InlinePlaced(SpeleothemFeature.Instance,
                    new SpeleothemConfiguration(S("dripstone_block"), S("pointed_dripstone"),
                        blockTag(BlockTags.DripstoneReplaceable), 0.2f, 0.7f, 0.5f, 0.5f),
                    EnvironmentScanPlacement.ScanningFor(Direction.Down, BlockPredicate.Solid(),
                        BlockPredicate.OnlyInAirOrWater, 12),
                    RandomOffsetPlacement.Vertical(ConstantInt.Of(1))),
                PlacementUtils.InlinePlaced(SpeleothemFeature.Instance,
                    new SpeleothemConfiguration(S("dripstone_block"), S("pointed_dripstone"),
                        blockTag(BlockTags.DripstoneReplaceable), 0.2f, 0.7f, 0.5f, 0.5f),
                    EnvironmentScanPlacement.ScanningFor(Direction.Up, BlockPredicate.Solid(),
                        BlockPredicate.OnlyInAirOrWater, 12),
                    RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)))
            })));
        FeatureUtils.Register(context, SulfurSpikeCluster, SpeleothemClusterFeature.Instance,
            new SpeleothemClusterConfiguration(S("sulfur"), S("sulfur_spike"),
                blockTag(BlockTags.SulfurSpikeReplaceable), 12, UniformInt.Of(1, 4), UniformInt.Of(2, 8), 1, 3,
                UniformInt.Of(2, 4), UniformFloat.Of(0.3f, 0.7f), ConstantFloat.Zero, 0.1f, 3, 8));
        FeatureUtils.Register(context, SulfurSpike, SimpleRandomSelectorFeature.Instance,
            new CompositeFeatureConfiguration(new DirectHolderSet<RegistryPlacedFeature>(new[]
            {
                PlacementUtils.InlinePlaced(SpeleothemFeature.Instance,
                    new SpeleothemConfiguration(S("sulfur"), S("sulfur_spike"),
                        blockTag(BlockTags.SulfurSpikeReplaceable), 0.2f, 0.7f, 0.5f, 0.5f),
                    EnvironmentScanPlacement.ScanningFor(Direction.Down, BlockPredicate.Solid(),
                        BlockPredicate.OnlyInAirOrWater, 12),
                    RandomOffsetPlacement.Vertical(ConstantInt.Of(1))),
                PlacementUtils.InlinePlaced(SpeleothemFeature.Instance,
                    new SpeleothemConfiguration(S("sulfur"), S("sulfur_spike"),
                        blockTag(BlockTags.SulfurSpikeReplaceable), 0.2f, 0.7f, 0.5f, 0.5f),
                    EnvironmentScanPlacement.ScanningFor(Direction.Up, BlockPredicate.Solid(),
                        BlockPredicate.OnlyInAirOrWater, 12),
                    RandomOffsetPlacement.Vertical(ConstantInt.Of(-1)))
            })));
        FeatureUtils.Register(context, UnderwaterMagma, UnderwaterMagmaFeature.Instance,
            new UnderwaterMagmaConfiguration(5, 1, 0.5f));
        FeatureUtils.Register(context, GlowLichen, MultifaceGrowthFeature.Instance,
            new MultifaceGrowthConfiguration(B("glow_lichen"), 20, false, true, true, 0.5f,
                DirectBlocks("stone", "andesite", "diorite", "granite", "dripstone_block", "calcite", "tuff",
                    "deepslate", "sulfur", "cinnabar")));
        FeatureUtils.Register(context, RootedAzaleaTree, RootSystemFeature.Instance,
            new RootSystemConfiguration(
                PlacementUtils.InlinePlaced(context.LookupOrThrow(Registries.CONFIGURED_FEATURE)
                    .GetOrThrow(TreeFeatures.AzaleaTree)),
                3, 0, 0, 3, blockTag(BlockTags.AzaleaRootReplaceable), Provider("rooted_dirt"), 20, 100, 3, 2,
                Provider("hanging_roots"), 20, 2,
                BlockPredicate.AllOf(BlockPredicate.AnyOf(BlockPredicate.OnlyInAir,
                    BlockPredicate.MatchesTag(BlockTags.ReplaceableByTrees)),
                    BlockPredicate.MatchesTag(new Vec3i(0, -1, 0), BlockTags.AzaleaGrowsOn))));
        FeatureUtils.Register(context, RootedSulfurSpring, RootSystemFeature.Instance,
            new RootSystemConfiguration(
                PlacementUtils.InlinePlaced(context.LookupOrThrow(Registries.CONFIGURED_FEATURE)
                    .GetOrThrow(MiscOverworldFeatures.SulfurSpring)),
                5, 8, 2, 3, blockTag(BlockTags.AzaleaRootReplaceable), Provider("sulfur"), 20, 184, 1, 1,
                Provider("sulfur"), 1, 1, BlockPredicate.OnlyInAir));
        var caveVinesBody = new WeightedStateProvider(WeightedList<BlockState>.Builder()
            .Add(S("cave_vines_plant"), 4)
            .Add(S("cave_vines_plant").TrySetValue(CaveVineBerries, true), 1)
            .Build());
        var caveVinesHead = new RandomizedIntStateProvider(new WeightedStateProvider(
            WeightedList<BlockState>.Builder()
                .Add(S("cave_vines"), 4)
                .Add(S("cave_vines").TrySetValue(CaveVineBerries, true), 1)
                .Build()), "age", UniformInt.Of(23, 25));
        FeatureUtils.Register(context, CaveVine, BlockColumnFeature.Instance, new BlockColumnConfiguration(
            new BlockColumnConfiguration.Layer[]
            {
                new(new WeightedListInt(WeightedList<GameIntProvider>.Builder()
                        .Add(UniformInt.Of(0, 19), 2).Add(UniformInt.Of(0, 2), 3).Add(UniformInt.Of(0, 6), 10)
                        .Build()), caveVinesBody),
                new(ConstantInt.Of(1), caveVinesHead)
            }, Direction.Down, BlockPredicate.OnlyInAir, true));
        FeatureUtils.Register(context, CaveVineInMoss, BlockColumnFeature.Instance, new BlockColumnConfiguration(
            new BlockColumnConfiguration.Layer[]
            {
                new(new WeightedListInt(WeightedList<GameIntProvider>.Builder()
                        .Add(UniformInt.Of(0, 3), 5).Add(UniformInt.Of(1, 7), 1).Build()), caveVinesBody),
                new(ConstantInt.Of(1), caveVinesHead)
            }, Direction.Down, BlockPredicate.OnlyInAir, true));
        FeatureUtils.Register(context, MossVegetation, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(S("flowering_azalea"), 4).Add(S("azalea"), 7).Add(S("moss_carpet"), 25)
                .Add(S("short_grass"), 50).Add(S("tall_grass"), 10).Build()), false));
        FeatureUtils.Register(context, MossPatch, VegetationPatchFeature.Instance,
            PatchConfig(BlockTags.MossReplaceable, "moss_block", features.GetOrThrow(MossVegetation), CaveSurface.Floor, 1, 0.0f, 5, 0.8f,
                4, 7, 0.3f));
        FeatureUtils.Register(context, MossPatchBonemeal, VegetationPatchFeature.Instance,
            PatchConfig(BlockTags.MossReplaceable, "moss_block", features.GetOrThrow(MossVegetation), CaveSurface.Floor, 1, 0.0f, 5, 0.6f,
                1, 2, 0.75f));
        FeatureUtils.Register(context, Dripleaf, SimpleRandomSelectorFeature.Instance,
            new CompositeFeatureConfiguration(new DirectHolderSet<RegistryPlacedFeature>(new[]
            {
                MakeSmallDripleaf(),
                MakeDripleaf(Direction.East), MakeDripleaf(Direction.West), MakeDripleaf(Direction.South),
                MakeDripleaf(Direction.North)
            })));
        FeatureUtils.Register(context, ClayWithDripleaves, VegetationPatchFeature.Instance,
            PatchConfig(BlockTags.LushGroundReplaceable, "clay", features.GetOrThrow(Dripleaf), CaveSurface.Floor, 3, 0.8f, 2, 0.05f,
                4, 7, 0.7f));
        FeatureUtils.Register(context, ClayPoolWithDripleaves, WaterloggedVegetationPatchFeature.WaterloggedInstance,
            PatchConfig(BlockTags.LushGroundReplaceable, "clay", features.GetOrThrow(Dripleaf), CaveSurface.Floor, 3, 0.8f, 5, 0.1f,
                4, 7, 0.7f));
        FeatureUtils.Register(context, LushCavesClay, RandomBooleanSelectorFeature.Instance,
            new RandomBooleanFeatureConfiguration(
                PlacementUtils.InlinePlaced(context.LookupOrThrow(Registries.CONFIGURED_FEATURE)
                    .GetOrThrow(ClayWithDripleaves)),
                PlacementUtils.InlinePlaced(context.LookupOrThrow(Registries.CONFIGURED_FEATURE)
                    .GetOrThrow(ClayPoolWithDripleaves))));
        FeatureUtils.Register(context, MossPatchCeiling, VegetationPatchFeature.Instance,
            new VegetationPatchConfiguration(blockTag(BlockTags.MossReplaceable), Provider("moss_block"),
                PlacementUtils.InlinePlaced(context.LookupOrThrow(Registries.CONFIGURED_FEATURE)
                    .GetOrThrow(CaveVineInMoss)), CaveSurface.Ceiling, UniformInt.Of(1, 2), 0.0f, 5, 0.08f,
                UniformInt.Of(4, 7), 0.3f));
        FeatureUtils.Register(context, SporeBlossom, SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            BlockStateProvider.Simple(B("spore_blossom")), false));
        FeatureUtils.Register(context, AmethystGeode, GeodeFeature.Instance, new GeodeConfiguration(
            new GeodeBlockSettings(Provider("air"), Provider("amethyst_block"), Provider("budding_amethyst"),
                Provider("calcite"), Provider("smooth_basalt"),
                new[] { S("small_amethyst_bud"), S("medium_amethyst_bud"), S("large_amethyst_bud"),
                    S("amethyst_cluster") },
                blockTag(BlockTags.FeaturesCannotReplace), blockTag(BlockTags.GeodeInvalidBlocks)),
            new GeodeLayerSettings(1.7, 2.2, 3.2, 4.2), new GeodeCrackSettings(0.95, 2.0, 2), 0.35, 0.083, true,
            UniformInt.Of(4, 6), UniformInt.Of(3, 4), UniformInt.Of(1, 2), -16, 16, 0.05, 1));
        FeatureUtils.Register(context, SculkPatchDeepDark, SculkPatchFeature.Instance,
            new SculkPatchConfiguration(10, 32, 64, 0, 1, ConstantInt.Of(0), 0.5f));
        FeatureUtils.Register(context, SculkPatchAncientCity, SculkPatchFeature.Instance,
            new SculkPatchConfiguration(10, 32, 64, 0, 1, UniformInt.Of(1, 3), 0.5f));
        FeatureUtils.Register(context, SculkVein, MultifaceGrowthFeature.Instance,
            new MultifaceGrowthConfiguration(B("sculk_vein"), 20, true, true, true, 1.0f,
                DirectBlocks("stone", "andesite", "diorite", "granite", "dripstone_block", "calcite", "tuff",
                    "deepslate")));
    }

    //MakeDripleaf an inline placed big dripleaf column facing one direction, maps to vanilla makeDripleaf
    private static Holder<RegistryPlacedFeature> MakeDripleaf(Direction direction)
        => PlacementUtils.InlinePlaced(BlockColumnFeature.Instance, new BlockColumnConfiguration(
            new BlockColumnConfiguration.Layer[]
            {
                new(new WeightedListInt(WeightedList<GameIntProvider>.Builder()
                        .Add(UniformInt.Of(0, 4), 2).Add(ConstantInt.Of(0), 1).Build()),
                    BlockStateProvider.Simple(S("big_dripleaf_stem")
                        .TrySetValue(BlockStateProperties.HorizontalFacing, ToEnumDirection(direction)))),
                new(ConstantInt.Of(1), BlockStateProvider.Simple(S("big_dripleaf")
                    .TrySetValue(BlockStateProperties.HorizontalFacing, ToEnumDirection(direction))))
            }, Direction.Up, BlockPredicate.OnlyInAirOrWater, true));

    //MakeSmallDripleaf an inline placed small dripleaf with a random facing, maps to vanilla makeSmallDripleaf
    private static Holder<RegistryPlacedFeature> MakeSmallDripleaf()
        => PlacementUtils.InlinePlaced(SimpleBlockFeature.Instance, new SimpleBlockConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(S("small_dripleaf").TrySetValue(BlockStateProperties.HorizontalFacing,
                    NacreCraft.Registry.Enums.Direction.east), 1)
                .Add(S("small_dripleaf").TrySetValue(BlockStateProperties.HorizontalFacing,
                    NacreCraft.Registry.Enums.Direction.west), 1)
                .Add(S("small_dripleaf").TrySetValue(BlockStateProperties.HorizontalFacing,
                    NacreCraft.Registry.Enums.Direction.north), 1)
                .Add(S("small_dripleaf").TrySetValue(BlockStateProperties.HorizontalFacing,
                    NacreCraft.Registry.Enums.Direction.south), 1)
                .Build()), false));

    //PatchConfig a vegetation patch configuration, maps to the repeated VegetationPatchConfiguration form
    private static VegetationPatchConfiguration PatchConfig(TagKey<RegBlock> replaceable, string ground,
        Holder<RegistryConfiguredFeature> vegetation, CaveSurface surface, int depth, float extraBottomChance,
        int verticalRange, float vegetationChance, int minRadius, int maxRadius, float extraEdgeChance)
        => new(BuiltInRegistries.BLOCK.GetOrCreate(replaceable), Provider(ground),
            PlacementUtils.InlinePlaced(vegetation),
            surface, ConstantInt.Of(depth), extraBottomChance, verticalRange, vegetationChance,
            UniformInt.Of(minRadius, maxRadius), extraEdgeChance);

    //ToEnumDirection converts a primitive direction to the enum direction used by block state properties
    private static NacreCraft.Registry.Enums.Direction ToEnumDirection(Direction direction)
        => direction.Id3D switch
        {
            Direction.NorthId => NacreCraft.Registry.Enums.Direction.north,
            Direction.EastId => NacreCraft.Registry.Enums.Direction.east,
            Direction.SouthId => NacreCraft.Registry.Enums.Direction.south,
            _ => NacreCraft.Registry.Enums.Direction.west
        };

    private static RegBlock B(string path) => WorldGenDataSupport.Block(path);

    private static BlockState S(string path) => WorldGenDataSupport.State(path);

    private static BlockStateProvider Provider(string path) => BlockStateProvider.Simple(WorldGenDataSupport.Block(path));

    private static IReadOnlyList<Identifier> Ids(params string[] paths)
        => paths.Select(Identifier.WithDefaultNamespace).ToList();

    private static HolderSet<RegBlock> DirectBlocks(params string[] paths)
        => new DirectHolderSet<RegBlock>(
            paths.Select(path => BuiltInRegistries.BLOCK.WrapAsHolder(WorldGenDataSupport.Block(path))).ToList());
}
