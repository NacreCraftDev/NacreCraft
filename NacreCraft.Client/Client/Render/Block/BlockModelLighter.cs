using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Render.Model;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;

namespace NacreCraft.Client.Render.Block;

//BlockModelLighter resolves per-quad light and shade, maps to vanilla net.minecraft.client.renderer.block.BlockModelLighter
//NC uses the flat-light path only: ambient occlusion corner blending and cardinal face shading are not ported yet, so the
//whole quad receives one packed light value and a neutral color
public sealed class BlockModelLighter
{
    public const int CheckLight = -1;

    private readonly Dictionary<long, int> _lightCache = new();

    public static void EnableCaching()
    {
    }

    public static void ClearCache()
    {
    }

    //GetLightCoords returns the packed block+sky light at the position, with the state's own emission folded in
    public int GetLightCoords(BlockState state, BlockAndTintGetter level, BlockPos pos)
    {
        var key = pos.AsLong();
        if (_lightCache.TryGetValue(key, out var cached)) return cached;

        var block = level.GetBrightness(LightLayer.Block, pos);
        var sky = level.GetBrightness(LightLayer.Sky, pos);
        var emission = state.GetLightEmission();
        if (emission > block) block = emission;
        var packed = LightTexture.PackLightCoords(block, sky);
        if (_lightCache.Count > 100) _lightCache.Clear();
        _lightCache[key] = packed;
        return packed;
    }

    //PrepareQuadAmbientOcclusion falls back to the flat light path
    public void PrepareQuadAmbientOcclusion(BlockAndTintGetter level, BlockState state, BlockPos centerPosition,
        BakedQuad quad, QuadInstance outputInstance)
        => PrepareQuadFlat(level, state, centerPosition, CheckLight, quad, outputInstance);

    //PrepareQuadFlat sets the quad's light and neutral color, maps to vanilla prepareQuadFlat
    public void PrepareQuadFlat(BlockAndTintGetter level, BlockState state, BlockPos pos, int lightCoords,
        BakedQuad quad, QuadInstance outputInstance)
    {
        outputInstance.LightCoords = lightCoords == CheckLight ? GetLightCoords(state, level, pos) : lightCoords;
        outputInstance.Color = -1;
    }
}
