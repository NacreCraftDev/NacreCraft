using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.Data.WorldGen.Placement;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Registry;

namespace NacreCraft.Game.Data.WorldGen.Biomes;

//BiomeDefaultFeatures the shared feature and spawn groups biome data classes add, maps to vanilla net.minecraft.data.worldgen.BiomeDefaultFeatures
//The generation step index is the GenerationStep.Decoration ordinal, matching the biome features array layout
public static class BiomeDefaultFeatures
{
    private const int Lakes = (int)GenerationStep.Decoration.Lakes;
    private const int LocalModifications = (int)GenerationStep.Decoration.LocalModifications;
    private const int UndergroundStructures = (int)GenerationStep.Decoration.UndergroundStructures;
    private const int SurfaceStructures = (int)GenerationStep.Decoration.SurfaceStructures;
    private const int UndergroundOres = (int)GenerationStep.Decoration.UndergroundOres;
    private const int UndergroundDecoration = (int)GenerationStep.Decoration.UndergroundDecoration;
    private const int FluidSprings = (int)GenerationStep.Decoration.FluidSprings;
    private const int VegetalDecoration = (int)GenerationStep.Decoration.VegetalDecoration;
    private const int TopLayerModification = (int)GenerationStep.Decoration.TopLayerModification;

    public static void AddDefaultCarversAndLakes(BiomeGenerationSettings.Builder builder)
    {
        builder.AddCarver(Carvers.Cave).AddCarver(Carvers.CaveExtraUnderground).AddCarver(Carvers.Canyon);
        F(builder, Lakes, MiscOverworldPlacements.LakeLavaUnderground, MiscOverworldPlacements.LakeLavaSurface);
    }

    public static void AddDefaultMonsterRoom(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundStructures, CavePlacements.MonsterRoom, CavePlacements.MonsterRoomDeep);

    public static void AddDefaultUndergroundVariety(BiomeGenerationSettings.Builder builder)
    {
        F(builder, UndergroundOres, OrePlacements.OreDirt, OrePlacements.OreGravel, OrePlacements.OreGraniteUpper,
            OrePlacements.OreGraniteLower, OrePlacements.OreDioriteUpper, OrePlacements.OreDioriteLower,
            OrePlacements.OreAndesiteUpper, OrePlacements.OreAndesiteLower, OrePlacements.OreTuff);
        F(builder, VegetalDecoration, CavePlacements.GlowLichen);
    }

    public static void AddDripstone(BiomeGenerationSettings.Builder builder)
    {
        F(builder, LocalModifications, CavePlacements.LargeDripstone);
        F(builder, UndergroundDecoration, CavePlacements.DripstoneCluster, CavePlacements.PointedDripstone);
    }

    public static void AddSculk(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundDecoration, CavePlacements.SculkVein, CavePlacements.SculkPatchDeepDark);

    public static void AddDefaultOres(BiomeGenerationSettings.Builder builder) => AddDefaultOres(builder, false);

    public static void AddDefaultOres(BiomeGenerationSettings.Builder builder, bool largeCopperBlobs)
    {
        F(builder, UndergroundOres, OrePlacements.OreCoalUpper, OrePlacements.OreCoalLower, OrePlacements.OreIronUpper,
            OrePlacements.OreIronMiddle, OrePlacements.OreIronSmall, OrePlacements.OreGold, OrePlacements.OreGoldLower,
            OrePlacements.OreRedstone, OrePlacements.OreRedstoneLower, OrePlacements.OreDiamond,
            OrePlacements.OreDiamondMedium, OrePlacements.OreDiamondLarge, OrePlacements.OreDiamondBuried,
            OrePlacements.OreLapis, OrePlacements.OreLapisBuried);
        F(builder, UndergroundOres, largeCopperBlobs ? OrePlacements.OreCopperLarge : OrePlacements.OreCopper);
        F(builder, UndergroundOres, CavePlacements.UnderwaterMagma);
    }

