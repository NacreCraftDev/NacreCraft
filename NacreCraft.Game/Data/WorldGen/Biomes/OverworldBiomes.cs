using NacreCraft.Codec;
using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Environment;
using NacreCraft.Util;

namespace NacreCraft.Game.Data.WorldGen.Biomes;

//OverworldBiomes built-in overworld biomes, maps to vanilla net.minecraft.data.worldgen.biome.OverworldBiomes
//Music entries use registry ids because the attribute settings here store identifiers instead of sound instances
public static class OverworldBiomes
{
    public const int NormalWaterColor = 4159204;
    private const int DarkDryFoliageColor = 8082228;
    public const int SwampSkeletonWeight = 70;

    //OverworldMusic the overworld default/creative music pair, maps to vanilla Musics.GAME/CREATIVE
    private static readonly BackgroundMusic OverworldMusic = new(
        Optional<Music>.Of(MusicOf("music.overworld.game")),
        Optional<Music>.Of(MusicOf("music.creative")),
        Optional<Music>.Empty());

    //UnderWaterMusic the underwater music, maps to vanilla Musics.UNDER_WATER
    private static readonly Music UnderWaterMusic = MusicOf("music.under_water");

    public static int CalculateSkyColor(float temperature)
    {
        var temp = Mth.Clamp(temperature / 3.0f, -1.0f, 1.0f);
        return Argb.Color(255, Mth.HsvToRgb(0.62222224f - temp * 0.05f, 0.5f + temp * 0.1f, 1.0f));
    }

