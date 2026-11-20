using NacreCraft.Resources;
using NacreCraft.Registry;
using StbImageSharp;

namespace NacreCraft.Client.Render.Texture;

//TextureContents decoded pixels plus sampler hints for one texture file, maps to vanilla net.minecraft.client.renderer.texture.TextureContents
public sealed class TextureContents
{
    public byte[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }
    public bool Blur { get; }
    public bool Clamp { get; }

    public TextureContents(byte[] pixels, int width, int height, bool blur = false, bool clamp = false)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        Blur = blur;
        Clamp = clamp;
    }

    //Load decodes the PNG at the location, falling back to the missing checkerboard when absent or broken
    public static TextureContents Load(ResourceManager resourceManager, Identifier location)
    {
        var resourceLocation = location.WithPrefix("textures/").WithSuffix(".png");
        var resource = resourceManager.GetResource(PackType.ClientResources, resourceLocation);
        if (resource is null) return CreateMissing();
        try
        {
            using var stream = resource.Open();
            var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            return new TextureContents(result.Data, result.Width, result.Height);
        }
        catch
        {
            return CreateMissing();
        }
    }

    public static TextureContents CreateMissing()
    {
        var (pixels, width, height) = MissingTextureAtlasSprite.GenerateMissingImage();
        return new TextureContents(pixels, width, height);
    }
}
