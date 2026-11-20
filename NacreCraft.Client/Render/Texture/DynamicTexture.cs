using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Textures;

namespace NacreCraft.Client.Render.Texture;

//DynamicTexture a texture whose pixels are supplied from memory, maps to vanilla net.minecraft.client.renderer.texture.DynamicTexture
//Used by the missing-texture checkerboard and by anything that generates an image at runtime
public sealed class DynamicTexture : AbstractTexture
{
    private byte[] _pixels;
    private readonly int _width;
    private readonly int _height;

    public DynamicTexture(string label, byte[] pixels, int width, int height)
    {
        _pixels = pixels;
        _width = width;
        _height = height;
        CreateTexture(label);
        Upload();
    }

    public DynamicTexture(string label, int width, int height, bool zero = true)
    {
        _width = width;
        _height = height;
        _pixels = new byte[width * height * 4];
        if (!zero) Array.Fill(_pixels, (byte)0xFF);
        CreateTexture(label);
    }

    private void CreateTexture(string label)
    {
        var device = RenderSystem.GetDevice();
        _texture = device.CreateTexture(label, GpuTexture.UsageTextureBinding | GpuTexture.UsageCopyDst,
            GpuFormat.Rgba8Unorm, _width, _height, 1, 1);
        _sampler = RenderSystem.GetSamplerCache().GetRepeat(FilterMode.Nearest);
        _textureView = device.CreateTextureView(_texture);
    }

    //Upload pushes the current CPU pixels to the GPU
    public void Upload() => _texture?.Upload(_pixels);

    public byte[] GetPixels() => _pixels;

    public void SetPixels(byte[] pixels)
    {
        _pixels = pixels;
    }

    public override void Dispose()
    {
        _pixels = Array.Empty<byte>();
        base.Dispose();
    }
}
