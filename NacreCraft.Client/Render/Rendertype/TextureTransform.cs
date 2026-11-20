using System.Numerics;

namespace NacreCraft.Client.Render.Rendertype;

//TextureTransform builds the texture matrix uploaded in DynamicTransforms, maps to vanilla net.minecraft.client.renderer.rendertype.TextureTransform
//The glint variants scroll the enchantment texture over time; NC reads the default glint speed because the options screen is not ported yet
public class TextureTransform
{
    public const double MAX_ENCHANTMENT_GLINT_SPEED_MILLIS = 8.0;

    //DEFAULT_GLINT_SPEED vanilla options.glintSpeed default
    private const double DefaultGlintSpeed = 0.5;

    private readonly string _name;
    private readonly Func<Matrix4x4> _supplier;

    public static readonly TextureTransform DEFAULT_TEXTURING = new("default_texturing", static () => Matrix4x4.Identity);
    public static readonly TextureTransform GLINT_TEXTURING = new("glint_texturing", () => SetupGlintTexturing(8.0f));
    public static readonly TextureTransform ENTITY_GLINT_TEXTURING = new("entity_glint_texturing", () => SetupGlintTexturing(0.5f));
    public static readonly TextureTransform ARMOR_ENTITY_GLINT_TEXTURING = new("armor_entity_glint_texturing", () => SetupGlintTexturing(0.16f));

    public TextureTransform(string name, Func<Matrix4x4> matrix)
    {
        _name = name;
        _supplier = matrix;
    }

    public Matrix4x4 CreateMatrix() => _supplier();

    public override string ToString() => $"TexturingStateShard[{_name}]";

    private static Matrix4x4 SetupGlintTexturing(float scale)
    {
        var glintSpeed = NacreCraft.Client.MinecraftClient.Current?.GpuApp?.RenderState.OptionsRenderState.GlintSpeed ?? DefaultGlintSpeed;
        if (glintSpeed <= 0.0) glintSpeed = DefaultGlintSpeed;
        var millis = (long)(Environment.TickCount64 * glintSpeed * MAX_ENCHANTMENT_GLINT_SPEED_MILLIS);
        var layerOffset0 = (float)(millis % 110000L) / 110000.0f;
        var layerOffset1 = (float)(millis % 30000L) / 30000.0f;
        //Row-vector order: scale the texture, rotate the glint, then scroll it, matching the vanilla translate/rotate/scale chain
        return Matrix4x4.CreateScale(scale)
               * Matrix4x4.CreateRotationZ(0.17453292f)
               * Matrix4x4.CreateTranslation(-layerOffset0, layerOffset1, 0.0f);
    }

    //OffsetTextureTransform shifts the texture by a fixed offset, maps to vanilla TextureTransform.OffsetTextureTransform
    public sealed class OffsetTextureTransform : TextureTransform
    {
        public OffsetTextureTransform(float uOffset, float vOffset)
            : base("offset_texturing", () => Matrix4x4.CreateTranslation(uOffset, vOffset, 0.0f))
        {
        }
    }
}
