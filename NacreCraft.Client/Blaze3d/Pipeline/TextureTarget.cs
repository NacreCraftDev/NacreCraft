using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Systems;

namespace NacreCraft.Client.Blaze3d.Pipeline;

//TextureTarget render target backed by plain textures, maps to vanilla com.mojang.blaze3d.pipeline.TextureTarget
//All allocation happens in the base class; this type only fixes the attachment storage to textures
public class TextureTarget : RenderTarget
{
    public TextureTarget(GpuDevice device, string? label, int width, int height, bool useDepth, GpuFormat format)
        : base(device, label, useDepth, format)
        => Resize(width, height);
}
