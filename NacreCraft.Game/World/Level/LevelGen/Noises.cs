using NacreCraft.Game.World.Level.LevelGen.Synth;
using NacreCraft.Registry;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen;

//Noises noise parameter registry, maps to vanilla net.minecraft.world.level.levelgen.Noises
//Defines the ResourceKeys of 64 built-in NoiseParameters and registers the actual values in Bootstrap
//Vanilla values come from data/minecraft/worldgen/noise/*.json; here they are hard-coded to avoid data pack loading
public static class Noises
{
    public static readonly ResourceKey<NoiseParameters> Temperature = CreateKey("temperature");
    public static readonly ResourceKey<NoiseParameters> Vegetation = CreateKey("vegetation");
    public static readonly ResourceKey<NoiseParameters> Continentalness = CreateKey("continentalness");
    public static readonly ResourceKey<NoiseParameters> Erosion = CreateKey("erosion");
    public static readonly ResourceKey<NoiseParameters> TemperatureLarge = CreateKey("temperature_large");
    public static readonly ResourceKey<NoiseParameters> VegetationLarge = CreateKey("vegetation_large");
    public static readonly ResourceKey<NoiseParameters> ContinentalnessLarge = CreateKey("continentalness_large");
    public static readonly ResourceKey<NoiseParameters> ErosionLarge = CreateKey("erosion_large");
    public static readonly ResourceKey<NoiseParameters> Ridge = CreateKey("ridge");
    //SHIFT matches the json path offset; keeps the vanilla constant name SHIFT
    public static readonly ResourceKey<NoiseParameters> Shift = CreateKey("offset");
    public static readonly ResourceKey<NoiseParameters> TemperatureNether = CreateKey("nether/temperature");
    public static readonly ResourceKey<NoiseParameters> VegetationNether = CreateKey("nether/vegetation");
    public static readonly ResourceKey<NoiseParameters> AquiferBarrier = CreateKey("aquifer_barrier");
    public static readonly ResourceKey<NoiseParameters> AquiferFluidLevelFloodedness = CreateKey("aquifer_fluid_level_floodedness");
    public static readonly ResourceKey<NoiseParameters> AquiferLava = CreateKey("aquifer_lava");
    public static readonly ResourceKey<NoiseParameters> AquiferFluidLevelSpread = CreateKey("aquifer_fluid_level_spread");
    public static readonly ResourceKey<NoiseParameters> Pillar = CreateKey("pillar");
    public static readonly ResourceKey<NoiseParameters> PillarRareness = CreateKey("pillar_rareness");
    public static readonly ResourceKey<NoiseParameters> PillarThickness = CreateKey("pillar_thickness");
    public static readonly ResourceKey<NoiseParameters> Spaghetti2D = CreateKey("spaghetti_2d");
    public static readonly ResourceKey<NoiseParameters> Spaghetti2DElevation = CreateKey("spaghetti_2d_elevation");
    public static readonly ResourceKey<NoiseParameters> Spaghetti2DModulator = CreateKey("spaghetti_2d_modulator");
    public static readonly ResourceKey<NoiseParameters> Spaghetti2DThickness = CreateKey("spaghetti_2d_thickness");
    public static readonly ResourceKey<NoiseParameters> Spaghetti3D1 = CreateKey("spaghetti_3d_1");
    public static readonly ResourceKey<NoiseParameters> Spaghetti3D2 = CreateKey("spaghetti_3d_2");
    public static readonly ResourceKey<NoiseParameters> Spaghetti3DRarity = CreateKey("spaghetti_3d_rarity");
    public static readonly ResourceKey<NoiseParameters> Spaghetti3DThickness = CreateKey("spaghetti_3d_thickness");
    public static readonly ResourceKey<NoiseParameters> SpaghettiRoughness = CreateKey("spaghetti_roughness");
    public static readonly ResourceKey<NoiseParameters> SpaghettiRoughnessModulator = CreateKey("spaghetti_roughness_modulator");
    public static readonly ResourceKey<NoiseParameters> CaveEntrance = CreateKey("cave_entrance");
    public static readonly ResourceKey<NoiseParameters> CaveLayer = CreateKey("cave_layer");
    public static readonly ResourceKey<NoiseParameters> CaveCheese = CreateKey("cave_cheese");
    public static readonly ResourceKey<NoiseParameters> OreVeininess = CreateKey("ore_veininess");
    public static readonly ResourceKey<NoiseParameters> OreVeinA = CreateKey("ore_vein_a");
    public static readonly ResourceKey<NoiseParameters> OreVeinB = CreateKey("ore_vein_b");
    public static readonly ResourceKey<NoiseParameters> OreGap = CreateKey("ore_gap");
    public static readonly ResourceKey<NoiseParameters> Noodle = CreateKey("noodle");
    public static readonly ResourceKey<NoiseParameters> NoodleThickness = CreateKey("noodle_thickness");
    public static readonly ResourceKey<NoiseParameters> NoodleRidgeA = CreateKey("noodle_ridge_a");
    public static readonly ResourceKey<NoiseParameters> NoodleRidgeB = CreateKey("noodle_ridge_b");
    public static readonly ResourceKey<NoiseParameters> Jagged = CreateKey("jagged");
    public static readonly ResourceKey<NoiseParameters> Surface = CreateKey("surface");
    public static readonly ResourceKey<NoiseParameters> SurfaceSecondary = CreateKey("surface_secondary");
    public static readonly ResourceKey<NoiseParameters> ClayBandsOffset = CreateKey("clay_bands_offset");
    public static readonly ResourceKey<NoiseParameters> BadlandsPillar = CreateKey("badlands_pillar");
    public static readonly ResourceKey<NoiseParameters> BadlandsPillarRoof = CreateKey("badlands_pillar_roof");
    public static readonly ResourceKey<NoiseParameters> BadlandsSurface = CreateKey("badlands_surface");
    public static readonly ResourceKey<NoiseParameters> IcebergPillar = CreateKey("iceberg_pillar");
    public static readonly ResourceKey<NoiseParameters> IcebergPillarRoof = CreateKey("iceberg_pillar_roof");
    public static readonly ResourceKey<NoiseParameters> IcebergSurface = CreateKey("iceberg_surface");
    public static readonly ResourceKey<NoiseParameters> SulfurCaveGradient = CreateKey("sulfur_cave_gradient");
    //SWAMP matches the json path surface_swamp; keeps the vanilla constant name SWAMP
    public static readonly ResourceKey<NoiseParameters> Swamp = CreateKey("surface_swamp");
    public static readonly ResourceKey<NoiseParameters> Calcite = CreateKey("calcite");
    public static readonly ResourceKey<NoiseParameters> Gravel = CreateKey("gravel");
    public static readonly ResourceKey<NoiseParameters> PowderSnow = CreateKey("powder_snow");
    public static readonly ResourceKey<NoiseParameters> PackedIce = CreateKey("packed_ice");
    public static readonly ResourceKey<NoiseParameters> Ice = CreateKey("ice");
    public static readonly ResourceKey<NoiseParameters> SoulSandLayer = CreateKey("soul_sand_layer");
    public static readonly ResourceKey<NoiseParameters> GravelLayer = CreateKey("gravel_layer");
    public static readonly ResourceKey<NoiseParameters> Patch = CreateKey("patch");
    public static readonly ResourceKey<NoiseParameters> Netherrack = CreateKey("netherrack");
    public static readonly ResourceKey<NoiseParameters> NetherWart = CreateKey("nether_wart");
    public static readonly ResourceKey<NoiseParameters> NetherStateSelector = CreateKey("nether_state_selector");

