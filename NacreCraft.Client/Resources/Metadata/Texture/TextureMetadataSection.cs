using System.Text.Json;
using NacreCraft.Client.Render.Texture;

namespace NacreCraft.Client.Resources.Metadata.Texture;

//TextureMetadataSection parses the texture section of .mcmeta, maps to vanilla TextureMetadataSection
//Root structure {"texture":{"blur":false,"clamp":false,"mipmap_strategy":"auto","alpha_cutoff_bias":0.0}}
public sealed record TextureMetadataSection(
    bool Blur,
    bool Clamp,
    MipmapStrategy MipmapStrategy,
    float AlphaCutoffBias)
{
    public const bool DefaultBlur = false;
    public const bool DefaultClamp = false;
    public const float DefaultAlphaCutoffBias = 0f;

    public static readonly TextureMetadataSection Default =
        new(DefaultBlur, DefaultClamp, MipmapStrategy.Auto, DefaultAlphaCutoffBias);

    //Parse returns the default section when the root has no texture section
    public static TextureMetadataSection Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("texture", out var texture)
            || texture.ValueKind != JsonValueKind.Object)
            return Default;

        return new TextureMetadataSection(
            texture.TryGetProperty("blur", out var blur) && blur.ValueKind == JsonValueKind.True,
            texture.TryGetProperty("clamp", out var clamp) && clamp.ValueKind == JsonValueKind.True,
            MipmapStrategyExtensions.Parse(texture.TryGetProperty("mipmap_strategy", out var mipmap) ? mipmap.GetString() : null),
            texture.TryGetProperty("alpha_cutoff_bias", out var alpha) ? (float)alpha.GetDouble() : DefaultAlphaCutoffBias);
    }
}
