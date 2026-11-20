using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d;

namespace NacreCraft.Client.Render.Texture;

//OverlayTexture the 16x16 entity color overlay (red hurt flash + white damage flash), maps to vanilla net.minecraft.client.renderer.texture.OverlayTexture
public sealed class OverlayTexture : IDisposable
{
    private const int Size = 16;

    public const int NoWhiteU = 0;
    public const int RedOverlayV = 3;
    public const int WhiteOverlayV = 10;
    public static readonly int NoOverlay = Pack(0, 10);

    private readonly DynamicTexture _texture = new("Entity Color Overlay", Size, Size, false);

    public OverlayTexture()
    {
        var pixels = _texture.GetPixels();
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var offset = (y * Size + x) * 4;
                if (y < 8)
                {
                    //0xB3000000, the reddish hurt overlay
                    pixels[offset] = 0;
                    pixels[offset + 1] = 0;
                    pixels[offset + 2] = 0;
                    pixels[offset + 3] = 179;
                }
                else
                {
                    var alpha = (byte)((1.0f - (float)x / 15.0f * 0.75f) * 255.0f);
                    pixels[offset] = 255;
                    pixels[offset + 1] = 255;
                    pixels[offset + 2] = 255;
                    pixels[offset + 3] = alpha;
                }
            }
        }

        _texture.Upload();
    }

    public static int U(float whiteOverlayProgress) => (int)(whiteOverlayProgress * 15.0f);

    public static int V(bool hurtOverlay) => hurtOverlay ? RedOverlayV : WhiteOverlayV;

    public static int Pack(int u, int v) => u | v << 16;

    public static int Pack(float whiteOverlayProgress, bool redOverlay) => Pack(U(whiteOverlayProgress), V(redOverlay));

    public GpuTextureView GetTextureView() => _texture.GetTextureView();

    public void Dispose() => _texture.Dispose();
}
