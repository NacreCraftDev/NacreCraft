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

    //Create builds the key from the vanilla block tag path
    private static TagKey<NacreCraft.Registry.Block> Create(string path)
        => TagKey<NacreCraft.Registry.Block>.Create(Registries.BLOCK,
            Identifier.WithDefaultNamespace(path));
}
