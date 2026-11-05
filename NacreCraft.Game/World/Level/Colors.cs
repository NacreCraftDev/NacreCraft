using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level;

//ColorMapColorUtil colormap pixel lookup shared by the grass and foliage colors, maps to vanilla net.minecraft.world.level.ColorMapColorUtil
//The vanilla 256 x 256 colormap is indexed by temperature and rainfall
public interface ColorMapColorUtil
{
    const int PixelCount = 256 * 256;

    //Get the colormap pixel for the temperature and rainfall, maps to vanilla get
    static int Get(double temp, double rain, int[] pixels, int defaultMapColor)
    {
        var x = (int)((1.0 - temp) * 255.0);
        var y = (int)((1.0 - (rain * temp)) * 255.0);
        var index = (y << 8) | x;
        if (index < 0 || index >= pixels.Length) return defaultMapColor;
        return pixels[index];
    }
}

//ColorResolver color of a biome at a world column, maps to vanilla net.minecraft.world.level.ColorResolver
public interface ColorResolver
{
    int GetColor(Biome biome, double x, double z);
}

//DryFoliageColor dry foliage colormap, maps to vanilla net.minecraft.world.level.DryFoliageColor
public sealed class DryFoliageColor
{
    public const int FoliageDryDefault = -10732494;

    private static int[] _pixels = new int[ColorMapColorUtil.PixelCount];

    public static void Init(int[] pixels) => _pixels = pixels;

    public static int Get(double temp, double rain) => ColorMapColorUtil.Get(temp, rain, _pixels, FoliageDryDefault);
}

//FoliageColor foliage colormap and the fixed biome colors, maps to vanilla net.minecraft.world.level.FoliageColor
public sealed class FoliageColor
{
    public const int FoliageEvergreen = -10380959;
    public const int FoliageBirch = -8345771;
    public const int FoliageDefault = -12012264;
    public const int FoliageMangrove = -7158200;

    private static int[] _pixels = new int[ColorMapColorUtil.PixelCount];

    public static void Init(int[] pixels) => _pixels = pixels;

    public static int Get(double temp, double rain) => ColorMapColorUtil.Get(temp, rain, _pixels, FoliageDefault);
}

//GrassColor grass colormap, maps to vanilla net.minecraft.world.level.GrassColor
public sealed class GrassColor
{
    private const int GrassDefault = -65281;

    private static int[] _pixels = new int[ColorMapColorUtil.PixelCount];

    public static void Init(int[] pixels) => _pixels = pixels;

    public static int Get(double temp, double rain) => ColorMapColorUtil.Get(temp, rain, _pixels, GrassDefault);

    public static int GetDefaultColor() => Get(0.5, 1.0);
}
