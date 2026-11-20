using NacreCraft.Client.Blaze3d.Platform;

namespace NacreCraft.Client.Render.World;

//ChunkSectionLayer the three terrain draw layers, maps to vanilla net.minecraft.client.renderer.chunk.ChunkSectionLayer
public enum ChunkSectionLayer
{
    Solid,
    Cutout,
    Translucent
}

public static class ChunkSectionLayerExtensions
{
    public static bool IsTranslucent(this ChunkSectionLayer layer) => layer == ChunkSectionLayer.Translucent;

    //ByTransparency picks the layer an image's transparency needs
    public static ChunkSectionLayer ByTransparency(Transparency transparency)
    {
        if (transparency.HasTranslucent) return ChunkSectionLayer.Translucent;
        if (transparency.HasTransparent) return ChunkSectionLayer.Cutout;
        return ChunkSectionLayer.Solid;
    }
}
