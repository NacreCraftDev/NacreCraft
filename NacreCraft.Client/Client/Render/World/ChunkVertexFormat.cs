using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Vertex;

namespace NacreCraft.Client.Render.World;

//ChunkVertexFormat terrain chunk vertex layout, aligned with Sodium's CompactChunkVertex
//20 bytes per vertex; position/texcoord/light are quantized so the terrain mesh keeps the upload bandwidth low
//loc0 a_Position    RG32_UINT   two u32 interleaving three 10-bit section-local coordinates
//loc1 a_Color       RGBA8_UNORM face shade + tint already baked in
//loc2 a_TexCoord    RG16_UINT  two 15-bit sprite coords plus one bias-sign bit used against texture bleed
//loc3 a_LightAndData RGBA8_UINT block|sky light, material bits and the section index
//The position is section-local; the shader adds the section origin, so a mesh stays valid wherever the section moves
public static class ChunkVertexFormat
{
    public const int Stride = 20;

    //Position quantization maps the [-8, 24) window around the section origin onto 20 bits
    //The extra margin beyond 0-16 lets extended block models reach past the section boundary
    public const int PositionMaxValue = 1 << 20;
    public const float ModelOrigin = 8.0f;
    public const float ModelRange = 32.0f;

    //TexCoord keeps 15 bits of the sprite coordinate; the remaining bit carries the bias sign
    public const int TextureMaxValue = 1 << 15;

    //This is Sodium's compact layout, not the vanilla BLOCK format: the position is a packed uvec2 rather than
    //RGB32_FLOAT, so the generic BufferBuilder path cannot consume it. Only the terrain shader unpacks it.
    public static readonly VertexFormat Format = VertexFormat.Create()
        .AddAttribute("Position", GpuFormat.Rg32Uint)
        .AddAttribute("Color", GpuFormat.Rgba8Unorm)
        .AddAttribute("TexCoord", GpuFormat.Rg16Uint)
        .AddAttribute("LightAndData", GpuFormat.Rgba8Uint)
        .Build();

    //PackPosition hi word carries the upper 10 bits of each coordinate, lo word the lower 10
    public static int PackPositionHi(int x, int y, int z)
        => ((x >>> 10) & 0x3FF) | (((y >>> 10) & 0x3FF) << 10) | (((z >>> 10) & 0x3FF) << 20);

    public static int PackPositionLo(int x, int y, int z)
        => (x & 0x3FF) | ((y & 0x3FF) << 10) | ((z & 0x3FF) << 20);

    //QuantizePosition converts a section-local coordinate into its 20-bit fixed point form
    public static int QuantizePosition(float position)
        => ((int)((ModelOrigin + position) / ModelRange * PositionMaxValue)) & 0xFFFFF;

    //PackTexture stores u in the low 16 bits and v in the high 16 bits
    public static int PackTexture(int u, int v) => (u & 0xFFFF) | ((v & 0xFFFF) << 16);

    //EncodeTexture shrinks the coordinate toward the sprite center and records only the bias sign
    //The GPU re-applies the bias with a tiny epsilon, which keeps texture bleed away without spending bits on it
    public static int EncodeTexture(float center, float x)
    {
        var bias = x < center ? 1 : -1;
        var quantized = (int)MathF.Round(x * TextureMaxValue) + bias;
        return (quantized & 0x7FFF) | (Sign(bias) << 15);
    }

    //EncodeLight packs the 0-15 block and sky levels into the low and high byte
    //The shader turns them back into a lightmap uv as (level + 0.5) / 16, matching the vanilla packed light semantics
    public static int EncodeLight(int blockLight, int skyLight)
    {
        var block = Math.Clamp(blockLight, 0, 15);
        var sky = Math.Clamp(skyLight, 0, 15);
        return block | (sky << 8);
    }

    //PackLightAndData lays out light, material bits and the section index into the fourth attribute
    public static int PackLightAndData(int light, int materialBits, int sectionIndex)
        => (light & 0xFFFF) | ((materialBits & 0xFF) << 16) | ((sectionIndex & 0xFF) << 24);

    //Sign shifts the sign bit down to the least significant bit
    private static int Sign(int x) => x >>> 31;
}
