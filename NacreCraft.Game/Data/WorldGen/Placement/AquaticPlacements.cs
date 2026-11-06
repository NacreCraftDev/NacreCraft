using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Registry;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//AquaticPlacements built-in placed ocean and river vegetation, maps to vanilla net.minecraft.data.worldgen.placement.AquaticPlacements
public static class AquaticPlacements
{
    public static readonly ResourceKey<RegistryPlacedFeature> SeagrassWarm =
        PlacementUtils.CreateKey("seagrass_warm");
    public static readonly ResourceKey<RegistryPlacedFeature> SeagrassNormal =
        PlacementUtils.CreateKey("seagrass_normal");
    public static readonly ResourceKey<RegistryPlacedFeature> SeagrassCold =
        PlacementUtils.CreateKey("seagrass_cold");
    public static readonly ResourceKey<RegistryPlacedFeature> SeagrassRiver =
        PlacementUtils.CreateKey("seagrass_river");
    public static readonly ResourceKey<RegistryPlacedFeature> SeagrassSwamp =
        PlacementUtils.CreateKey("seagrass_swamp");
    public static readonly ResourceKey<RegistryPlacedFeature> SeagrassDeepWarm =
        PlacementUtils.CreateKey("seagrass_deep_warm");
    public static readonly ResourceKey<RegistryPlacedFeature> SeagrassDeep =
        PlacementUtils.CreateKey("seagrass_deep");
    public static readonly ResourceKey<RegistryPlacedFeature> SeagrassDeepCold =
        PlacementUtils.CreateKey("seagrass_deep_cold");
    public static readonly ResourceKey<RegistryPlacedFeature> SeaPickle = PlacementUtils.CreateKey("sea_pickle");
    public static readonly ResourceKey<RegistryPlacedFeature> KelpCold = PlacementUtils.CreateKey("kelp_cold");
    public static readonly ResourceKey<RegistryPlacedFeature> KelpWarm = PlacementUtils.CreateKey("kelp_warm");
    public static readonly ResourceKey<RegistryPlacedFeature> WarmOceanVegetation =
        PlacementUtils.CreateKey("warm_ocean_vegetation");

    //SeagrassPlacement the spread/surface/count chain every seagrass entry uses, maps to vanilla seagrassPlacement
    private static PlacementModifier[] SeagrassPlacement(int count) =>
    [
        InSquarePlacement.Spread(), PlacementUtils.HeightmapTopSolid, CountPlacement.Of(count), BiomeFilter.Biome()
    ];

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var seagrassShort = features.GetOrThrow(AquaticFeatures.SeagrassShort);
        var seagrassSlightlyLessShort = features.GetOrThrow(AquaticFeatures.SeagrassSlightlyLessShort);
        var seagrassMid = features.GetOrThrow(AquaticFeatures.SeagrassMid);
        var seagrassTall = features.GetOrThrow(AquaticFeatures.SeagrassTall);
        var seaPickle = features.GetOrThrow(AquaticFeatures.SeaPickle);
        var kelp = features.GetOrThrow(AquaticFeatures.Kelp);
        var warmOceanVegetation = features.GetOrThrow(AquaticFeatures.WarmOceanVegetation);
        PlacementUtils.Register(context, SeagrassWarm, seagrassShort, SeagrassPlacement(80));
        PlacementUtils.Register(context, SeagrassNormal, seagrassShort, SeagrassPlacement(48));
        PlacementUtils.Register(context, SeagrassCold, seagrassShort, SeagrassPlacement(32));
        PlacementUtils.Register(context, SeagrassRiver, seagrassSlightlyLessShort, SeagrassPlacement(48));
        PlacementUtils.Register(context, SeagrassSwamp, seagrassMid, SeagrassPlacement(64));
        PlacementUtils.Register(context, SeagrassDeepWarm, seagrassTall, SeagrassPlacement(80));
        PlacementUtils.Register(context, SeagrassDeep, seagrassTall, SeagrassPlacement(48));
        PlacementUtils.Register(context, SeagrassDeepCold, seagrassTall, SeagrassPlacement(40));
        PlacementUtils.Register(context, SeaPickle, seaPickle, RarityFilter.OnAverageOnceEvery(16),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapTopSolid, BiomeFilter.Biome());
        PlacementUtils.Register(context, KelpCold, kelp, NoiseBasedCountPlacement.Of(120, 80.0, 0.0),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapTopSolid, BiomeFilter.Biome());
        PlacementUtils.Register(context, KelpWarm, kelp, NoiseBasedCountPlacement.Of(80, 80.0, 0.0),
            InSquarePlacement.Spread(), PlacementUtils.HeightmapTopSolid, BiomeFilter.Biome());
        PlacementUtils.Register(context, WarmOceanVegetation, warmOceanVegetation,
            NoiseBasedCountPlacement.Of(20, 400.0, 0.0), InSquarePlacement.Spread(),
            PlacementUtils.HeightmapTopSolid, BiomeFilter.Biome());
    }
}
