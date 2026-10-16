using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;
using NacreCraft.Client.Blaze3d;
namespace NacreCraft.Client.Render.Texture;

//ItemTextureAtlas item texture atlas, maps to vanilla TextureAtlas
//The PoC procedurally generates a 16x16 single-texture atlas for 3D item sampling
//The full version should load from textures/texture_atlas.png and split by sprite metadata
//Vertex UVs are [0,1] coordinates relative to the atlas; the shader samples directly with texture(sampler, fragUv)
//GenerateTestTexture generates a checkerboard test texture so UV sampling is visible with a different color per cell
public sealed class ItemTextureAtlas : IDisposable
{
    public const int AtlasSize = 16;

    private readonly GpuDevice? _device;
    private GpuTexture? _texture;
    private GpuSampler? _sampler;
    private bool _disposed;

    public ItemTextureAtlas(GpuDevice? device = null)
    {
        _device = device;
        if (device?.SupportsGpuRendering == true)
            CreateResources();
    }

    //Texture item texture atlas; null means no GPU backend
    public GpuTexture? Texture => _texture;
    //Sampler item texture sampler; null means no GPU backend
    public GpuSampler? Sampler => _sampler;

    //CreateResources creates the GPU texture + sampler, called only when SupportsGpuRendering=true
    private void CreateResources()
    {
        _texture = _device!.CreateTexture(null, GpuTexture.UsageTextureBinding, GpuFormat.Rgba8Unorm, AtlasSize, AtlasSize, 1, 1);
        _texture.Upload(GenerateTestTexture());
        //The small item texture atlas uses nearest to keep the pixel feel, matching vanilla Minecraft's pixel style
        _sampler = _device.CreateSampler(AddressMode.ClampToEdge, AddressMode.ClampToEdge, FilterMode.Nearest, FilterMode.Nearest, 1, null);
    }

    //GenerateTestTexture generates a 16x16 RGBA checkerboard test texture
    //4x4 cells of 8x8 pixels each, alternating light and dark gray to verify UV sampling
    //2 cells horizontally and 2 vertically (0,0)=dark gray (1,0)=light gray (0,1)=light gray (1,1)=dark gray
    public static byte[] GenerateTestTexture()
    {
        var pixels = new byte[AtlasSize * AtlasSize * 4];
        var cellSize = AtlasSize / 2;
        for (int y = 0; y < AtlasSize; y++)
        {
            for (int x = 0; x < AtlasSize; x++)
            {
                var cellX = x / cellSize;
                var cellY = y / cellSize;
                //Checkerboard alternating light and dark gray
                var isLight = (cellX + cellY) % 2 == 0;
                var v = (byte)(isLight ? 200 : 80);
                var idx = (y * AtlasSize + x) * 4;
                pixels[idx] = v;
                pixels[idx + 1] = v;
                pixels[idx + 2] = v;
                pixels[idx + 3] = 255;
            }
        }
        return pixels;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _texture?.Dispose();
        _sampler?.Dispose();
        _disposed = true;
    }
}

//ItemTints item tint table, maps to vanilla net.minecraft.client.color.item.ItemTints
//Vanilla looks up dyeColor/potionColor/leatherColor by tintIndex; the PoC simplifies to static white
//tintIndex=-1 means no tint and returns white 0xFFFFFFFF
//PackColor packs RGBA 4 bytes into an ARGB int, maps to the vanilla vertex color format
public static class ItemTints
{
    //White white packed ARGB 0xFFFFFFFF = -1 (int), maps to the vanilla default tint
    public const int White = unchecked((int)0xFFFFFFFF);

    //PackColor packs RGBA into an ARGB int with the high 8 bits A then BGR
    //maps to the vanilla vertex color format; the shader unpacks with bit ops like (color>>16)&0xFF for R
    public static int PackColor(byte r, byte g, byte b, byte a = 255)
        => (a << 24) | (r << 16) | (g << 8) | b;

    //GetTint returns the tint color by tintIndex; the PoC always returns white
    //tintIndex=-1 or unknown both return white; the full version looks up the tint table by tintIndex
    public static int GetTint(int tintIndex) => White;
}
