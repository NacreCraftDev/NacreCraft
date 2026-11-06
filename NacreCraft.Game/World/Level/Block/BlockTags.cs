using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.Block;

//BlockTags block tags wired up here, maps to vanilla net.minecraft.tags.BlockTags
//Tag values come from datapack data/<ns>/tags/block/*.json and are bound to the registry by TagsReloadListener at startup
//Only the ones in use are registered here, the several hundred vanilla ones are added as needed
//The supports_* family is the 26.2 attachment system: what vegetation and crops can be planted on is decided entirely by tags instead of being hardcoded
public static class BlockTags
{
    //SupportsVegetation support face of plain vegetation shared by grass, flowers, saplings and bushes, maps to vanilla supports_vegetation
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsVegetation = Create("supports_vegetation");

    //DampensVibrations blocks that muffle a vibration passing through, maps to vanilla DAMPENS_VIBRATIONS
    //Wool and carpets are in it; a sculk sensor ignores a vibration that came out of one
    public static readonly TagKey<NacreCraft.Registry.Block> DampensVibrations = Create("dampens_vibrations");

    //OccludesVibrationSignals blocks that cut a vibration signal off entirely, maps to vanilla OCCLUDES_VIBRATION_SIGNALS
    //A sensor behind one of these never hears the vibration at all
    public static readonly TagKey<NacreCraft.Registry.Block> OccludesVibrationSignals = Create("occludes_vibration_signals");

    //VibrationResonators blocks that answer a vibration with a resonance, maps to vanilla VIBRATION_RESONATORS
    public static readonly TagKey<NacreCraft.Registry.Block> VibrationResonators = Create("vibration_resonators");

    //SculkReplaceable blocks a sculk charge may overwrite, maps to vanilla SCULK_REPLACEABLE
    public static readonly TagKey<NacreCraft.Registry.Block> SculkReplaceable = Create("sculk_replaceable");

    //SculkReplaceableWorldGen the narrower set world generation may overwrite, maps to vanilla SCULK_REPLACEABLE_WORLD_GEN
    public static readonly TagKey<NacreCraft.Registry.Block> SculkReplaceableWorldGen = Create("sculk_replaceable_world_gen");

    //SupportsDryVegetation support face of dry grass and dead bushes, maps to vanilla supports_dry_vegetation
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsDryVegetation = Create("supports_dry_vegetation");

    //SupportsCrops support face of crops, effectively farmland, maps to vanilla supports_crops
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsCrops = Create("supports_crops");

    //SupportsStemCrops support face of stem crops, maps to vanilla supports_stem_crops
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsStemCrops = Create("supports_stem_crops");

    //SupportsStemFruit the face a stem's fruit cell must attach to, maps to vanilla supports_stem_fruit
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsStemFruit = Create("supports_stem_fruit");

    //SupportsSugarCane support face of sugar cane, maps to vanilla supports_sugar_cane
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsSugarCane = Create("supports_sugar_cane");

    //SupportsSugarCaneAdjacently what sugar cane must be next to in order to grow, maps to vanilla supports_sugar_cane_adjacently
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsSugarCaneAdjacently = Create("supports_sugar_cane_adjacently");

    //SupportsBamboo support face of bamboo, maps to vanilla supports_bamboo
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsBamboo = Create("supports_bamboo");

    //SupportsSmallDripleaf support face of small dripleaf, maps to vanilla supports_small_dripleaf
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsSmallDripleaf = Create("supports_small_dripleaf");

    //SupportsBigDripleaf support face of big dripleaf, maps to vanilla supports_big_dripleaf
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsBigDripleaf = Create("supports_big_dripleaf");

    //SupportsCactus support face of cactus, maps to vanilla supports_cactus
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsCactus = Create("supports_cactus");

    //SupportsChorusPlant support face of chorus plant, maps to vanilla supports_chorus_plant
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsChorusPlant = Create("supports_chorus_plant");

    //SupportsChorusFlower support face of chorus flower, maps to vanilla supports_chorus_flower
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsChorusFlower = Create("supports_chorus_flower");

    //SupportsNetherSprouts support face of nether sprouts, maps to vanilla supports_nether_sprouts
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsNetherSprouts = Create("supports_nether_sprouts");

    //SupportsAzalea support face of azalea, maps to vanilla supports_azalea
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsAzalea = Create("supports_azalea");

    //SupportsWarpedFungus support face of warped fungus, maps to vanilla supports_warped_fungus
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsWarpedFungus = Create("supports_warped_fungus");

    //SupportsCrimsonFungus support face of crimson fungus, maps to vanilla supports_crimson_fungus
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsCrimsonFungus = Create("supports_crimson_fungus");

    //SupportsMangrovePropagule support face of mangrove propagule, maps to vanilla supports_mangrove_propagule
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsMangrovePropagule = Create("supports_mangrove_propagule");

    //SupportsNetherWart support face of nether wart, maps to vanilla supports_nether_wart
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsNetherWart = Create("supports_nether_wart");

