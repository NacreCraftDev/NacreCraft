using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//EndPlacements built-in placed end features, maps to vanilla net.minecraft.data.worldgen.placement.EndPlacements
public static class EndPlacements
{
    //EndSpawnPoint the obsidian platform center, maps to vanilla ServerLevel.END_SPAWN_POINT
    private static readonly BlockPos EndSpawnPoint = new(0, 64, 0);

    public static readonly ResourceKey<RegistryPlacedFeature> EndPlatform = PlacementUtils.CreateKey("end_platform");
    public static readonly ResourceKey<RegistryPlacedFeature> EndSpike = PlacementUtils.CreateKey("end_spike");
    public static readonly ResourceKey<RegistryPlacedFeature> EndGatewayReturn =
        PlacementUtils.CreateKey("end_gateway_return");
    public static readonly ResourceKey<RegistryPlacedFeature> ChorusPlant = PlacementUtils.CreateKey("chorus_plant");
    public static readonly ResourceKey<RegistryPlacedFeature> EndIslandDecorated =
        PlacementUtils.CreateKey("end_island_decorated");

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var endPlatform = features.GetOrThrow(EndFeatures.EndPlatform);
        var endSpike = features.GetOrThrow(EndFeatures.EndSpike);
        var endGatewayReturn = features.GetOrThrow(EndFeatures.EndGatewayReturn);
        var chorusPlant = features.GetOrThrow(EndFeatures.ChorusPlant);
        var endIsland = features.GetOrThrow(EndFeatures.EndIsland);
        PlacementUtils.Register(context, EndPlatform, endPlatform,
            FixedPlacement.Of(EndSpawnPoint.Offset(0, -1, 0)), BiomeFilter.Biome());
        PlacementUtils.Register(context, EndSpike, endSpike, BiomeFilter.Biome());
        PlacementUtils.Register(context, EndGatewayReturn, endGatewayReturn,
            RarityFilter.OnAverageOnceEvery(700), InSquarePlacement.Spread(), PlacementUtils.Heightmap,
            RandomOffsetPlacement.Vertical(UniformInt.Of(3, 9)), BiomeFilter.Biome());
        PlacementUtils.Register(context, ChorusPlant, chorusPlant,
            CountPlacement.Of(UniformInt.Of(0, 4)), InSquarePlacement.Spread(), PlacementUtils.Heightmap,
            BiomeFilter.Biome());
        PlacementUtils.Register(context, EndIslandDecorated, endIsland,
            RarityFilter.OnAverageOnceEvery(14), PlacementUtils.CountExtra(1, 0.25f, 1), InSquarePlacement.Spread(),
            HeightRangePlacement.Uniform(VerticalAnchor.Absolute(55), VerticalAnchor.Absolute(70)),
            BiomeFilter.Biome());
    }
}
