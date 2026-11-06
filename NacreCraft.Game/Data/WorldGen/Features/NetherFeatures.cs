using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Nether;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Vegetation;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Util.Random;
using RegBlock = NacreCraft.Registry.Block;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//NetherFeatures built-in configured features for the nether, maps to vanilla net.minecraft.data.worldgen.features.NetherFeatures
//Blocks that are not registered yet resolve through WorldGenDataSupport and fall back to air
public static class NetherFeatures
{
    public static readonly ResourceKey<RegistryConfiguredFeature> Delta = FeatureUtils.CreateKey("delta");
    public static readonly ResourceKey<RegistryConfiguredFeature> SmallBasaltColumns =
        FeatureUtils.CreateKey("small_basalt_columns");
    public static readonly ResourceKey<RegistryConfiguredFeature> LargeBasaltColumns =
        FeatureUtils.CreateKey("large_basalt_columns");
    public static readonly ResourceKey<RegistryConfiguredFeature> BasaltBlobs =
        FeatureUtils.CreateKey("basalt_blobs");
    public static readonly ResourceKey<RegistryConfiguredFeature> BlackstoneBlobs =
        FeatureUtils.CreateKey("blackstone_blobs");
    public static readonly ResourceKey<RegistryConfiguredFeature> GlowstoneExtra =
        FeatureUtils.CreateKey("glowstone_extra");
    public static readonly ResourceKey<RegistryConfiguredFeature> CrimsonForestVegetation =
        FeatureUtils.CreateKey("crimson_forest_vegetation");
    public static readonly ResourceKey<RegistryConfiguredFeature> CrimsonForestVegetationBonemeal =
        FeatureUtils.CreateKey("crimson_forest_vegetation_bonemeal");
    public static readonly ResourceKey<RegistryConfiguredFeature> WarpedForestVegetation =
        FeatureUtils.CreateKey("warped_forest_vegetation");
    public static readonly ResourceKey<RegistryConfiguredFeature> WarpedForestVegetationBonemeal =
        FeatureUtils.CreateKey("warped_forest_vegetation_bonemeal");
    public static readonly ResourceKey<RegistryConfiguredFeature> NetherSprouts =
        FeatureUtils.CreateKey("nether_sprouts");
    public static readonly ResourceKey<RegistryConfiguredFeature> NetherSproutsBonemeal =
        FeatureUtils.CreateKey("nether_sprouts_bonemeal");
    public static readonly ResourceKey<RegistryConfiguredFeature> TwistingVines =
        FeatureUtils.CreateKey("twisting_vines");
    public static readonly ResourceKey<RegistryConfiguredFeature> TwistingVinesBonemeal =
        FeatureUtils.CreateKey("twisting_vines_bonemeal");
    public static readonly ResourceKey<RegistryConfiguredFeature> WeepingVines =
        FeatureUtils.CreateKey("weeping_vines");
    public static readonly ResourceKey<RegistryConfiguredFeature> CrimsonRoots =
        FeatureUtils.CreateKey("crimson_roots");
    public static readonly ResourceKey<RegistryConfiguredFeature> BasaltPillar =
        FeatureUtils.CreateKey("basalt_pillar");
    public static readonly ResourceKey<RegistryConfiguredFeature> SpringLavaNether =
        FeatureUtils.CreateKey("spring_lava_nether");
    public static readonly ResourceKey<RegistryConfiguredFeature> SpringNetherClosed =
        FeatureUtils.CreateKey("spring_nether_closed");
    public static readonly ResourceKey<RegistryConfiguredFeature> SpringNetherOpen =
        FeatureUtils.CreateKey("spring_nether_open");
    public static readonly ResourceKey<RegistryConfiguredFeature> Fire = FeatureUtils.CreateKey("patch_fire");
    public static readonly ResourceKey<RegistryConfiguredFeature> SoulFire = FeatureUtils.CreateKey("patch_soul_fire");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        FeatureUtils.Register(context, Delta, DeltaFeature.Instance, new DeltaFeatureConfiguration(
            WorldGenDataSupport.State("lava"), WorldGenDataSupport.State("magma_block"),
            UniformInt.Of(3, 7), UniformInt.Of(0, 2)));
        FeatureUtils.Register(context, SmallBasaltColumns, BasaltColumnsFeature.Instance,
            new ColumnFeatureConfiguration(ConstantInt.Of(1), UniformInt.Of(1, 4)));
        FeatureUtils.Register(context, LargeBasaltColumns, BasaltColumnsFeature.Instance,
            new ColumnFeatureConfiguration(UniformInt.Of(2, 3), UniformInt.Of(5, 10)));
        FeatureUtils.Register(context, BasaltBlobs, NetherrackReplaceBlobsFeature.Instance,
            new ReplaceSphereConfiguration(WorldGenDataSupport.State("netherrack"), WorldGenDataSupport.State("basalt"),
                UniformInt.Of(3, 7)));
        FeatureUtils.Register(context, BlackstoneBlobs, NetherrackReplaceBlobsFeature.Instance,
            new ReplaceSphereConfiguration(WorldGenDataSupport.State("netherrack"),
                WorldGenDataSupport.State("blackstone"), UniformInt.Of(3, 7)));
        FeatureUtils.Register(context, GlowstoneExtra, GlowstoneFeature.Instance);
        var crimsonVegetationProvider = new WeightedStateProvider(WeightedList<BlockState>.Builder()
            .Add(WorldGenDataSupport.State("crimson_roots"), 87)
            .Add(WorldGenDataSupport.State("crimson_fungus"), 11)
            .Add(WorldGenDataSupport.State("warped_fungus"), 1)
            .Build());
        FeatureUtils.Register(context, CrimsonForestVegetation, NetherForestVegetationFeature.Instance,
            new NetherForestVegetationConfig(crimsonVegetationProvider, 8, 4));
        FeatureUtils.Register(context, CrimsonForestVegetationBonemeal, NetherForestVegetationFeature.Instance,
            new NetherForestVegetationConfig(crimsonVegetationProvider, 3, 1));
        var warpedVegetationProvider = new WeightedStateProvider(WeightedList<BlockState>.Builder()
            .Add(WorldGenDataSupport.State("warped_roots"), 85)
            .Add(WorldGenDataSupport.State("crimson_roots"), 1)
            .Add(WorldGenDataSupport.State("warped_fungus"), 13)
            .Add(WorldGenDataSupport.State("crimson_fungus"), 1)
            .Build());
        FeatureUtils.Register(context, WarpedForestVegetation, NetherForestVegetationFeature.Instance,
            new NetherForestVegetationConfig(warpedVegetationProvider, 8, 4));
        FeatureUtils.Register(context, WarpedForestVegetationBonemeal, NetherForestVegetationFeature.Instance,
            new NetherForestVegetationConfig(warpedVegetationProvider, 3, 1));
        FeatureUtils.Register(context, NetherSprouts, NetherForestVegetationFeature.Instance,
            new NetherForestVegetationConfig(BlockStateProvider.Simple(WorldGenDataSupport.Block("nether_sprouts")), 8, 4));
        FeatureUtils.Register(context, NetherSproutsBonemeal, NetherForestVegetationFeature.Instance,
            new NetherForestVegetationConfig(BlockStateProvider.Simple(WorldGenDataSupport.Block("nether_sprouts")), 3, 1));
        FeatureUtils.Register(context, TwistingVines, TwistingVinesFeature.Instance, new TwistingVinesConfig(8, 4, 8));
        FeatureUtils.Register(context, TwistingVinesBonemeal, TwistingVinesFeature.Instance, new TwistingVinesConfig(3, 1, 2));
        FeatureUtils.Register(context, WeepingVines, WeepingVinesFeature.Instance);
        FeatureUtils.Register(context, CrimsonRoots, SimpleBlockFeature.Instance,
            new SimpleBlockConfiguration(BlockStateProvider.Simple(WorldGenDataSupport.Block("crimson_roots")), false));
        FeatureUtils.Register(context, BasaltPillar, BasaltPillarFeature.Instance);
        //The vanilla spring state is a lava fluid state; SpringConfiguration keeps a block state here, so lava's legacy block stands in
        var lava = WorldGenDataSupport.State("lava");
        FeatureUtils.Register(context, SpringLavaNether, SpringFeature.Instance,
            new SpringConfiguration(lava, true, 4, 1, DirectBlocks(
                "netherrack", "soul_sand", "gravel", "magma_block", "blackstone")));
        FeatureUtils.Register(context, SpringNetherClosed, SpringFeature.Instance,
            new SpringConfiguration(lava, false, 5, 0, DirectBlocks("netherrack")));
        FeatureUtils.Register(context, SpringNetherOpen, SpringFeature.Instance,
            new SpringConfiguration(lava, false, 4, 1, DirectBlocks("netherrack")));
        FeatureUtils.Register(context, Fire, SimpleBlockFeature.Instance,
            new SimpleBlockConfiguration(BlockStateProvider.Simple(WorldGenDataSupport.Block("fire")), false));
        FeatureUtils.Register(context, SoulFire, SimpleBlockFeature.Instance,
            new SimpleBlockConfiguration(BlockStateProvider.Simple(WorldGenDataSupport.Block("soul_fire")), false));
    }

    //DirectBlocks the direct holder set of the named blocks, maps to vanilla HolderSet.direct
    private static HolderSet<RegBlock> DirectBlocks(params string[] paths)
        => new DirectHolderSet<RegBlock>(paths.Select(path => BuiltInRegistries.BLOCK.WrapAsHolder(WorldGenDataSupport.Block(path))).ToList());
}
