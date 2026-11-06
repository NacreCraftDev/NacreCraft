using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Registry;
using GameConfiguredFeature = NacreCraft.Game.World.Level.LevelGen.Features.ConfiguredFeature;
using GameFeature = NacreCraft.Game.World.Level.LevelGen.Features.Feature;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//FeatureUtils shared configured feature construction, maps to vanilla net.minecraft.data.worldgen.features.FeatureUtils
//Every feature data class registers through CreateKey + Register; the aggregate Bootstrap grows as the remaining data classes land
public static class FeatureUtils
{
    //Bootstrap registers every built-in configured feature data class, maps to vanilla bootstrap
    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        AquaticFeatures.Bootstrap(context);
        CaveFeatures.Bootstrap(context);
        EndFeatures.Bootstrap(context);
        MiscOverworldFeatures.Bootstrap(context);
        NetherFeatures.Bootstrap(context);
        OreFeatures.Bootstrap(context);
        PileFeatures.Bootstrap(context);
        TreeFeatures.Bootstrap(context);
        VegetationFeatures.Bootstrap(context);
    }

    public static ResourceKey<RegistryConfiguredFeature> CreateKey(string name)
        => ResourceKey<RegistryConfiguredFeature>.Create(Registries.CONFIGURED_FEATURE,
            Identifier.WithDefaultNamespace(name));

    //Register a feature that takes no configuration, maps to vanilla register(context, id, feature)
    public static void Register(BootstrapContext<RegistryConfiguredFeature> context,
        ResourceKey<RegistryConfiguredFeature> id, GameFeature feature)
        => Register(context, id, feature, NoneFeatureConfiguration.Instance);

    public static void Register(BootstrapContext<RegistryConfiguredFeature> context,
        ResourceKey<RegistryConfiguredFeature> id, GameFeature feature, FeatureConfiguration config)
        => context.Register(id, new GameConfiguredFeature(feature, config));
}
