using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Registry;

namespace NacreCraft.Game.Data.WorldGen.Biomes;

//EndBiomes built-in end biomes, maps to vanilla net.minecraft.data.worldgen.biome.EndBiomes
public static class EndBiomes
{
    //BaseEndBiome the shared end biome skeleton: no precipitation, mild climate, plain water color and end spawns
    private static Biome BaseEndBiome(BiomeGenerationSettings.Builder generation)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.EndSpawns(mobs);
        return new Biome.BiomeBuilder()
            .HasPrecipitation(false)
            .Temperature(0.5f)
            .Downfall(0.5f)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(4159204).Build())
            .MobSpawnSettings(mobs.Build())
            .GenerationSettings(generation.Build())
            .Build();
    }

    public static Biome EndBarrens(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
        => BaseEndBiome(new BiomeGenerationSettings.Builder(placedFeatures, carvers));

    public static Biome TheEnd(HolderLookup<PlacedFeature> placedFeatures, HolderLookup<ConfiguredWorldCarver> carvers)
        => BaseEndBiome(new BiomeGenerationSettings.Builder(placedFeatures, carvers)
            .AddFeature((int)GenerationStep.Decoration.SurfaceStructures, EndPlacements.EndSpike)
            .AddFeature((int)GenerationStep.Decoration.TopLayerModification, EndPlacements.EndPlatform));

    public static Biome EndMidlands(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
        => BaseEndBiome(new BiomeGenerationSettings.Builder(placedFeatures, carvers));

    public static Biome EndHighlands(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
        => BaseEndBiome(new BiomeGenerationSettings.Builder(placedFeatures, carvers)
            .AddFeature((int)GenerationStep.Decoration.SurfaceStructures, EndPlacements.EndGatewayReturn)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, EndPlacements.ChorusPlant));

    public static Biome SmallEndIslands(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
        => BaseEndBiome(new BiomeGenerationSettings.Builder(placedFeatures, carvers)
            .AddFeature((int)GenerationStep.Decoration.RawGeneration, EndPlacements.EndIslandDecorated));
}