    public static void AddExtraGold(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundOres, OrePlacements.OreGoldExtra);

    public static void AddExtraEmeralds(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundOres, OrePlacements.OreEmerald);

    public static void AddInfestedStone(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundDecoration, OrePlacements.OreInfested);

    public static void AddDefaultSoftDisks(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundOres, MiscOverworldPlacements.DiskSand, MiscOverworldPlacements.DiskClay,
            MiscOverworldPlacements.DiskGravel);

    public static void AddSwampClayDisk(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundOres, MiscOverworldPlacements.DiskClay);

    public static void AddMangroveSwampDisks(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundOres, MiscOverworldPlacements.DiskGrass, MiscOverworldPlacements.DiskClay);

    public static void AddMossyStoneBlock(BiomeGenerationSettings.Builder builder)
        => F(builder, LocalModifications, MiscOverworldPlacements.ForestRock);

    public static void AddFerns(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchLargeFern);

    public static void AddBushes(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchBush);

    public static void AddRareBerryBushes(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchBerryRare);

    public static void AddCommonBerryBushes(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchBerryCommon);

    public static void AddLightBambooVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.BambooLight);

    public static void AddBambooVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.Bamboo, VegetationPlacements.BambooVegetation);

    public static void AddTaigaTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesTaiga);

    public static void AddGroveTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesGrove);

    public static void AddWaterTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesWater);

    public static void AddBirchTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesBirch);

    public static void AddOtherBirchTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesBirchAndOakLeafLitter);

    public static void AddTallBirchTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.BirchTall);

    public static void AddBirchForestFlowers(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.WildflowersBirchForest);

    public static void AddSavannaTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesSavanna);

    public static void AddShatteredSavannaTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesWindsweptSavanna);

    public static void AddLushCavesVegetationFeatures(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, CavePlacements.LushCavesCeilingVegetation, CavePlacements.CaveVines,
            CavePlacements.LushCavesClay, CavePlacements.LushCavesVegetation, CavePlacements.RootedAzaleaTree,
            CavePlacements.SporeBlossom, CavePlacements.ClassicVines);

    public static void AddSulfurCavesFeatures(BiomeGenerationSettings.Builder builder)
    {
        F(builder, Lakes, CavePlacements.RootedSulfurSpring, MiscOverworldPlacements.SulfurPool);
        F(builder, UndergroundDecoration, CavePlacements.SulfurSpikeCluster, CavePlacements.SulfurSpike);
    }

    public static void AddLushCavesSpecialOres(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundOres, OrePlacements.OreClay);

    public static void AddMountainTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesWindsweptHills);

    public static void AddMountainForestTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesWindsweptForest);

    public static void AddJungleTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesJungle);

    public static void AddSparseJungleTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesSparseJungle);

    public static void AddBadlandsTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesBadlands);

    public static void AddSnowyTrees(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesSnowy);

    public static void AddJungleGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassJungle);

    public static void AddSavannaGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchTallGrass);

    public static void AddShatteredSavannaGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassNormal);

    public static void AddSavannaExtraGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassSavanna);

    public static void AddBadlandGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassBadlands,
            VegetationPlacements.PatchDryGrassBadlands, VegetationPlacements.PatchDeadBushBadlands);

    public static void AddForestFlowers(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.ForestFlowers);

    public static void AddForestGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassForest);

    public static void AddSwampVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesSwamp, VegetationPlacements.FlowerSwamp,
            VegetationPlacements.PatchGrassNormal, VegetationPlacements.PatchDeadBush,
            VegetationPlacements.PatchWaterlily, VegetationPlacements.BrownMushroomSwamp,
            VegetationPlacements.RedMushroomSwamp);

    public static void AddMangroveSwampVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesMangrove, VegetationPlacements.PatchGrassNormal,
            VegetationPlacements.PatchDeadBush, VegetationPlacements.PatchWaterlily);

    public static void AddMushroomFieldVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.MushroomIslandVegetation,
            VegetationPlacements.BrownMushroomTaiga, VegetationPlacements.RedMushroomTaiga);

    public static void AddPlainVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.TreesPlains, VegetationPlacements.FlowerPlains,
            VegetationPlacements.PatchGrassPlain);

    public static void AddDesertVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchDryGrassDesert,
            VegetationPlacements.PatchDeadBush2);

    public static void AddGiantTaigaVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassTaiga, VegetationPlacements.PatchDeadBush,
            VegetationPlacements.BrownMushroomOldGrowth, VegetationPlacements.RedMushroomOldGrowth);

    public static void AddDefaultFlowers(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.FlowerDefault);

    public static void AddCherryGroveVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassPlain, VegetationPlacements.FlowerCherry,
            VegetationPlacements.TreesCherry);

    public static void AddMeadowVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassMeadow, VegetationPlacements.FlowerMeadow,
            VegetationPlacements.TreesMeadow, VegetationPlacements.WildflowersMeadow);

    public static void AddWarmFlowers(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.FlowerWarm);

    public static void AddDefaultGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassBadlands);

    public static void AddTaigaGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchGrassTaiga2,
            VegetationPlacements.BrownMushroomTaiga, VegetationPlacements.RedMushroomTaiga);

    public static void AddPlainGrass(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchTallGrass2);

    public static void AddDefaultMushrooms(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.BrownMushroomNormal,
            VegetationPlacements.RedMushroomNormal);

    public static void AddDefaultExtraVegetation(BiomeGenerationSettings.Builder builder,
        bool shouldGenerateNearWaterVegetation)
    {
        F(builder, VegetalDecoration, VegetationPlacements.PatchPumpkin);
        if (shouldGenerateNearWaterVegetation) AddNearWaterVegetation(builder);
    }

    public static void AddNearWaterVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchSugarCane,
            VegetationPlacements.PatchFireflyBushNearWater);

    public static void AddLeafLitterPatch(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchLeafLitter);

    public static void AddBadlandExtraVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchSugarCaneBadlands,
            VegetationPlacements.PatchPumpkin, VegetationPlacements.PatchCactusDecorated,
            VegetationPlacements.PatchFireflyBushNearWater);

    public static void AddJungleMelons(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchMelon);

    public static void AddSparseJungleMelons(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchMelonSparse);

    public static void AddJungleVines(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.Vines);

    public static void AddDesertExtraVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchSugarCaneDesert,
            VegetationPlacements.PatchPumpkin, VegetationPlacements.PatchCactusDesert);

    public static void AddSwampExtraVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, VegetationPlacements.PatchSugarCaneSwamp,
            VegetationPlacements.PatchPumpkin, VegetationPlacements.PatchFireflyBushSwamp,
            VegetationPlacements.PatchFireflyBushNearWaterSwamp);

    public static void AddMangroveSwampExtraVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, AquaticPlacements.SeagrassSwamp,
            VegetationPlacements.PatchFireflyBushNearWater);

    public static void AddDesertExtraDecoration(BiomeGenerationSettings.Builder builder)
        => F(builder, SurfaceStructures, MiscOverworldPlacements.DesertWell);

    public static void AddFossilDecoration(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundStructures, CavePlacements.FossilUpper, CavePlacements.FossilLower);

    public static void AddColdOceanExtraVegetation(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, AquaticPlacements.KelpCold);

    public static void AddLukeWarmKelp(BiomeGenerationSettings.Builder builder)
        => F(builder, VegetalDecoration, AquaticPlacements.KelpWarm);

    public static void AddDefaultSprings(BiomeGenerationSettings.Builder builder)
        => F(builder, FluidSprings, MiscOverworldPlacements.SpringWater, MiscOverworldPlacements.SpringLava);

    public static void AddFrozenSprings(BiomeGenerationSettings.Builder builder)
        => F(builder, FluidSprings, MiscOverworldPlacements.SpringLavaFrozen);

    public static void AddIcebergs(BiomeGenerationSettings.Builder builder)
        => F(builder, LocalModifications, MiscOverworldPlacements.IcebergPacked, MiscOverworldPlacements.IcebergBlue);

    public static void AddBlueIce(BiomeGenerationSettings.Builder builder)
        => F(builder, SurfaceStructures, MiscOverworldPlacements.BlueIce);

    public static void AddSurfaceFreezing(BiomeGenerationSettings.Builder builder)
        => F(builder, TopLayerModification, MiscOverworldPlacements.FreezeTopLayer);

    public static void AddNetherDefaultOres(BiomeGenerationSettings.Builder builder)
    {
        F(builder, UndergroundDecoration, OrePlacements.OreGravelNether, OrePlacements.OreBlackstone,
            OrePlacements.OreGoldNether, OrePlacements.OreQuartzNether);
        AddAncientDebris(builder);
    }

    public static void AddAncientDebris(BiomeGenerationSettings.Builder builder)
        => F(builder, UndergroundDecoration, OrePlacements.OreAncientDebrisLarge,
            OrePlacements.OreAncientDebrisSmall);

    public static void AddDefaultCrystalFormations(BiomeGenerationSettings.Builder builder)
        => F(builder, LocalModifications, CavePlacements.AmethystGeode);

    public static void FarmAnimals(MobSpawnSettings.Builder builder)
    {
        S(builder, MobCategory.Creature, 12, "sheep", 4, 4);
        S(builder, MobCategory.Creature, 10, "pig", 4, 4);
        S(builder, MobCategory.Creature, 10, "chicken", 4, 4);
        S(builder, MobCategory.Creature, 8, "cow", 4, 4);
    }

    public static void CaveSpawns(MobSpawnSettings.Builder builder)
    {
        S(builder, MobCategory.Ambient, 10, "bat", 8, 8);
        S(builder, MobCategory.UndergroundWaterCreature, 10, "glow_squid", 4, 6);
    }

    public static void CommonSpawns(MobSpawnSettings.Builder builder) => CommonSpawns(builder, 100);

    public static void CommonSpawns(MobSpawnSettings.Builder builder, int skeletonWeight)
    {
        CaveSpawns(builder);
        Monsters(builder, 95, 5, 0, skeletonWeight, false);
    }

    public static void CommonSpawnWithZombieHorse(MobSpawnSettings.Builder builder)
    {
        CaveSpawns(builder);
        Monsters(builder, 90, 5, 5, 100, false);
    }

    public static void SwampSpawns(MobSpawnSettings.Builder builder, int swampSkeletonWeight)
    {
        CommonSpawns(builder, swampSkeletonWeight);
        S(builder, MobCategory.Monster, 1, "slime", 1, 1);
        S(builder, MobCategory.Monster, 30, "bogged", 4, 4);
        S(builder, MobCategory.Creature, 10, "frog", 2, 5);
    }

    public static void OceanSpawns(MobSpawnSettings.Builder builder, int squidProbabilityWeight,
        int squidMaxCount, int codProbabilityWeight)
    {
        S(builder, MobCategory.WaterCreature, squidProbabilityWeight, "squid", 1, squidMaxCount);
        S(builder, MobCategory.WaterAmbient, codProbabilityWeight, "cod", 3, 6);
        CommonSpawns(builder);
        S(builder, MobCategory.Monster, 5, "drowned", 1, 1);
    }

    public static void WarmOceanSpawns(MobSpawnSettings.Builder builder, int squidProbabilityWeight,
        int squidMinCount)
    {
        S(builder, MobCategory.WaterCreature, squidProbabilityWeight, "squid", squidMinCount, 4);
        S(builder, MobCategory.WaterAmbient, 25, "tropical_fish", 8, 8);
        S(builder, MobCategory.WaterCreature, 2, "dolphin", 1, 2);
        S(builder, MobCategory.Monster, 5, "drowned", 1, 1);
        CommonSpawns(builder);
    }

    public static void PlainsSpawns(MobSpawnSettings.Builder builder)
    {
        FarmAnimals(builder);
        S(builder, MobCategory.Creature, 5, "horse", 2, 6);
        S(builder, MobCategory.Creature, 1, "donkey", 1, 3);
        CommonSpawnWithZombieHorse(builder);
    }

    public static void SnowySpawns(MobSpawnSettings.Builder builder, bool spawnZombieHorse)
    {
        S(builder, MobCategory.Creature, 10, "rabbit", 2, 3);
        S(builder, MobCategory.Creature, 1, "polar_bear", 1, 2);
        CaveSpawns(builder);
        Monsters(builder, spawnZombieHorse ? 90 : 95, 5, spawnZombieHorse ? 5 : 0, 20, false);
        S(builder, MobCategory.Monster, 80, "stray", 4, 4);
    }

    public static void DesertSpawns(MobSpawnSettings.Builder builder)
    {
        S(builder, MobCategory.Creature, 12, "rabbit", 2, 3);
        S(builder, MobCategory.Creature, 1, "camel", 1, 1);
        CaveSpawns(builder);
        Monsters(builder, 19, 1, 0, 50, false);
        S(builder, MobCategory.Monster, 80, "husk", 4, 4);
        S(builder, MobCategory.Monster, 50, "parched", 4, 4);
    }

    public static void DripstoneCavesSpawns(MobSpawnSettings.Builder builder)
    {
        CaveSpawns(builder);
        Monsters(builder, 95, 5, 0, 100, false);
        S(builder, MobCategory.Monster, 95, "drowned", 4, 4);
    }

    public static void Monsters(MobSpawnSettings.Builder builder, int zombieWeight, int zombieVillagerWeight,
        int zombieHorseWeight, int skeletonWeight, bool drownedZombies)
    {
        S(builder, MobCategory.Monster, 100, "spider", 4, 4);
        S(builder, MobCategory.Monster, zombieWeight, drownedZombies ? "drowned" : "zombie", 4, 4);
        S(builder, MobCategory.Monster, zombieVillagerWeight, "zombie_villager", 1, 1);
        if (zombieHorseWeight > 0) S(builder, MobCategory.Monster, zombieHorseWeight, "zombie_horse", 1, 1);
        S(builder, MobCategory.Monster, skeletonWeight, "skeleton", 4, 4);
        S(builder, MobCategory.Monster, 100, "creeper", 4, 4);
        S(builder, MobCategory.Monster, 100, "slime", 4, 4);
        S(builder, MobCategory.Monster, 10, "enderman", 1, 4);
        S(builder, MobCategory.Monster, 5, "witch", 1, 1);
    }

    public static void MooshroomSpawns(MobSpawnSettings.Builder builder)
    {
        S(builder, MobCategory.Creature, 8, "mooshroom", 4, 8);
        CaveSpawns(builder);
    }

    public static void BaseJungleSpawns(MobSpawnSettings.Builder builder)
    {
        FarmAnimals(builder);
        S(builder, MobCategory.Creature, 10, "chicken", 4, 4);
        CommonSpawns(builder);
    }

    public static void EndSpawns(MobSpawnSettings.Builder builder)
        => S(builder, MobCategory.Monster, 10, "enderman", 4, 4);

    //F adds several placements at one generation step, maps to the repeated addFeature calls
    private static void F(BiomeGenerationSettings.Builder builder, int step,
        params ResourceKey<Registry.PlacedFeature>[] keys)
    {
        foreach (var key in keys) builder.AddFeature(step, key);
    }

    //S adds one spawn entry, maps to addSpawn(category, weight, new SpawnerData(...))
    private static void S(MobSpawnSettings.Builder builder, MobCategory category, int weight, string entity,
        int minCount, int maxCount)
        => builder.AddSpawn(category, weight, Spawn(entity, minCount, maxCount));

    //Spawn one spawner entry by entity path
    public static SpawnerData Spawn(string entity, int minCount, int maxCount)
        => new(Identifier.WithDefaultNamespace(entity), minCount, maxCount, 0);
}
