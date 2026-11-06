using NacreCraft.Registry;

namespace NacreCraft.Game.Data.WorldGen.Biomes;

//BiomeData registers every built-in biome, maps to vanilla net.minecraft.data.worldgen.biome.BiomeData
//The overworld, nether and end factories do the actual assembly; this class only supplies the registry keys
public static class BiomeData
{
    public static void Bootstrap(BootstrapContext<Biome> context)
    {
        var placed = context.LookupOrThrow(Registries.PLACED_FEATURE);
        var carvers = context.LookupOrThrow(Registries.CONFIGURED_CARVER);
        context.Register(NacreCraft.Registry.Biomes.THE_VOID, OverworldBiomes.TheVoid(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.PLAINS, OverworldBiomes.Plains(placed, carvers, false, false, false));
        context.Register(NacreCraft.Registry.Biomes.SUNFLOWER_PLAINS, OverworldBiomes.Plains(placed, carvers, true, false, false));
        context.Register(NacreCraft.Registry.Biomes.SNOWY_PLAINS, OverworldBiomes.Plains(placed, carvers, false, true, false));
        context.Register(NacreCraft.Registry.Biomes.ICE_SPIKES, OverworldBiomes.Plains(placed, carvers, false, true, true));
        context.Register(NacreCraft.Registry.Biomes.DESERT, OverworldBiomes.Desert(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.SWAMP, OverworldBiomes.Swamp(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.MANGROVE_SWAMP, OverworldBiomes.MangroveSwamp(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.FOREST, OverworldBiomes.Forest(placed, carvers, false, false, false));
        context.Register(NacreCraft.Registry.Biomes.FLOWER_FOREST, OverworldBiomes.Forest(placed, carvers, false, false, true));
        context.Register(NacreCraft.Registry.Biomes.BIRCH_FOREST, OverworldBiomes.Forest(placed, carvers, true, false, false));
        context.Register(NacreCraft.Registry.Biomes.DARK_FOREST, OverworldBiomes.DarkForest(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.PALE_GARDEN, OverworldBiomes.DarkForest(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.OLD_GROWTH_BIRCH_FOREST, OverworldBiomes.Forest(placed, carvers, true, true, false));
        context.Register(NacreCraft.Registry.Biomes.OLD_GROWTH_PINE_TAIGA, OverworldBiomes.OldGrowthTaiga(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.OLD_GROWTH_SPRUCE_TAIGA, OverworldBiomes.OldGrowthTaiga(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.TAIGA, OverworldBiomes.Taiga(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.SNOWY_TAIGA, OverworldBiomes.Taiga(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.SAVANNA, OverworldBiomes.Savanna(placed, carvers, false, false));
        context.Register(NacreCraft.Registry.Biomes.SAVANNA_PLATEAU, OverworldBiomes.Savanna(placed, carvers, false, true));
        context.Register(NacreCraft.Registry.Biomes.WINDSWEPT_HILLS, OverworldBiomes.WindsweptHills(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.WINDSWEPT_GRAVELLY_HILLS, OverworldBiomes.WindsweptHills(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.WINDSWEPT_FOREST, OverworldBiomes.WindsweptHills(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.WINDSWEPT_SAVANNA, OverworldBiomes.Savanna(placed, carvers, true, false));
        context.Register(NacreCraft.Registry.Biomes.JUNGLE, OverworldBiomes.Jungle(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.SPARSE_JUNGLE, OverworldBiomes.SparseJungle(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.BAMBOO_JUNGLE, OverworldBiomes.BambooJungle(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.BADLANDS, OverworldBiomes.Badlands(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.ERODED_BADLANDS, OverworldBiomes.Badlands(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.WOODED_BADLANDS, OverworldBiomes.Badlands(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.MEADOW, OverworldBiomes.MeadowOrCherryGrove(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.CHERRY_GROVE, OverworldBiomes.MeadowOrCherryGrove(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.GROVE, OverworldBiomes.Grove(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.SNOWY_SLOPES, OverworldBiomes.SnowySlopes(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.FROZEN_PEAKS, OverworldBiomes.FrozenPeaks(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.JAGGED_PEAKS, OverworldBiomes.JaggedPeaks(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.STONY_PEAKS, OverworldBiomes.StonyPeaks(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.RIVER, OverworldBiomes.River(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.FROZEN_RIVER, OverworldBiomes.River(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.BEACH, OverworldBiomes.Beach(placed, carvers, false, false));
        context.Register(NacreCraft.Registry.Biomes.SNOWY_BEACH, OverworldBiomes.Beach(placed, carvers, true, false));
        context.Register(NacreCraft.Registry.Biomes.STONY_SHORE, OverworldBiomes.Beach(placed, carvers, false, true));
        context.Register(NacreCraft.Registry.Biomes.WARM_OCEAN, OverworldBiomes.WarmOcean(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.LUKEWARM_OCEAN, OverworldBiomes.LukeWarmOcean(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.DEEP_LUKEWARM_OCEAN, OverworldBiomes.LukeWarmOcean(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.OCEAN, OverworldBiomes.Ocean(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.DEEP_OCEAN, OverworldBiomes.Ocean(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.COLD_OCEAN, OverworldBiomes.ColdOcean(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.DEEP_COLD_OCEAN, OverworldBiomes.ColdOcean(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.FROZEN_OCEAN, OverworldBiomes.FrozenOcean(placed, carvers, false));
        context.Register(NacreCraft.Registry.Biomes.DEEP_FROZEN_OCEAN, OverworldBiomes.FrozenOcean(placed, carvers, true));
        context.Register(NacreCraft.Registry.Biomes.MUSHROOM_FIELDS, OverworldBiomes.MushroomFields(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.DRIPSTONE_CAVES, OverworldBiomes.DripstoneCaves(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.LUSH_CAVES, OverworldBiomes.LushCaves(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.DEEP_DARK, OverworldBiomes.DeepDark(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.SULFUR_CAVES, OverworldBiomes.SulfurCaves(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.NETHER_WASTES, NetherBiomes.NetherWastes(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.WARPED_FOREST, NetherBiomes.WarpedForest(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.CRIMSON_FOREST, NetherBiomes.CrimsonForest(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.SOUL_SAND_VALLEY, NetherBiomes.SoulSandValley(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.BASALT_DELTAS, NetherBiomes.BasaltDeltas(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.THE_END, EndBiomes.TheEnd(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.END_HIGHLANDS, EndBiomes.EndHighlands(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.END_MIDLANDS, EndBiomes.EndMidlands(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.SMALL_END_ISLANDS, EndBiomes.SmallEndIslands(placed, carvers));
        context.Register(NacreCraft.Registry.Biomes.END_BARRENS, EndBiomes.EndBarrens(placed, carvers));
    }
}