    //CreateKey creates an element key in the worldgen/noise registry, maps to vanilla createKey
    private static ResourceKey<NoiseParameters> CreateKey(string path)
        => ResourceKey<NoiseParameters>.Create(Registries.NOISE, Identifier.WithDefaultNamespace(path));

    //Bootstrapped flag avoids double registration, matching the vanilla registry being read-only after freeze
    private static bool _bootstrapped;

    //Bootstrap registers the 64 built-in NoiseParameters into BuiltInRegistries.NOISE
    //Called by GameBootstrap to make sure every noise parameter is ready before RandomState instantiates
    //The values live in NoiseData, this only wires the built-in registry to it
    public static void Bootstrap()
    {
        if (_bootstrapped) return;
        _bootstrapped = true;
        NacreCraft.Game.Data.WorldGen.NoiseData.Bootstrap(
            new NacreCraft.Game.Data.WorldGen.BuiltInBootstrapContext<NoiseParameters>(BuiltInRegistries.NOISE));
    }

    //Instantiate looks up NoiseParameters by ResourceKey and builds a NormalNoise, maps to vanilla instantiate
    //context derives a deterministic RandomSource through FromHashOf so results reproduce across instances
    public static NormalNoise Instantiate(Registry<NoiseParameters> noises, PositionalRandomFactory context, ResourceKey<NoiseParameters> name)
    {
        var parameters = noises.GetValueOrThrow(name);
        return NormalNoise.Create(context.FromHashOf(name.Identifier.ToString()), parameters);
    }
}
