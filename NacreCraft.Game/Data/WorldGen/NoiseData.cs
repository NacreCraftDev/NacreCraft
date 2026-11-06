using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Synth;
using NacreCraft.Registry;

namespace NacreCraft.Game.Data.WorldGen;

//NoiseData built-in noise parameter values, maps to vanilla net.minecraft.data.worldgen.NoiseData
//One entry per data/minecraft/worldgen/noise/*.json; the octave layout drives the temperature/vegetation/continentalness/erosion bands
public static class NoiseData
{
    //DefaultShift the vanilla default shift noise, maps to vanilla DEFAULT_SHIFT
    public static readonly NoiseParameters DefaultShift = new(-3, 1.0, 1.0, 1.0, 0.0);

    public static void Bootstrap(BootstrapContext<NoiseParameters> context)
    {
        RegisterBiomeNoises(context, 0, Noises.Temperature, Noises.Vegetation, Noises.Continentalness, Noises.Erosion);
        RegisterBiomeNoises(context, -2, Noises.TemperatureLarge, Noises.VegetationLarge,
            Noises.ContinentalnessLarge, Noises.ErosionLarge);
        Register(context, Noises.TemperatureNether, -7, 1.0, 1.0);
        Register(context, Noises.VegetationNether, -7, 1.0, 1.0);
        Register(context, Noises.Ridge, -7, 1.0, 2.0, 1.0, 0.0, 0.0, 0.0);
        context.Register(Noises.Shift, DefaultShift);
        Register(context, Noises.AquiferBarrier, -3, 1.0);
        Register(context, Noises.AquiferFluidLevelFloodedness, -7, 1.0);
        Register(context, Noises.AquiferLava, -1, 1.0);
        Register(context, Noises.AquiferFluidLevelSpread, -5, 1.0);
        Register(context, Noises.Pillar, -7, 1.0, 1.0);
        Register(context, Noises.PillarRareness, -8, 1.0);
        Register(context, Noises.PillarThickness, -8, 1.0);
        Register(context, Noises.Spaghetti2D, -7, 1.0);
        Register(context, Noises.Spaghetti2DElevation, -8, 1.0);
        Register(context, Noises.Spaghetti2DModulator, -11, 1.0);
        Register(context, Noises.Spaghetti2DThickness, -11, 1.0);
        Register(context, Noises.Spaghetti3D1, -7, 1.0);
        Register(context, Noises.Spaghetti3D2, -7, 1.0);
        Register(context, Noises.Spaghetti3DRarity, -11, 1.0);
        Register(context, Noises.Spaghetti3DThickness, -8, 1.0);
        Register(context, Noises.SpaghettiRoughness, -5, 1.0);
        Register(context, Noises.SpaghettiRoughnessModulator, -8, 1.0);
        Register(context, Noises.CaveEntrance, -7, 0.4, 0.5, 1.0);
        Register(context, Noises.CaveLayer, -8, 1.0);
        Register(context, Noises.CaveCheese, -8, 0.5, 1.0, 2.0, 1.0, 2.0, 1.0, 0.0, 2.0, 0.0);
        Register(context, Noises.OreVeininess, -8, 1.0);
        Register(context, Noises.OreVeinA, -7, 1.0);
        Register(context, Noises.OreVeinB, -7, 1.0);
        Register(context, Noises.OreGap, -5, 1.0);
        Register(context, Noises.Noodle, -8, 1.0);
        Register(context, Noises.NoodleThickness, -8, 1.0);
        Register(context, Noises.NoodleRidgeA, -7, 1.0);
        Register(context, Noises.NoodleRidgeB, -7, 1.0);
        Register(context, Noises.Jagged, -16, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
            1.0);
        Register(context, Noises.Surface, -6, 1.0, 1.0, 1.0);
        Register(context, Noises.SurfaceSecondary, -6, 1.0, 1.0, 0.0, 1.0);
        Register(context, Noises.ClayBandsOffset, -8, 1.0);
        Register(context, Noises.BadlandsPillar, -2, 1.0, 1.0, 1.0, 1.0);
        Register(context, Noises.BadlandsPillarRoof, -8, 1.0);
        Register(context, Noises.BadlandsSurface, -6, 1.0, 1.0, 1.0);
        Register(context, Noises.IcebergPillar, -6, 1.0, 1.0, 1.0, 1.0);
        Register(context, Noises.IcebergPillarRoof, -3, 1.0);
        Register(context, Noises.IcebergSurface, -6, 1.0, 1.0, 1.0);
        Register(context, Noises.SulfurCaveGradient, -5, 1.0, 0.0, 1.0);
        Register(context, Noises.Swamp, -2, 1.0);
        Register(context, Noises.Calcite, -9, 1.0, 1.0, 1.0, 1.0);
        Register(context, Noises.Gravel, -8, 1.0, 1.0, 1.0, 1.0);
        Register(context, Noises.PowderSnow, -6, 1.0, 1.0, 1.0, 1.0);
        Register(context, Noises.PackedIce, -7, 1.0, 1.0, 1.0, 1.0);
        Register(context, Noises.Ice, -4, 1.0, 1.0, 1.0, 1.0);
        Register(context, Noises.SoulSandLayer, -8, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.013333333333333334);
        Register(context, Noises.GravelLayer, -8, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.013333333333333334);
        Register(context, Noises.Patch, -5, 1.0, 0.0, 0.0, 0.0, 0.0, 0.013333333333333334);
        Register(context, Noises.Netherrack, -3, 1.0, 0.0, 0.0, 0.35);
        Register(context, Noises.NetherWart, -3, 1.0, 0.0, 0.0, 0.9);
        Register(context, Noises.NetherStateSelector, -4, 1.0);
    }

    //RegisterBiomeNoises the four climate noises of one band, maps to vanilla registerBiomeNoises
    private static void RegisterBiomeNoises(BootstrapContext<NoiseParameters> context, int octaveOffset,
        ResourceKey<NoiseParameters> temperature, ResourceKey<NoiseParameters> vegetation,
        ResourceKey<NoiseParameters> continentalness, ResourceKey<NoiseParameters> erosion)
    {
        Register(context, temperature, -10 + octaveOffset, 1.5, 0.0, 1.0, 0.0, 0.0, 0.0);
        Register(context, vegetation, -8 + octaveOffset, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0);
        Register(context, continentalness, -9 + octaveOffset, 1.0, 1.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0, 1.0);
        Register(context, erosion, -9 + octaveOffset, 1.0, 1.0, 0.0, 1.0, 1.0);
    }

    //Register builds the parameters and stores them under the key, maps to vanilla register
    private static void Register(BootstrapContext<NoiseParameters> context, ResourceKey<NoiseParameters> key,
        int firstOctave, params double[] amplitudes)
        => context.Register(key, new NoiseParameters(firstOctave, amplitudes));
}