    //SupportsCrimsonRoots support face of crimson roots, maps to vanilla supports_crimson_roots
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsCrimsonRoots = Create("supports_crimson_roots");

    //SupportsWarpedRoots support face of warped roots, maps to vanilla supports_warped_roots
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsWarpedRoots = Create("supports_warped_roots");

    //SupportsWitherRose support face of wither rose, maps to vanilla supports_wither_rose
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsWitherRose = Create("supports_wither_rose");

    //SupportsCocoa the face cocoa attaches to, maps to vanilla supports_cocoa
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsCocoa = Create("supports_cocoa");

    //SupportsLilyPad what lily pads float on, maps to vanilla supports_lily_pad
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsLilyPad = Create("supports_lily_pad");

    //SupportsFrogspawn support face of frogspawn, maps to vanilla supports_frogspawn
    public static readonly TagKey<NacreCraft.Registry.Block> SupportsFrogspawn = Create("supports_frogspawn");

    //CannotSupportSeagrass blocks that cannot support seagrass, effectively only magma, maps to vanilla cannot_support_seagrass
    public static readonly TagKey<NacreCraft.Registry.Block> CannotSupportSeagrass = Create("cannot_support_seagrass");

    //CannotSupportKelp blocks that cannot support kelp, maps to vanilla cannot_support_kelp
    public static readonly TagKey<NacreCraft.Registry.Block> CannotSupportKelp = Create("cannot_support_kelp");

    //SupportOverrideCactusFlower cactus flowers accept it directly instead of checking face sturdiness, maps to vanilla support_override_cactus_flower
    public static readonly TagKey<NacreCraft.Registry.Block> SupportOverrideCactusFlower = Create("support_override_cactus_flower");

    //Walls all wall types; fence gates use it to decide whether to place in the walled state, maps to vanilla walls
    public static readonly TagKey<NacreCraft.Registry.Block> Walls = Create("walls");

    //Trapdoors all trapdoor types, pathfinding treats them as TRAPDOOR cells, maps to vanilla trapdoors
    public static readonly TagKey<NacreCraft.Registry.Block> Trapdoors = Create("trapdoors");

    //Fences all fence types, pathfinding treats them as FENCE cells, maps to vanilla fences
    public static readonly TagKey<NacreCraft.Registry.Block> Fences = Create("fences");

    //Speleothems pointed dripstone and similar, pathfinding marks them DAMAGE_CAUTIOUS, maps to vanilla speleothems
    public static readonly TagKey<NacreCraft.Registry.Block> Speleothems = Create("speleothems");

    //Fire fire blocks, pathfinding marks them FIRE and checks neighbors for it, maps to vanilla fire
    public static readonly TagKey<NacreCraft.Registry.Block> Fire = Create("fire");

    //Rails all rail types, dispensers place minecarts only onto a rail, maps to vanilla rails
    public static readonly TagKey<NacreCraft.Registry.Block> Rails = Create("rails");

    //OverworldCarverReplaceables blocks an overworld cave or canyon may carve through, maps to vanilla overworld_carver_replaceables
    public static readonly TagKey<NacreCraft.Registry.Block> OverworldCarverReplaceables = Create("overworld_carver_replaceables");

    //NetherCarverReplaceables blocks a nether cave may carve through, maps to vanilla nether_carver_replaceables
    public static readonly TagKey<NacreCraft.Registry.Block> NetherCarverReplaceables = Create("nether_carver_replaceables");

    //BaseStoneOverworld the overworld base stone set, ore features replace it, maps to vanilla base_stone_overworld
    public static readonly TagKey<NacreCraft.Registry.Block> BaseStoneOverworld = Create("base_stone_overworld");

    //BaseStoneNether the nether base stone set, maps to vanilla base_stone_nether
    public static readonly TagKey<NacreCraft.Registry.Block> BaseStoneNether = Create("base_stone_nether");

    //StoneOreReplaceables stone variants an overworld ore may replace, maps to vanilla stone_ore_replaceables
    public static readonly TagKey<NacreCraft.Registry.Block> StoneOreReplaceables = Create("stone_ore_replaceables");

    //DeepslateOreReplaceables deepslate variants an overworld ore may replace, maps to vanilla deepslate_ore_replaceables
    public static readonly TagKey<NacreCraft.Registry.Block> DeepslateOreReplaceables = Create("deepslate_ore_replaceables");

    //IceSpikeReplaceable blocks an ice spike may replace, maps to vanilla ice_spike_replaceable
    public static readonly TagKey<NacreCraft.Registry.Block> IceSpikeReplaceable = Create("ice_spike_replaceable");

    //ForestRockCanPlaceOn blocks a forest rock may sit on, maps to vanilla forest_rock_can_place_on
    public static readonly TagKey<NacreCraft.Registry.Block> ForestRockCanPlaceOn = Create("forest_rock_can_place_on");

    //FeaturesCannotReplace blocks features must not overwrite, maps to vanilla features_cannot_replace
    public static readonly TagKey<NacreCraft.Registry.Block> FeaturesCannotReplace = Create("features_cannot_replace");