    private static Biome.BiomeBuilder BaseBiome(float temperature, float downfall)
        => new Biome.BiomeBuilder()
            .HasPrecipitation(true)
            .Temperature(temperature)
            .Downfall(downfall)
            .SetAttribute(EnvironmentAttributes.SkyColor, CalculateSkyColor(temperature))
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(NormalWaterColor).Build());

    private static void GlobalOverworldGeneration(BiomeGenerationSettings.Builder generation)
    {
        BiomeDefaultFeatures.AddDefaultCarversAndLakes(generation);
        BiomeDefaultFeatures.AddDefaultCrystalFormations(generation);
        BiomeDefaultFeatures.AddDefaultMonsterRoom(generation);
        BiomeDefaultFeatures.AddDefaultUndergroundVariety(generation);
        BiomeDefaultFeatures.AddDefaultSprings(generation);
        BiomeDefaultFeatures.AddSurfaceFreezing(generation);
    }

    public static Biome OldGrowthTaiga(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool spruce)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.FarmAnimals(mobs);
        S(mobs, MobCategory.Creature, 8, "wolf", 4, 4);
        S(mobs, MobCategory.Creature, 4, "rabbit", 2, 3);
        S(mobs, MobCategory.Creature, 8, "fox", 2, 4);
        if (spruce) BiomeDefaultFeatures.CommonSpawns(mobs);
        else
        {
            BiomeDefaultFeatures.CaveSpawns(mobs);
            BiomeDefaultFeatures.Monsters(mobs, 100, 25, 0, 100, false);
        }
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddMossyStoneBlock(generation);
        BiomeDefaultFeatures.AddFerns(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
            spruce ? VegetationPlacements.TreesOldGrowthSpruceTaiga : VegetationPlacements.TreesOldGrowthPineTaiga);
        BiomeDefaultFeatures.AddDefaultFlowers(generation);
        BiomeDefaultFeatures.AddGiantTaigaVegetation(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        BiomeDefaultFeatures.AddCommonBerryBushes(generation);
        return BaseBiome(spruce ? 0.25f : 0.3f, 0.8f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.old_growth_taiga"))
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome SparseJungle(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.BaseJungleSpawns(mobs);
        S(mobs, MobCategory.Creature, 8, "wolf", 2, 4);
        return BaseJungle(placedFeatures, carvers, 0.8f, false, true, false)
            .MobSpawnSettings(mobs.Build())
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.sparse_jungle"))
            .Build();
    }

    public static Biome Jungle(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.BaseJungleSpawns(mobs);
        S(mobs, MobCategory.Creature, 40, "parrot", 1, 2);
        S(mobs, MobCategory.Monster, 2, "ocelot", 1, 3);
        S(mobs, MobCategory.Creature, 1, "panda", 1, 2);
        return BaseJungle(placedFeatures, carvers, 0.9f, false, false, true)
            .MobSpawnSettings(mobs.Build())
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.jungle"))
            .SetAttribute(EnvironmentAttributes.IncreasedFireBurnout, true)
            .Build();
    }

    public static Biome BambooJungle(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.BaseJungleSpawns(mobs);
        S(mobs, MobCategory.Creature, 40, "parrot", 1, 2);
        S(mobs, MobCategory.Creature, 80, "panda", 1, 2);
        S(mobs, MobCategory.Monster, 2, "ocelot", 1, 1);
        return BaseJungle(placedFeatures, carvers, 0.9f, true, false, true)
            .MobSpawnSettings(mobs.Build())
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.bamboo_jungle"))
            .SetAttribute(EnvironmentAttributes.IncreasedFireBurnout, true)
            .Build();
    }

    private static Biome.BiomeBuilder BaseJungle(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, float downfall, bool bamboo, bool sparse, bool core)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        if (bamboo) BiomeDefaultFeatures.AddBambooVegetation(generation);
        else
        {
            if (core) BiomeDefaultFeatures.AddLightBambooVegetation(generation);
            if (sparse) BiomeDefaultFeatures.AddSparseJungleTrees(generation);
            else BiomeDefaultFeatures.AddJungleTrees(generation);
        }
        BiomeDefaultFeatures.AddWarmFlowers(generation);
        BiomeDefaultFeatures.AddJungleGrass(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        BiomeDefaultFeatures.AddJungleVines(generation);
        if (sparse) BiomeDefaultFeatures.AddSparseJungleMelons(generation);
        else BiomeDefaultFeatures.AddJungleMelons(generation);
        return BaseBiome(0.95f, downfall).GenerationSettings(generation.Build());
    }

    public static Biome WindsweptHills(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool moreTrees)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.FarmAnimals(mobs);
        S(mobs, MobCategory.Creature, 5, "llama", 4, 6);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        if (moreTrees) BiomeDefaultFeatures.AddMountainForestTrees(generation);
        else BiomeDefaultFeatures.AddMountainTrees(generation);
        BiomeDefaultFeatures.AddBushes(generation);
        BiomeDefaultFeatures.AddDefaultFlowers(generation);
        BiomeDefaultFeatures.AddDefaultGrass(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        BiomeDefaultFeatures.AddExtraEmeralds(generation);
        BiomeDefaultFeatures.AddInfestedStone(generation);
        return BaseBiome(0.2f, 0.3f).MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Desert(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.DesertSpawns(mobs);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        BiomeDefaultFeatures.AddFossilDecoration(generation);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddDefaultFlowers(generation);
        BiomeDefaultFeatures.AddDefaultGrass(generation);
        BiomeDefaultFeatures.AddDesertVegetation(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDesertExtraVegetation(generation);
        BiomeDefaultFeatures.AddDesertExtraDecoration(generation);
        return BaseBiome(2.0f, 0.0f).HasPrecipitation(false)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.desert"))
            .SetAttribute(EnvironmentAttributes.SnowGolemMelts, true)
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Plains(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool sunflower, bool snowy, bool spikes)
    {
        var mobs = new MobSpawnSettings.Builder();
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        if (snowy)
        {
            mobs.CreatureSpawnProbability(0.07f);
            BiomeDefaultFeatures.SnowySpawns(mobs, !spikes);
            if (spikes)
            {
                generation.AddFeature((int)GenerationStep.Decoration.SurfaceStructures, MiscOverworldPlacements.IceSpike);
                generation.AddFeature((int)GenerationStep.Decoration.SurfaceStructures, MiscOverworldPlacements.IcePatch);
            }
        }
        else
        {
            BiomeDefaultFeatures.PlainsSpawns(mobs);
            BiomeDefaultFeatures.AddPlainGrass(generation);
            if (sunflower)
                generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
                    VegetationPlacements.PatchSunflower);
            else BiomeDefaultFeatures.AddBushes(generation);
        }
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        if (snowy)
        {
            BiomeDefaultFeatures.AddSnowyTrees(generation);
            BiomeDefaultFeatures.AddDefaultFlowers(generation);
            BiomeDefaultFeatures.AddDefaultGrass(generation);
        }
        else BiomeDefaultFeatures.AddPlainVegetation(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        return BaseBiome(snowy ? 0.0f : 0.8f, snowy ? 0.5f : 0.4f)
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome MushroomFields(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.MooshroomSpawns(mobs);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddMushroomFieldVegetation(generation);
        BiomeDefaultFeatures.AddNearWaterVegetation(generation);
        return BaseBiome(0.9f, 1.0f)
            .SetAttribute(EnvironmentAttributes.IncreasedFireBurnout, true)
            .SetAttribute(EnvironmentAttributes.CanPillagerPatrolSpawn, false)
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Savanna(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool shattered, bool plateau)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        if (!shattered) BiomeDefaultFeatures.AddSavannaGrass(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        if (shattered)
        {
            BiomeDefaultFeatures.AddShatteredSavannaTrees(generation);
            BiomeDefaultFeatures.AddDefaultFlowers(generation);
            BiomeDefaultFeatures.AddShatteredSavannaGrass(generation);
        }
        else
        {
            BiomeDefaultFeatures.AddSavannaTrees(generation);
            BiomeDefaultFeatures.AddWarmFlowers(generation);
            BiomeDefaultFeatures.AddSavannaExtraGrass(generation);
        }
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.FarmAnimals(mobs);
        S(mobs, MobCategory.Creature, 1, "horse", 2, 6);
        S(mobs, MobCategory.Creature, 1, "donkey", 1, 1);
        S(mobs, MobCategory.Creature, 10, "armadillo", 2, 3);
        BiomeDefaultFeatures.CommonSpawnWithZombieHorse(mobs);
        if (plateau)
        {
            S(mobs, MobCategory.Creature, 8, "llama", 4, 4);
            S(mobs, MobCategory.Creature, 8, "wolf", 4, 8);
        }
        return BaseBiome(2.0f, 0.0f).HasPrecipitation(false)
            .SetAttribute(EnvironmentAttributes.SnowGolemMelts, true)
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Badlands(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool wooded)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.FarmAnimals(mobs);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        S(mobs, MobCategory.Creature, 6, "armadillo", 1, 2);
        mobs.CreatureSpawnProbability(0.03f);
        if (wooded)
        {
            S(mobs, MobCategory.Creature, 2, "wolf", 4, 8);
            mobs.CreatureSpawnProbability(0.04f);
        }
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddExtraGold(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        if (wooded) BiomeDefaultFeatures.AddBadlandsTrees(generation);
        BiomeDefaultFeatures.AddBadlandGrass(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddBadlandExtraVegetation(generation);
        return BaseBiome(2.0f, 0.0f).HasPrecipitation(false)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.badlands"))
            .SetAttribute(EnvironmentAttributes.SnowGolemMelts, true)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(NormalWaterColor)
                .FoliageColor(10387789).GrassColor(9470285).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    private static Biome.BiomeBuilder BaseOcean()
        => BaseBiome(0.5f, 0.5f).SetAttribute(EnvironmentAttributes.BackgroundMusic,
            OverworldMusic.WithUnderwater(UnderWaterMusic));

    private static BiomeGenerationSettings.Builder BaseOceanGeneration(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddWaterTrees(generation);
        BiomeDefaultFeatures.AddDefaultFlowers(generation);
        BiomeDefaultFeatures.AddDefaultGrass(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        return generation;
    }

    public static Biome ColdOcean(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool deep)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.OceanSpawns(mobs, 3, 4, 15);
        S(mobs, MobCategory.WaterAmbient, 15, "salmon", 1, 5);
        S(mobs, MobCategory.WaterCreature, 2, "nautilus", 1, 1);
        var generation = BaseOceanGeneration(placedFeatures, carvers);
        generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
            deep ? AquaticPlacements.SeagrassDeepCold : AquaticPlacements.SeagrassCold);
        BiomeDefaultFeatures.AddColdOceanExtraVegetation(generation);
        return BaseOcean().SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(4020182).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Ocean(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool deep)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.OceanSpawns(mobs, 1, 4, 10);
        S(mobs, MobCategory.WaterCreature, 1, "dolphin", 1, 2);
        S(mobs, MobCategory.WaterCreature, 5, "nautilus", 1, 1);
        var generation = BaseOceanGeneration(placedFeatures, carvers);
        generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
            deep ? AquaticPlacements.SeagrassDeep : AquaticPlacements.SeagrassNormal);
        BiomeDefaultFeatures.AddColdOceanExtraVegetation(generation);
        return BaseOcean().MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome LukeWarmOcean(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool deep)
    {
        var mobs = new MobSpawnSettings.Builder();
        if (deep) BiomeDefaultFeatures.OceanSpawns(mobs, 8, 4, 8);
        else BiomeDefaultFeatures.OceanSpawns(mobs, 10, 2, 15);
        S(mobs, MobCategory.WaterAmbient, 5, "pufferfish", 1, 3);
        S(mobs, MobCategory.WaterAmbient, 25, "tropical_fish", 8, 8);
        S(mobs, MobCategory.WaterCreature, 2, "dolphin", 1, 2);
        S(mobs, MobCategory.WaterCreature, 5, "nautilus", 1, 1);
        var generation = BaseOceanGeneration(placedFeatures, carvers);
        generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
            deep ? AquaticPlacements.SeagrassDeepWarm : AquaticPlacements.SeagrassWarm);
        BiomeDefaultFeatures.AddLukeWarmKelp(generation);
        return BaseOcean().SetAttribute(EnvironmentAttributes.WaterFogColor, -16509389)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(4566514).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome WarmOcean(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.WaterAmbient, 15, "pufferfish", 1, 3);
        S(mobs, MobCategory.WaterCreature, 5, "nautilus", 1, 1);
        BiomeDefaultFeatures.WarmOceanSpawns(mobs, 10, 4);
        var generation = BaseOceanGeneration(placedFeatures, carvers)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, AquaticPlacements.WarmOceanVegetation)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, AquaticPlacements.SeagrassWarm)
            .AddFeature((int)GenerationStep.Decoration.VegetalDecoration, AquaticPlacements.SeaPickle);
        return BaseOcean().SetAttribute(EnvironmentAttributes.WaterFogColor, -16507085)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(4445678).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome FrozenOcean(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool deep)
    {
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.WaterCreature, 1, "squid", 1, 4);
        S(mobs, MobCategory.WaterAmbient, 15, "salmon", 1, 5);
        S(mobs, MobCategory.Creature, 1, "polar_bear", 1, 2);
        S(mobs, MobCategory.WaterCreature, 2, "nautilus", 1, 1);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        S(mobs, MobCategory.Monster, 5, "drowned", 1, 1);
        var temperature = deep ? 0.5f : 0.0f;
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        BiomeDefaultFeatures.AddIcebergs(generation);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddBlueIce(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddWaterTrees(generation);
        BiomeDefaultFeatures.AddDefaultFlowers(generation);
        BiomeDefaultFeatures.AddDefaultGrass(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        return BaseBiome(temperature, 0.5f).TemperatureAdjustment(NacreCraft.Registry.TemperatureModifier.Frozen)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(3750089).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Forest(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool birch, bool tall, bool flower)
    {
        BackgroundMusic music;
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        if (flower)
        {
            music = Music("music.overworld.flower_forest");
            generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
                VegetationPlacements.FlowerForestFlowers);
        }
        else
        {
            music = Music("music.overworld.forest");
            BiomeDefaultFeatures.AddForestFlowers(generation);
        }
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        if (flower)
        {
            generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
                VegetationPlacements.TreesFlowerForest);
            generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
                VegetationPlacements.FlowerFlowerForest);
            BiomeDefaultFeatures.AddDefaultGrass(generation);
        }
        else
        {
            if (birch)
            {
                BiomeDefaultFeatures.AddBirchForestFlowers(generation);
                if (tall) BiomeDefaultFeatures.AddTallBirchTrees(generation);
                else BiomeDefaultFeatures.AddBirchTrees(generation);
            }
            else BiomeDefaultFeatures.AddOtherBirchTrees(generation);
            BiomeDefaultFeatures.AddBushes(generation);
            BiomeDefaultFeatures.AddDefaultFlowers(generation);
            BiomeDefaultFeatures.AddForestGrass(generation);
        }
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.FarmAnimals(mobs);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        if (flower) S(mobs, MobCategory.Creature, 4, "rabbit", 2, 3);
        else if (!birch) S(mobs, MobCategory.Creature, 5, "wolf", 4, 4);
        return BaseBiome(birch ? 0.6f : 0.7f, birch ? 0.6f : 0.8f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, music)
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Taiga(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool snowy)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.FarmAnimals(mobs);
        S(mobs, MobCategory.Creature, 8, "wolf", 4, 4);
        S(mobs, MobCategory.Creature, 4, "rabbit", 2, 3);
        S(mobs, MobCategory.Creature, 8, "fox", 2, 4);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddFerns(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddTaigaTrees(generation);
        BiomeDefaultFeatures.AddDefaultFlowers(generation);
        BiomeDefaultFeatures.AddTaigaGrass(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        if (snowy) BiomeDefaultFeatures.AddRareBerryBushes(generation);
        else BiomeDefaultFeatures.AddCommonBerryBushes(generation);
        var waterColor = snowy ? 4020182 : NormalWaterColor;
        return BaseBiome(snowy ? -0.5f : 0.25f, snowy ? 0.4f : 0.8f)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(waterColor).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome DarkForest(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool isPaleGarden)
    {
        var mobs = new MobSpawnSettings.Builder();
        if (!isPaleGarden) BiomeDefaultFeatures.FarmAnimals(mobs);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
            isPaleGarden ? VegetationPlacements.PaleGardenVegetation : VegetationPlacements.DarkForestVegetation);
        if (!isPaleGarden) BiomeDefaultFeatures.AddForestFlowers(generation);
        else
        {
            generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
                VegetationPlacements.PaleMossPatch);
            generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
                VegetationPlacements.PaleGardenFlowers);
        }
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        if (!isPaleGarden) BiomeDefaultFeatures.AddDefaultFlowers(generation);
        else
            generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
                VegetationPlacements.FlowerPaleGarden);
        BiomeDefaultFeatures.AddForestGrass(generation);
        if (!isPaleGarden)
        {
            BiomeDefaultFeatures.AddDefaultMushrooms(generation);
            BiomeDefaultFeatures.AddLeafLitterPatch(generation);
        }
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        var paleGardenAttributes = EnvironmentAttributeMap.CreateBuilder()
            .Set(EnvironmentAttributes.SkyColor, -4605511)
            .Set(EnvironmentAttributes.FogColor, -8292496)
            .Set(EnvironmentAttributes.WaterFogColor, -11179648)
            .Set(EnvironmentAttributes.BackgroundMusic, BackgroundMusic.Empty)
            .Set(EnvironmentAttributes.MusicVolume, 0.0f)
            .Build();
        var darkForestAttributes = EnvironmentAttributeMap.CreateBuilder()
            .Set(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.forest"))
            .Build();
        var builder = BaseBiome(0.7f, 0.8f).PutAttributes(isPaleGarden ? paleGardenAttributes : darkForestAttributes);
        var effects = isPaleGarden
            ? new BiomeSpecialEffects.Builder().WaterColor(7768221).GrassColor(7832178).FoliageColor(8883574)
                .DryFoliageColor(10528412).Build()
            : new BiomeSpecialEffects.Builder().WaterColor(NormalWaterColor)
                .DryFoliageColor(DarkDryFoliageColor)
                .GrassColorModifier(NacreCraft.Registry.GrassColorModifier.DarkForest).Build();
        return builder.SpecialEffects(effects).MobSpawnSettings(mobs.Build())
            .GenerationSettings(generation.Build()).Build();
    }

    public static Biome Swamp(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.FarmAnimals(mobs);
        BiomeDefaultFeatures.SwampSpawns(mobs, SwampSkeletonWeight);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        BiomeDefaultFeatures.AddFossilDecoration(generation);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddSwampClayDisk(generation);
        BiomeDefaultFeatures.AddSwampVegetation(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddSwampExtraVegetation(generation);
        generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration, AquaticPlacements.SeagrassSwamp);
        return BaseBiome(0.8f, 0.9f)
            .SetAttribute(EnvironmentAttributes.WaterFogColor, -14474473)
            .Modify(EnvironmentAttributes.WaterFogEndDistance, FloatModifier.Multiply, 0.85f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.swamp"))
            .SetAttribute(EnvironmentAttributes.IncreasedFireBurnout, true)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(6388580).FoliageColor(6975545)
                .DryFoliageColor(DarkDryFoliageColor)
                .GrassColorModifier(NacreCraft.Registry.GrassColorModifier.Swamp).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome MangroveSwamp(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.SwampSpawns(mobs, SwampSkeletonWeight);
        S(mobs, MobCategory.WaterAmbient, 25, "tropical_fish", 8, 8);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        BiomeDefaultFeatures.AddFossilDecoration(generation);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddMangroveSwampDisks(generation);
        BiomeDefaultFeatures.AddMangroveSwampVegetation(generation);
        BiomeDefaultFeatures.AddMangroveSwampExtraVegetation(generation);
        return BaseBiome(0.8f, 0.9f)
            .SetAttribute(EnvironmentAttributes.FogColor, -4138753)
            .SetAttribute(EnvironmentAttributes.WaterFogColor, -11699616)
            .Modify(EnvironmentAttributes.WaterFogEndDistance, FloatModifier.Multiply, 0.85f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.swamp"))
            .SetAttribute(EnvironmentAttributes.IncreasedFireBurnout, true)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(3832426).FoliageColor(9285927)
                .DryFoliageColor(DarkDryFoliageColor)
                .GrassColorModifier(NacreCraft.Registry.GrassColorModifier.Swamp).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome River(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool frozen)
    {
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.WaterCreature, 2, "squid", 1, 4);
        S(mobs, MobCategory.WaterAmbient, 5, "salmon", 1, 5);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        S(mobs, MobCategory.Monster, frozen ? 1 : 100, "drowned", 1, 1);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddWaterTrees(generation);
        BiomeDefaultFeatures.AddBushes(generation);
        BiomeDefaultFeatures.AddDefaultFlowers(generation);
        BiomeDefaultFeatures.AddDefaultGrass(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        if (!frozen)
            generation.AddFeature((int)GenerationStep.Decoration.VegetalDecoration,
                AquaticPlacements.SeagrassRiver);
        return BaseBiome(frozen ? 0.0f : 0.5f, 0.5f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, OverworldMusic.WithUnderwater(UnderWaterMusic))
            .SpecialEffects(new BiomeSpecialEffects.Builder()
                .WaterColor(frozen ? 3750089 : NormalWaterColor).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Beach(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool snowy, bool stony)
    {
        var mobs = new MobSpawnSettings.Builder();
        var sandy = !stony && !snowy;
        if (sandy) S(mobs, MobCategory.Creature, 5, "turtle", 2, 5);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddDefaultFlowers(generation);
        BiomeDefaultFeatures.AddDefaultGrass(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, true);
        var temperature = snowy ? 0.05f : stony ? 0.2f : 0.8f;
        return BaseBiome(temperature, sandy ? 0.4f : 0.3f)
            .SpecialEffects(new BiomeSpecialEffects.Builder()
                .WaterColor(snowy ? 4020182 : NormalWaterColor).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome TheVoid(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        generation.AddFeature((int)GenerationStep.Decoration.TopLayerModification,
            MiscOverworldPlacements.VoidStartPlatform);
        return BaseBiome(0.5f, 0.5f).HasPrecipitation(false)
            .MobSpawnSettings(new MobSpawnSettings.Builder().Build())
            .GenerationSettings(generation.Build()).Build();
    }

    public static Biome MeadowOrCherryGrove(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers, bool cherryGrove)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.Creature, 1, cherryGrove ? "pig" : "donkey", 1, 2);
        S(mobs, MobCategory.Creature, 2, "rabbit", 2, 6);
        S(mobs, MobCategory.Creature, 2, "sheep", 2, 4);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddPlainGrass(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        if (cherryGrove) BiomeDefaultFeatures.AddCherryGroveVegetation(generation);
        else BiomeDefaultFeatures.AddMeadowVegetation(generation);
        BiomeDefaultFeatures.AddExtraEmeralds(generation);
        BiomeDefaultFeatures.AddInfestedStone(generation);
        if (cherryGrove)
        {
            var effects = new BiomeSpecialEffects.Builder().WaterColor(6141935).GrassColor(11983713)
                .FoliageColor(11983713);
            return BaseBiome(0.5f, 0.8f)
                .SetAttribute(EnvironmentAttributes.WaterFogColor, -10635281)
                .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.cherry_grove"))
                .SpecialEffects(effects.Build())
                .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
        }
        return BaseBiome(0.5f, 0.8f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.meadow"))
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(937679).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    private static Biome.BiomeBuilder BasePeaks(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.Creature, 5, "goat", 1, 3);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddFrozenSprings(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddExtraEmeralds(generation);
        BiomeDefaultFeatures.AddInfestedStone(generation);
        return BaseBiome(-0.7f, 0.9f)
            .SetAttribute(EnvironmentAttributes.IncreasedFireBurnout, true)
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build());
    }

    public static Biome FrozenPeaks(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
        => BasePeaks(placedFeatures, carvers)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.frozen_peaks")).Build();

    public static Biome JaggedPeaks(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
        => BasePeaks(placedFeatures, carvers)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.jagged_peaks")).Build();

    public static Biome StonyPeaks(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.CommonSpawns(mobs);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddExtraEmeralds(generation);
        BiomeDefaultFeatures.AddInfestedStone(generation);
        return BaseBiome(1.0f, 0.3f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.stony_peaks"))
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome SnowySlopes(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.Creature, 4, "rabbit", 2, 3);
        S(mobs, MobCategory.Creature, 5, "goat", 1, 3);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddFrozenSprings(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, false);
        BiomeDefaultFeatures.AddExtraEmeralds(generation);
        BiomeDefaultFeatures.AddInfestedStone(generation);
        return BaseBiome(-0.3f, 0.9f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.snowy_slopes"))
            .SetAttribute(EnvironmentAttributes.IncreasedFireBurnout, true)
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome Grove(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.Creature, 1, "wolf", 1, 1);
        S(mobs, MobCategory.Creature, 8, "rabbit", 2, 3);
        S(mobs, MobCategory.Creature, 4, "fox", 2, 4);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddFrozenSprings(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddGroveTrees(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, false);
        BiomeDefaultFeatures.AddExtraEmeralds(generation);
        BiomeDefaultFeatures.AddInfestedStone(generation);
        return BaseBiome(-0.2f, 0.8f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.grove"))
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome SulfurCaves(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.Ambient, 10, "bat", 8, 8);
        S(mobs, MobCategory.Monster, 100, "sulfur_cube", 2, 4);
        S(mobs, MobCategory.Monster, 50, "creeper", 2, 2);
        S(mobs, MobCategory.Monster, 50, "skeleton", 2, 2);
        S(mobs, MobCategory.Monster, 25, "slime", 1, 1);
        S(mobs, MobCategory.Monster, 20, "cave_spider", 1, 1);
        S(mobs, MobCategory.Monster, 50, "zombie", 2, 2);
        S(mobs, MobCategory.Monster, 10, "enderman", 1, 1);
        S(mobs, MobCategory.Monster, 1, "witch", 1, 1);
        S(mobs, MobCategory.Monster, 5, "zombie_villager", 1, 1);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddPlainGrass(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddSulfurCavesFeatures(generation);
        return BaseBiome(0.8f, 0.4f)
            .SetAttribute(EnvironmentAttributes.FogColor, -7555023)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.sulfur_caves"))
            .SetAttribute(EnvironmentAttributes.WaterFogColor, -15248324)
            .SpecialEffects(new BiomeSpecialEffects.Builder().WaterColor(-13320311).GrassColor(11249231).Build())
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome LushCaves(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        S(mobs, MobCategory.Axolotls, 10, "axolotl", 4, 6);
        S(mobs, MobCategory.WaterAmbient, 25, "tropical_fish", 8, 8);
        BiomeDefaultFeatures.CommonSpawns(mobs);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddPlainGrass(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddLushCavesSpecialOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddLushCavesVegetationFeatures(generation);
        return BaseBiome(0.5f, 0.5f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.lush_caves"))
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome DripstoneCaves(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var mobs = new MobSpawnSettings.Builder();
        BiomeDefaultFeatures.DripstoneCavesSpawns(mobs);
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        GlobalOverworldGeneration(generation);
        BiomeDefaultFeatures.AddPlainGrass(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation, true);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddPlainVegetation(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, false);
        BiomeDefaultFeatures.AddDripstone(generation);
        return BaseBiome(0.8f, 0.4f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.dripstone_caves"))
            .MobSpawnSettings(mobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    public static Biome DeepDark(HolderLookup<PlacedFeature> placedFeatures,
        HolderLookup<ConfiguredWorldCarver> carvers)
    {
        var noMobs = new MobSpawnSettings.Builder();
        var generation = new BiomeGenerationSettings.Builder(placedFeatures, carvers);
        generation.AddCarver(Carvers.Cave).AddCarver(Carvers.CaveExtraUnderground).AddCarver(Carvers.Canyon);
        BiomeDefaultFeatures.AddDefaultCrystalFormations(generation);
        BiomeDefaultFeatures.AddDefaultMonsterRoom(generation);
        BiomeDefaultFeatures.AddDefaultUndergroundVariety(generation);
        BiomeDefaultFeatures.AddSurfaceFreezing(generation);
        BiomeDefaultFeatures.AddPlainGrass(generation);
        BiomeDefaultFeatures.AddDefaultOres(generation);
        BiomeDefaultFeatures.AddDefaultSoftDisks(generation);
        BiomeDefaultFeatures.AddPlainVegetation(generation);
        BiomeDefaultFeatures.AddDefaultMushrooms(generation);
        BiomeDefaultFeatures.AddDefaultExtraVegetation(generation, false);
        BiomeDefaultFeatures.AddSculk(generation);
        return BaseBiome(0.8f, 0.4f)
            .SetAttribute(EnvironmentAttributes.BackgroundMusic, Music("music.overworld.deep_dark"))
            .MobSpawnSettings(noMobs.Build()).GenerationSettings(generation.Build()).Build();
    }

    //Music the background music of one biome, maps to new BackgroundMusic(SoundEvents.<biome music>)
    private static BackgroundMusic Music(string path) => new(MusicOf(path));

    private static Music MusicOf(string path)
        => new(Identifier.WithDefaultNamespace(path), 12000, 24000, false);

    //S adds one spawn entry, maps to addSpawn(category, weight, new SpawnerData(...))
    private static void S(MobSpawnSettings.Builder builder, MobCategory category, int weight, string entity,
        int minCount, int maxCount)
        => builder.AddSpawn(category, weight,
            new SpawnerData(Identifier.WithDefaultNamespace(entity), minCount, maxCount, 0));
}
