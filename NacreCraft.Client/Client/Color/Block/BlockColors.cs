using NacreCraft.Game.World.Level.Block;
using NacreCraft.Registry.State;
using RegistryBlock = NacreCraft.Registry.Block;

namespace NacreCraft.Client.Color.Block;

//BlockColors the registry of block tint layers, maps to vanilla net.minecraft.client.color.block.BlockColors
//The vanilla defaults also cover blocks NC's block table does not have yet (large fern, tall grass, leaves, stems, ...);
//those registrations are left out until the blocks exist, the available ones are registered below
public sealed class BlockColors
{
    public const int LilyPadInWorld = -14647248;
    public const int LilyPadDefault = -9321636;

    private static readonly BlockTintSource BlankLayer = BlockTintSources.Constant(-1);

    private readonly Dictionary<RegistryBlock, IReadOnlyList<BlockTintSource>> _sources =
        new(ReferenceEqualityComparer.Instance);

    public static BlockColors CreateDefault()
    {
        var colors = new BlockColors();
        colors.Register(new[] { BlockTintSources.Grass() }, Blocks.FERN, Blocks.SHORT_GRASS, Blocks.BUSH);
        colors.Register(new[] { BlockTintSources.GrassBlock() }, Blocks.GRASS_BLOCK);
        colors.Register(new[] { BlockTintSources.WaterParticles() }, Blocks.WATER);
        colors.Register(new[] { BlockTintSources.Redstone() }, Blocks.REDSTONE_WIRE);
        colors.Register(new[] { BlockTintSources.SugarCane() }, Blocks.SUGAR_CANE);
        colors.Register(new[] { BlockTintSources.Constant(LilyPadDefault, LilyPadInWorld) }, Blocks.LILY_PAD);
        return colors;
    }

    public IReadOnlyList<BlockTintSource> GetTintSources(BlockState state)
        => _sources.TryGetValue(state.Owner, out var layers) ? layers : Array.Empty<BlockTintSource>();

    public BlockTintSource? GetTintSource(BlockState state, int layer)
    {
        var layers = GetTintSources(state);
        return layer >= layers.Count ? null : layers[layer];
    }

    public void Register(IReadOnlyList<BlockTintSource> layers, params RegistryBlock[] blocks)
    {
        foreach (var block in blocks) _sources[block] = layers;
    }

    public IReadOnlySet<PropertyBase> GetColoringProperties(RegistryBlock block)
    {
        if (!_sources.TryGetValue(block, out var sources) || sources.Count == 0)
            return new HashSet<PropertyBase>();

        var properties = new HashSet<PropertyBase>();
        foreach (var source in sources) properties.UnionWith(source.RelevantProperties());
        return properties;
    }
}
