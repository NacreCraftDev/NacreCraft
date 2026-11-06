using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Features.Impl.Nether;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;

namespace NacreCraft.Game.Data.WorldGen.Features;

//EndFeatures built-in configured features for the end, maps to vanilla net.minecraft.data.worldgen.features.EndFeatures
public static class EndFeatures
{
    //EndSpawnPoint the obsidian platform center the return gateway points at, maps to vanilla ServerLevel.END_SPAWN_POINT
    private static readonly BlockPos EndSpawnPoint = new(0, 64, 0);

    public static readonly ResourceKey<RegistryConfiguredFeature> EndPlatform = FeatureUtils.CreateKey("end_platform");
    public static readonly ResourceKey<RegistryConfiguredFeature> EndSpike = FeatureUtils.CreateKey("end_spike");
    public static readonly ResourceKey<RegistryConfiguredFeature> EndGatewayReturn =
        FeatureUtils.CreateKey("end_gateway_return");
    public static readonly ResourceKey<RegistryConfiguredFeature> EndGatewayDelayed =
        FeatureUtils.CreateKey("end_gateway_delayed");
    public static readonly ResourceKey<RegistryConfiguredFeature> ChorusPlant = FeatureUtils.CreateKey("chorus_plant");
    public static readonly ResourceKey<RegistryConfiguredFeature> EndIsland = FeatureUtils.CreateKey("end_island");

    public static void Bootstrap(BootstrapContext<RegistryConfiguredFeature> context)
    {
        FeatureUtils.Register(context, EndPlatform, EndPlatformFeature.Instance);
        FeatureUtils.Register(context, EndSpike, EndSpikeFeature.Instance,
            new EndSpikeConfiguration(false, Array.Empty<EndSpike>(), null));
        FeatureUtils.Register(context, EndGatewayReturn, EndGatewayFeature.Instance,
            new EndGatewayConfiguration(EndSpawnPoint, true));
        FeatureUtils.Register(context, EndGatewayDelayed, EndGatewayFeature.Instance,
            new EndGatewayConfiguration(null, false));
        FeatureUtils.Register(context, ChorusPlant, ChorusPlantFeature.Instance);
        FeatureUtils.Register(context, EndIsland, EndIslandFeature.Instance);
    }
}
