namespace NacreCraft.Game.World.Level.LevelGen;

//Density density constants, maps to vanilla net.minecraft.world.level.levelgen.Density
//Shared thresholds of the density field; surface is the boundary between solid and air
public static class Density
{
    //Surface surface density, maps to vanilla SURFACE
    public const double Surface = 0.0d;

    //UnrecoverablyDense density above which terrain is treated as solid even after blending, maps to vanilla UNRECOVERABLY_DENSE
    public const double UnrecoverablyDense = 64.0d;

    //UnrecoverablyThin density below which terrain is treated as air, maps to vanilla UNRECOVERABLY_THIN
    public const double UnrecoverablyThin = -64.0d;
}
