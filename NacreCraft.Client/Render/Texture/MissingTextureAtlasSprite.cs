using NacreCraft.Registry;

namespace NacreCraft.Client.Render.Texture;

//MissingTextureAtlasSprite builds the vanilla magenta/black checkerboard used for missing textures, maps to net.minecraft.client.renderer.texture.MissingTextureAtlasSprite
public static class MissingTextureAtlasSprite
{
    private const int MissingImageWidth = 16;
    private const int MissingImageHeight = 16;
    private const string MissingTextureName = "missingno";

    private static readonly Identifier MissingTextureLocation = Identifier.WithDefaultNamespace(MissingTextureName);

    //GenerateMissingImage builds the 16x16 RGBA checkerboard, the magenta cells are 0xFFF800F8 and the others black
    public static (byte[] Pixels, int Width, int Height) GenerateMissingImage() => GenerateMissingImage(MissingImageWidth, MissingImageHeight);

    public static (byte[] Pixels, int Width, int Height) GenerateMissingImage(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                var magenta = (y < height / 2) ^ (x < width / 2);
                if (magenta)
                {
                    pixels[offset] = 248;
                    pixels[offset + 1] = 0;
                    pixels[offset + 2] = 248;
                    pixels[offset + 3] = 255;
                }
                else
                {
                    pixels[offset] = 0;
                    pixels[offset + 1] = 0;
                    pixels[offset + 2] = 0;
                    pixels[offset + 3] = 255;
                }
            }
        }

        return (pixels, width, height);
    }

    public static Identifier GetLocation() => MissingTextureLocation;
}
