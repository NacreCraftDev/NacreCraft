using NacreCraft.Client.Render.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;

namespace NacreCraft.Client.Color.Block;

//BlockTintSource supplies the tint layers of one block, maps to vanilla net.minecraft.client.color.block.BlockTintSource
public interface BlockTintSource
{
    int Color(BlockState state);

    int ColorInWorld(BlockState state, BlockAndTintGetter level, BlockPos pos) => Color(state);

    int ColorAsTerrainParticle(BlockState state, BlockAndTintGetter level, BlockPos pos)
        => ColorInWorld(state, level, pos);

    IReadOnlySet<PropertyBase> RelevantProperties() => EmptyProperties;

    private static readonly IReadOnlySet<PropertyBase> EmptyProperties = new HashSet<PropertyBase>();
}
