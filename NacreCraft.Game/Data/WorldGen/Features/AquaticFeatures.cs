using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Misc;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Vegetation;
using NacreCraft.Registry;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//AquaticFeatures built-in configured features for oceans and rivers, maps to vanilla net.minecraft.data.worldgen.features.AquaticFeatures
public static class AquaticFeatures
{
    public static readonly ResourceKey<RegistryConfiguredFeature> SeagrassShort =
        FeatureUtils.CreateKey("seagrass_short");
    public static readonly ResourceKey<RegistryConfiguredFeature> SeagrassSlightlyLessShort =
        FeatureUtils.CreateKey("seagrass_slightly_less_short");
    public static readonly ResourceKey<RegistryConfiguredFeature> SeagrassMid =
        FeatureUtils.CreateKey("seagrass_mid");
    public static readonly ResourceKey<RegistryConfiguredFeature> SeagrassTall =
        FeatureUtils.CreateKey("seagrass_tall");
    public static readonly ResourceKey<RegistryConfiguredFeature> SeaPickle =
        FeatureUtils.CreateKey("sea_pickle");
    public static readonly ResourceKey<RegistryConfiguredFeature> Kelp =
        FeatureUtils.CreateKey("kelp");
    public static readonly ResourceKey<RegistryConfiguredFeature> WarmOceanVegetation =
        FeatureUtils.CreateKey("warm_ocean_vegetation");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        FeatureUtils.Register(context, SeagrassShort, SeagrassFeature.Instance, new ProbabilityFeatureConfiguration(0.3f));
        FeatureUtils.Register(context, SeagrassSlightlyLessShort, SeagrassFeature.Instance,
            new ProbabilityFeatureConfiguration(0.4f));
        FeatureUtils.Register(context, SeagrassMid, SeagrassFeature.Instance, new ProbabilityFeatureConfiguration(0.6f));
        FeatureUtils.Register(context, SeagrassTall, SeagrassFeature.Instance, new ProbabilityFeatureConfiguration(0.8f));
        FeatureUtils.Register(context, SeaPickle, SeaPickleFeature.Instance, new CountConfiguration(ConstantInt.Of(20)));
        FeatureUtils.Register(context, Kelp, KelpFeature.Instance);
        var warmOceanVegetation = new CompositeFeatureConfiguration(new DirectHolderSet<RegistryPlacedFeature>(new[]
        {
            PlacementUtils.InlinePlaced(CoralTreeFeature.Instance, NoneFeatureConfiguration.Instance),
            PlacementUtils.InlinePlaced(CoralClawFeature.Instance, NoneFeatureConfiguration.Instance),
            PlacementUtils.InlinePlaced(CoralMushroomFeature.Instance, NoneFeatureConfiguration.Instance)
        }));
        FeatureUtils.Register(context, WarmOceanVegetation, SimpleRandomSelectorFeature.Instance, warmOceanVegetation);
    }
}
