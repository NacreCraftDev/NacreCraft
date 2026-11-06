using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Util.Random;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//PileFeatures built-in block pile features, maps to vanilla net.minecraft.data.worldgen.features.PileFeatures
public static class PileFeatures
{
    public static readonly ResourceKey<RegistryConfiguredFeature> PileHay = FeatureUtils.CreateKey("pile_hay");
    public static readonly ResourceKey<RegistryConfiguredFeature> PileMelon = FeatureUtils.CreateKey("pile_melon");
    public static readonly ResourceKey<RegistryConfiguredFeature> PileSnow = FeatureUtils.CreateKey("pile_snow");
    public static readonly ResourceKey<RegistryConfiguredFeature> PileIce = FeatureUtils.CreateKey("pile_ice");
    public static readonly ResourceKey<RegistryConfiguredFeature> PilePumpkin = FeatureUtils.CreateKey("pile_pumpkin");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        FeatureUtils.Register(context, PileHay, BlockPileFeature.Instance,
            new BlockPileConfiguration(new RotatedBlockProvider(WorldGenDataSupport.Block("hay_block"))));
        FeatureUtils.Register(context, PileMelon, BlockPileFeature.Instance,
            new BlockPileConfiguration(BlockStateProvider.Simple(WorldGenDataSupport.Block("melon"))));
        FeatureUtils.Register(context, PileSnow, BlockPileFeature.Instance,
            new BlockPileConfiguration(BlockStateProvider.Simple(WorldGenDataSupport.Block("snow"))));
        FeatureUtils.Register(context, PileIce, BlockPileFeature.Instance, new BlockPileConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(WorldGenDataSupport.State("blue_ice"), 1)
                .Add(WorldGenDataSupport.State("packed_ice"), 5)
                .Build())));
        FeatureUtils.Register(context, PilePumpkin, BlockPileFeature.Instance, new BlockPileConfiguration(
            new WeightedStateProvider(WeightedList<BlockState>.Builder()
                .Add(WorldGenDataSupport.State("pumpkin"), 19)
                .Add(WorldGenDataSupport.State("jack_o_lantern"), 1)
                .Build())));
    }
}
