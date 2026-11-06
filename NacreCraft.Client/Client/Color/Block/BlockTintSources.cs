using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Block;
using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.Environment;
using NacreCraft.Registry.State;

namespace NacreCraft.Client.Color.Block;

//BlockTintSources factories for the built-in tint layers, maps to vanilla net.minecraft.client.color.block.BlockTintSources
public static class BlockTintSources
{
    public static BlockTintSource Constant(int color) => new DelegateTintSource(_ => color);

    public static BlockTintSource Constant(int colorInHand, int colorInWorld)
        => new DelegateTintSource(_ => colorInHand, (_, _, _) => colorInWorld);

    public static BlockTintSource DoubleTallGrass() => new DelegateTintSource(
        _ => GrassColor.GetDefaultColor(),
        (state, level, pos) => BiomeColors.GetAverageGrassColor(level,
            state.GetValue(BlockStateProperties.DoubleBlockHalfProperty) == DoubleBlockHalf.upper
                ? pos.Offset(0, -1, 0)
                : pos),
        null,
        new HashSet<PropertyBase> { BlockStateProperties.DoubleBlockHalfProperty });

    public static BlockTintSource Grass() => new DelegateTintSource(
        _ => GrassColor.GetDefaultColor(),
        (_, level, pos) => BiomeColors.GetAverageGrassColor(level, pos));

    public static BlockTintSource GrassBlock() => new DelegateTintSource(
        _ => GrassColor.GetDefaultColor(),
        (_, level, pos) => BiomeColors.GetAverageGrassColor(level, pos),
        (_, _, _) => -1);

    public static BlockTintSource SugarCane() => new DelegateTintSource(
        _ => -1,
        (_, level, pos) => BiomeColors.GetAverageGrassColor(level, pos));

    public static BlockTintSource Foliage() => new DelegateTintSource(
        _ => FoliageColor.FoliageDefault,
        (_, level, pos) => BiomeColors.GetAverageFoliageColor(level, pos));

    public static BlockTintSource DryFoliage() => new DelegateTintSource(
        _ => DryFoliageColor.FoliageDryDefault,
        (_, level, pos) => BiomeColors.GetAverageDryFoliageColor(level, pos));

    public static BlockTintSource Water() => new DelegateTintSource(
        _ => -1,
        (_, level, pos) => BiomeColors.GetAverageWaterColor(level, pos));

    public static BlockTintSource WaterParticles() => new DelegateTintSource(
        _ => -1,
        null,
        (_, level, pos) => BiomeColors.GetAverageWaterColor(level, pos));

    public static BlockTintSource Redstone() => new DelegateTintSource(
        state => RedstoneColor(state.GetValue(BlockStateProperties.Power)),
        null,
        null,
        new HashSet<PropertyBase> { BlockStateProperties.Power });

    public static BlockTintSource Stem() => new DelegateTintSource(
        state =>
        {
            var age = state.GetValue(BlockStateProperties.Age7);
            return Argb.Color(age * 32, 255 - age * 8, age * 4, 0);
        },
        null,
        null,
        new HashSet<PropertyBase> { BlockStateProperties.Age7 });

    //RedstoneColor the vanilla redstone wire power ramp
    private static int RedstoneColor(int power)
    {
        var p = power / 15f;
        var red = p * 0.6f + (p > 0f ? 0.4f : 0.3f);
        var green = Math.Clamp(p * p * 0.7f - 0.5f, 0f, 1f);
        var blue = Math.Clamp(p * p * 0.6f - 0.7f, 0f, 1f);
        return Argb.ColorFromFloat(1f, red, green, blue);
    }

    private sealed class DelegateTintSource : BlockTintSource
    {
        private readonly Func<BlockState, int> _color;
        private readonly Func<BlockState, BlockAndTintGetter, BlockPos, int>? _inWorld;
        private readonly Func<BlockState, BlockAndTintGetter, BlockPos, int>? _particle;
        private readonly IReadOnlySet<PropertyBase> _properties;

        public DelegateTintSource(Func<BlockState, int> color,
            Func<BlockState, BlockAndTintGetter, BlockPos, int>? inWorld = null,
            Func<BlockState, BlockAndTintGetter, BlockPos, int>? particle = null,
            IReadOnlySet<PropertyBase>? properties = null)
        {
            _color = color;
            _inWorld = inWorld;
            _particle = particle;
            _properties = properties ?? new HashSet<PropertyBase>();
        }

        public int Color(BlockState state) => _color(state);

        public int ColorInWorld(BlockState state, BlockAndTintGetter level, BlockPos pos)
            => _inWorld?.Invoke(state, level, pos) ?? _color(state);

        public int ColorAsTerrainParticle(BlockState state, BlockAndTintGetter level, BlockPos pos)
            => _particle?.Invoke(state, level, pos) ?? ColorInWorld(state, level, pos);

        public IReadOnlySet<PropertyBase> RelevantProperties() => _properties;
    }
}
