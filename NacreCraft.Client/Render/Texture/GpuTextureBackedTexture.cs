using NacreCraft.Client.Blaze3d.Textures;

namespace NacreCraft.Client.Render.Texture;

//GpuTextureBackedTexture exposes an existing GPU texture through TextureManager without owning it
//Used to register the world block atlas under RenderTypes' synthetic atlas identifier
public sealed class GpuTextureBackedTexture : AbstractTexture
{
    public GpuTextureBackedTexture(GpuTexture texture, GpuTextureView view, GpuSampler sampler)
    {
        _texture = texture;
        _textureView = view;
        _sampler = sampler;
    }

    //Dispose intentionally leaves the wrapped resources alone; the atlas owner disposes them
    public override void Dispose()
    {
    }
}
