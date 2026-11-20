using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Resources;
using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Texture;

//ReloadableTexture a texture that can be rebuilt from resource pack contents, maps to vanilla net.minecraft.client.renderer.texture.ReloadableTexture
public abstract class ReloadableTexture : AbstractTexture
{
    protected ReloadableTexture(Identifier resourceId) => ResourceId = resourceId;

    public Identifier ResourceId { get; }

    //Apply replaces the GPU resources with the supplied decoded contents
    public void Apply(TextureContents contents)
    {
        ReleaseTextures();
        var device = RenderSystem.GetDevice();
        var addressMode = contents.Clamp ? AddressMode.ClampToEdge : AddressMode.Repeat;
        var filterMode = contents.Blur ? FilterMode.Linear : FilterMode.Nearest;
        _sampler = RenderSystem.GetSamplerCache().GetSampler(addressMode, addressMode, filterMode, filterMode, false);
        _texture = device.CreateTexture(ResourceId.ToString(), GpuTexture.UsageTextureBinding | GpuTexture.UsageCopyDst,
            GpuFormat.Rgba8Unorm, contents.Width, contents.Height, 1, 1);
        _texture.Upload(contents.Pixels);
        _textureView = device.CreateTextureView(_texture);
    }

    public abstract TextureContents LoadContents(ResourceManager resourceManager);
}
