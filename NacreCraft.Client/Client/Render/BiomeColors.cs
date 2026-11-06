using NacreCraft.Client.Render.Block;
using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render;

//BiomeColors biome tint entry points, maps to vanilla net.minecraft.client.renderer.BiomeColors
//Biome-specific color methods are not ported yet, so the resolvers fall back to the vanilla default colors
public static class BiomeColors
{
    public static readonly ColorResolver GRASS_COLOR_RESOLVER = new FixedColorResolver(GrassColor.GetDefaultColor());

    public static readonly ColorResolver FOLIAGE_COLOR_RESOLVER = new FixedColorResolver(FoliageColor.FoliageDefault);

    public static readonly ColorResolver DRY_FOLIAGE_COLOR_RESOLVER = new FixedColorResolver(DryFoliageColor.FoliageDryDefault);

    public static readonly ColorResolver WATER_COLOR_RESOLVER = new FixedColorResolver(4159204);

    public static int GetAverageGrassColor(BlockAndTintGetter level, BlockPos pos)
        => level.GetBlockTint(pos, GRASS_COLOR_RESOLVER);

    public static int GetAverageFoliageColor(BlockAndTintGetter level, BlockPos pos)
        => level.GetBlockTint(pos, FOLIAGE_COLOR_RESOLVER);

    public static int GetAverageDryFoliageColor(BlockAndTintGetter level, BlockPos pos)
        => level.GetBlockTint(pos, DRY_FOLIAGE_COLOR_RESOLVER);

    public static int GetAverageWaterColor(BlockAndTintGetter level, BlockPos pos)
        => level.GetBlockTint(pos, WATER_COLOR_RESOLVER);

    //FixedColorResolver the fallback while Biome has no per-biome color accessor
    private sealed class FixedColorResolver(int color) : ColorResolver
    {
        public int GetColor(Biome biome, double x, double z) => color;
    }
}
