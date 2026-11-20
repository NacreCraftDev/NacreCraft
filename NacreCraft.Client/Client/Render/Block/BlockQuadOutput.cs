using System.Numerics;
using NacreCraft.Client.Render.Model;

namespace NacreCraft.Client.Render.Block;

//BlockQuadOutput receives a model quad placed at a block-local position, maps to vanilla net.minecraft.client.renderer.block.BlockQuadOutput
public interface BlockQuadOutput
{
    void Put(float x, float y, float z, BakedQuad quad, QuadInstance instance);
}
