using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;

namespace NacreCraft.Client.Render.Texture;

//AbstractTexture base class for every named texture, maps to vanilla net.minecraft.client.renderer.texture.AbstractTexture
//The subclass owns creation and upload; this base exposes the GPU view and sampler render passes bind
public abstract class AbstractTexture : IDisposable
{
    protected GpuTexture? _texture;
    protected GpuTextureView? _textureView;
    protected GpuSampler? _sampler;

    public virtual void Dispose()
    {
        ReleaseTextures();
    }

    protected void ReleaseTextures()
    {
        _textureView?.Dispose();
        _textureView = null;
        _texture?.Dispose();
        _texture = null;
    }

    public GpuTexture GetTexture()
        => _texture ?? throw new InvalidOperationException("Texture does not exist, can't get it before something initializes it");

    public GpuTextureView GetTextureView()
        => _textureView ?? throw new InvalidOperationException("Texture view does not exist, can't get it before something initializes it");

    public GpuSampler GetSampler()
        => _sampler ??= RenderSystem.GetSamplerCache().GetSampler(AddressMode.Repeat, AddressMode.Repeat, FilterMode.Nearest, FilterMode.Linear, false);
}
