using NacreCraft.Codec;
using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Environment;

namespace NacreCraft.Game.Data.WorldGen.Biomes;

//NetherBiomes built-in nether biomes, maps to vanilla net.minecraft.data.worldgen.biome.NetherBiomes
//Sound and particle references are identifiers because the attribute settings in this project use ids instead of sound instances
public static class NetherBiomes
{
    private static Biome.BiomeBuilder BaseBiome()
        => new Biome.BiomeBuilder()
            .HasPrecipitation(false)
            .Temperature(2.0f)
            .Downfall(0.0f)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(4159204).Build());

    public static Biome NetherWastes(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder()
            .AddSpawn(MobCategory.Monster, 50, Spawn("ghast", 4, 4))
            .AddSpawn(MobCategory.Monster, 100, Spawn("zombified_piglin", 4, 4))
            .AddSpawn(MobCategory.Monster, 2, Spawn("magma_cube", 4, 4))
            .AddSpawn(MobCategory.Monster, 1, Spawn("enderman", 4, 4))
            .AddSpawn(MobCategory.Monster, 15, Spawn("piglin", 4, 4))
            .AddSpawn(MobCategory.Creature, 60, Spawn("strider", 1, 2))
            .Build();
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers)
            .AddCarver(NacreCraft.Game.Data.WorldGen.Carvers.NetherCave)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, MiscOverworldPlacements.SpringLava);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        var step = (int)GenerationStep.Decoration.UndergroundDecoration;
        generation
            .AddFeature(step, NetherPlacements.SpringOpen)
            .AddFeature(step, NetherPlacements.PatchFire)
            .AddFeature(step, NetherPlacements.PatchSoulFire)
            .AddFeature(step, NetherPlacements.GlowstoneExtra)
            .AddFeature(step, NetherPlacements.Glowstone)
            .AddFeature(step, VegetationPlacements.BrownMushroomNether)
            .AddFeature(step, VegetationPlacements.RedMushroomNether)
            .AddFeature(step, OrePlacements.OreMagma)
            .AddFeature(step, NetherPlacements.SpringClosed);
        BiomeDefaultFeatures.AddNetherDefaultOres(generation);
        return BaseBiome()
            .SetAttribute(EnvironmentAttributes.FogColor, -13432824)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic,
                new BackgroundMusic(new Music(Id("music.nether.nether_wastes"), 12000, 24000, false)))
            .SetAttribute(EnvironmentAttributes.AmbientSounds, Ambient("nether_wastes", "nether_wastes", "nether_wastes"))
            .MobSpawnSettings(mobs)
            .GenerationSettings(generation.Build())
            .Build();
    }

    public static Biome SoulSandValley(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder()
            .AddSpawn(MobCategory.Monster, 20, Spawn("skeleton", 5, 5))
            .AddSpawn(MobCategory.Monster, 50, Spawn("ghast", 4, 4))
            .AddSpawn(MobCategory.Monster, 1, Spawn("enderman", 4, 4))
            .AddSpawn(MobCategory.Creature, 60, Spawn("strider", 1, 2))
            .AddMobCharge(Id("skeleton"), 0.7, 0.15)
            .AddMobCharge(Id("ghast"), 0.7, 0.15)
            .AddMobCharge(Id("enderman"), 0.7, 0.15)
            .AddMobCharge(Id("strider"), 0.7, 0.15)
            .Build();
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers)
            .AddCarver(NacreCraft.Game.Data.WorldGen.Carvers.NetherCave)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, MiscOverworldPlacements.SpringLava)
            .AddFeature((int)GenerationStep.Decoration.LocalModifications, NetherPlacements.BasaltPillar);
        var step = (int)GenerationStep.Decoration.UndergroundDecoration;
        generation
            .AddFeature(step, NetherPlacements.SpringOpen)
            .AddFeature(step, NetherPlacements.PatchFire)
            .AddFeature(step, NetherPlacements.PatchSoulFire)
            .AddFeature(step, NetherPlacements.GlowstoneExtra)
            .AddFeature(step, NetherPlacements.Glowstone)
            .AddFeature(step, NetherPlacements.PatchCrimsonRoots)
            .AddFeature(step, OrePlacements.OreMagma)
            .AddFeature(step, NetherPlacements.SpringClosed)
            .AddFeature(step, OrePlacements.OreSoulSand);
        BiomeDefaultFeatures.AddNetherDefaultOres(generation);
        return BaseBiome()
            .SetAttribute(EnvironmentAttributes.FogColor, -14989499)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic,
                new BackgroundMusic(new Music(Id("music.nether.soul_sand_valley"), 12000, 24000, false)))
            .SetAttribute(EnvironmentAttributes.AmbientParticles, AmbientParticle.Of(Id("ash"), 0.00625f))
            .SetAttribute(EnvironmentAttributes.AmbientSounds,
                Ambient("soul_sand_valley", "soul_sand_valley", "soul_sand_valley"))
            .MobSpawnSettings(mobs)
            .GenerationSettings(generation.Build())
            .Build();
    }

    public static Biome BasaltDeltas(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder()
            .AddSpawn(MobCategory.Monster, 40, Spawn("ghast", 1, 1))
            .AddSpawn(MobCategory.Monster, 100, Spawn("magma_cube", 2, 5))
            .AddSpawn(MobCategory.Creature, 60, Spawn("strider", 1, 2))
            .Build();
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers)
            .AddCarver(NacreCraft.Game.Data.WorldGen.Carvers.NetherCave)
            .AddFeature((int)GenerationStep.Decoration.SurfaceStructures, NetherPlacements.Delta)
            .AddFeature((int)GenerationStep.Decoration.SurfaceStructures, NetherPlacements.SmallBasaltColumns)
            .AddFeature((int)GenerationStep.Decoration.SurfaceStructures, NetherPlacements.LargeBasaltColumns);
        var step = (int)GenerationStep.Decoration.UndergroundDecoration;
        generation
            .AddFeature(step, NetherPlacements.BasaltBlobs)
            .AddFeature(step, NetherPlacements.BlackstoneBlobs)
            .AddFeature(step, NetherPlacements.SpringDelta)
            .AddFeature(step, NetherPlacements.PatchFire)
            .AddFeature(step, NetherPlacements.PatchSoulFire)
            .AddFeature(step, NetherPlacements.GlowstoneExtra)
            .AddFeature(step, NetherPlacements.Glowstone)
            .AddFeature(step, VegetationPlacements.BrownMushroomNether)
            .AddFeature(step, VegetationPlacements.RedMushroomNether)
            .AddFeature(step, OrePlacements.OreMagma)
            .AddFeature(step, NetherPlacements.SpringClosedDouble)
            .AddFeature(step, OrePlacements.OreGoldDeltas)
            .AddFeature(step, OrePlacements.OreQuartzDeltas);
        BiomeDefaultFeatures.AddAncientDebris(generation);
        return BaseBiome()
            .SetAttribute(EnvironmentAttributes.FogColor, -9937040)
            .SetAttribute(EnvironmentAttributes.AmbientParticles, AmbientParticle.Of(Id("white_ash"), 0.118093334f))
            .SetAttribute(EnvironmentAttributes.BackgroundMusic,
                new BackgroundMusic(new Music(Id("music.nether.basalt_deltas"), 12000, 24000, false)))
            .SetAttribute(EnvironmentAttributes.AmbientSounds,
                Ambient("basalt_deltas", "basalt_deltas", "basalt_deltas"))
            .MobSpawnSettings(mobs)
            .GenerationSettings(generation.Build())
            .Build();
    }

    public static Biome CrimsonForest(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder()
            .AddSpawn(MobCategory.Monster, 1, Spawn("zombified_piglin", 2, 4))
            .AddSpawn(MobCategory.Monster, 9, Spawn("hoglin", 3, 4))
            .AddSpawn(MobCategory.Monster, 5, Spawn("piglin", 3, 4))
            .AddSpawn(MobCategory.Creature, 60, Spawn("strider", 1, 2))
            .Build();
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers)
            .AddCarver(NacreCraft.Game.Data.WorldGen.Carvers.NetherCave)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, MiscOverworldPlacements.SpringLava);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        var step = (int)GenerationStep.Decoration.UndergroundDecoration;
        generation
            .AddFeature(step, NetherPlacements.SpringOpen)
            .AddFeature(step, NetherPlacements.PatchFire)
            .AddFeature(step, NetherPlacements.GlowstoneExtra)
            .AddFeature(step, NetherPlacements.Glowstone)
            .AddFeature(step, OrePlacements.OreMagma)
            .AddFeature(step, NetherPlacements.SpringClosed)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, NetherPlacements.WeepingVines)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, TreePlacements.CrimsonFungi)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, NetherPlacements.CrimsonForestVegetation);
        BiomeDefaultFeatures.AddNetherDefaultOres(generation);
        return BaseBiome()
            .SetAttribute(EnvironmentAttributes.FogColor, -13434109)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic,
                new BackgroundMusic(new Music(Id("music.nether.crimson_forest"), 12000, 24000, false)))
            .SetAttribute(EnvironmentAttributes.AmbientParticles, AmbientParticle.Of(Id("crimson_spore"), 0.025f))
            .SetAttribute(EnvironmentAttributes.AmbientSounds,
                Ambient("crimson_forest", "crimson_forest", "crimson_forest"))
            .MobSpawnSettings(mobs)
            .GenerationSettings(generation.Build())
            .Build();
    }

    public static Biome WarpedForest(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder()
            .AddSpawn(MobCategory.Monster, 1, Spawn("enderman", 4, 4))
            .AddSpawn(MobCategory.Creature, 60, Spawn("strider", 1, 2))
            .AddMobCharge(Id("enderman"), 1.0, 0.12)
            .Build();
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers)
            .AddCarver(NacreCraft.Game.Data.WorldGen.Carvers.NetherCave)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, MiscOverworldPlacements.SpringLava);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        var step = (int)GenerationStep.Decoration.UndergroundDecoration;
        generation
            .AddFeature(step, NetherPlacements.SpringOpen)
            .AddFeature(step, NetherPlacements.PatchFire)
            .AddFeature(step, NetherPlacements.PatchSoulFire)
            .AddFeature(step, NetherPlacements.GlowstoneExtra)
            .AddFeature(step, NetherPlacements.Glowstone)
            .AddFeature(step, OrePlacements.OreMagma)
            .AddFeature(step, NetherPlacements.SpringClosed)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, TreePlacements.WarpedFungi)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, NetherPlacements.WarpedForestVegetation)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, NetherPlacements.NetherSprouts)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, NetherPlacements.TwistingVines);
        BiomeDefaultFeatures.AddNetherDefaultOres(generation);
        return BaseBiome()
            .SetAttribute(EnvironmentAttributes.FogColor, -15071974)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic,
                new BackgroundMusic(new Music(Id("music.nether.warped_forest"), 12000, 24000, false)))
            .SetAttribute(EnvironmentAttributes.AmbientParticles, AmbientParticle.Of(Id("warped_spore"), 0.01428f))
            .SetAttribute(EnvironmentAttributes.AmbientSounds,
                Ambient("warped_forest", "warped_forest", "warped_forest"))
            .MobSpawnSettings(mobs)
            .GenerationSettings(generation.Build())
            .Build();
    }

    //Ambient the loop/mood/additions sound group of one nether biome, maps to the AmbientSounds chains in vanilla
    private static AmbientSounds Ambient(string loop, string mood, string additions)
        => new(Optional<Identifier>.Of(Id($"ambient.{loop}.loop")),
            Optional<AmbientMoodSettings>.Of(new AmbientMoodSettings(Id($"ambient.{mood}.mood"), 6000, 8, 2.0)),
            new[] { new AmbientAdditionsSettings(Id($"ambient.{additions}.additions"), 0.0111) });

    private static Identifier Id(string path) => Identifier.WithDefaultNamespace(path);

    private static SpawnerData Spawn(string entity, int minCount, int maxCount)
        => new(Id(entity), minCount, maxCount, 0);
}