    //LavaPoolStoneCannotReplace blocks a lava lake's stone shell must not overwrite, maps to vanilla lava_pool_stone_cannot_replace
    public static readonly TagKey<NacreCraft.Registry.Block> LavaPoolStoneCannotReplace = Create("lava_pool_stone_cannot_replace");

    //CannotReplaceBelowTreeTrunk blocks a tree must not turn into dirt below its trunk, maps to vanilla cannot_replace_below_tree_trunk
    public static readonly TagKey<NacreCraft.Registry.Block> CannotReplaceBelowTreeTrunk = Create("cannot_replace_below_tree_trunk");

    //BeneathTreePodzolReplaceable blocks a mega spruce turns into podzol, maps to vanilla beneath_tree_podzol_replaceable
    public static readonly TagKey<NacreCraft.Registry.Block> BeneathTreePodzolReplaceable = Create("beneath_tree_podzol_replaceable");

    //HugeBrownMushroomCanPlaceOn blocks a huge brown mushroom may grow on, maps to vanilla huge_brown_mushroom_can_place_on
    public static readonly TagKey<NacreCraft.Registry.Block> HugeBrownMushroomCanPlaceOn = Create("huge_brown_mushroom_can_place_on");

    //HugeRedMushroomCanPlaceOn blocks a huge red mushroom may grow on, maps to vanilla huge_red_mushroom_can_place_on
    public static readonly TagKey<NacreCraft.Registry.Block> HugeRedMushroomCanPlaceOn = Create("huge_red_mushroom_can_place_on");

    //MangroveLogsCanGrowThrough blocks a mangrove trunk may pass through, maps to vanilla mangrove_logs_can_grow_through
    public static readonly TagKey<NacreCraft.Registry.Block> MangroveLogsCanGrowThrough = Create("mangrove_logs_can_grow_through");

    //MangroveRootsCanGrowThrough blocks mangrove roots may pass through, maps to vanilla mangrove_roots_can_grow_through
    public static readonly TagKey<NacreCraft.Registry.Block> MangroveRootsCanGrowThrough = Create("mangrove_roots_can_grow_through");

    //MossReplaceable blocks a moss patch may replace, maps to vanilla moss_replaceable
    public static readonly TagKey<NacreCraft.Registry.Block> MossReplaceable = Create("moss_replaceable");

    //Doors all door types, village zombie degradation strips them, maps to vanilla doors
    public static readonly TagKey<NacreCraft.Registry.Block> Doors = Create("doors");

    //AncientCityReplaceable blocks ancient city degradation may rot, maps to vanilla ancient_city_replaceable
    public static readonly TagKey<NacreCraft.Registry.Block> AncientCityReplaceable = Create("ancient_city_replaceable");

    //TrailRuinsReplaceable blocks trail ruin archaeology turns into suspicious gravel, maps to vanilla trail_ruins_replaceable
    public static readonly TagKey<NacreCraft.Registry.Block> TrailRuinsReplaceable = Create("trail_ruins_replaceable");

    //DripstoneReplaceable blocks a dripstone cluster or large dripstone may replace, maps to vanilla dripstone_replaceable_blocks
    public static readonly TagKey<NacreCraft.Registry.Block> DripstoneReplaceable = Create("dripstone_replaceable_blocks");

    //SulfurSpikeReplaceable blocks a sulfur spike cluster may replace, maps to vanilla sulfur_spike_replaceable
    public static readonly TagKey<NacreCraft.Registry.Block> SulfurSpikeReplaceable = Create("sulfur_spike_replaceable");

    //AzaleaRootReplaceable blocks the azalea root system may replace, maps to vanilla azalea_root_replaceable
    public static readonly TagKey<NacreCraft.Registry.Block> AzaleaRootReplaceable = Create("azalea_root_replaceable");

    //AzaleaGrowsOn blocks the azalea root system may start on, maps to vanilla azalea_grows_on
    public static readonly TagKey<NacreCraft.Registry.Block> AzaleaGrowsOn = Create("azalea_grows_on");

    //ReplaceableByTrees blocks a tree may overwrite, maps to vanilla replaceable_by_trees
    public static readonly TagKey<NacreCraft.Registry.Block> ReplaceableByTrees = Create("replaceable_by_trees");

    //LushGroundReplaceable blocks a lush caves clay patch may replace, maps to vanilla lush_ground_replaceable
    public static readonly TagKey<NacreCraft.Registry.Block> LushGroundReplaceable = Create("lush_ground_replaceable");

    //GeodeInvalidBlocks blocks a geode must not carve into, maps to vanilla geode_invalid_blocks
    public static readonly TagKey<NacreCraft.Registry.Block> GeodeInvalidBlocks = Create("geode_invalid_blocks");

    //Create builds the key from the vanilla block tag path
    private static TagKey<NacreCraft.Registry.Block> Create(string path)
        => TagKey<NacreCraft.Registry.Block>.Create(Registries.BLOCK,
            Identifier.WithDefaultNamespace(path));
}

