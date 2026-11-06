using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;

namespace NacreCraft.Client.Render.Block;

//BlockAndTintGetter level view that can resolve a biome tint at a position, maps to vanilla net.minecraft.client.renderer.block.BlockAndTintGetter
public interface BlockAndTintGetter : BlockAndLightGetter
{
    //GetBlockTint the tint color for one position through a color resolver, maps to vanilla getBlockTint
    int GetBlockTint(BlockPos pos, ColorResolver color);
}
