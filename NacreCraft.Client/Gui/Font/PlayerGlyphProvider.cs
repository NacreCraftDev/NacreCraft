using System.Numerics;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Rendertype;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;

namespace NacreCraft.Client.Gui.Font;

//PlayerGlyphProvider renders a player head as a text glyph, maps to vanilla net.minecraft.client.gui.font.PlayerGlyphProvider
//The head is the 8x8 face quad (and the hat quad when requested) sampled from the 64x64 skin texture
public sealed class PlayerGlyphProvider
{
    private static readonly IGlyphInfo GlyphInfo = IGlyphInfo.Simple(8.0f);

    private readonly PlayerSkinRenderCache _cache;
    private readonly Dictionary<FontDescription.PlayerSprite, GlyphSource> _sources = new();

    public PlayerGlyphProvider(PlayerSkinRenderCache cache) => _cache = cache;

    public GlyphSource SourceForPlayer(FontDescription.PlayerSprite playerInfo)
    {
        if (_sources.TryGetValue(playerInfo, out var cached)) return cached;
        var skin = _cache.CreateLookup(playerInfo);
        var source = new SingleSpriteSource(new SkinGlyph(skin, playerInfo.Hat));
        _sources[playerInfo] = source;
        return source;
    }

    private sealed class SkinGlyph(Func<PlayerSkinRenderCache.RenderInfo> skin, bool hat) : BakedGlyph
    {
        public override IGlyphInfo Info => GlyphInfo;

        public override void Render(IGuiRenderContext context, in GlyphRenderOptions options)
        {
        }

        public override TextRenderable.Styled? CreateGlyph(float x, float y, int color, int shadowColor,
            Style style, float boldOffset, float shadowOffset)
            => new SkinRenderable(skin, hat, x, y, color, shadowColor, shadowOffset, style);
    }

    private sealed record SkinRenderable(
        Func<PlayerSkinRenderCache.RenderInfo> Skin, bool Hat,
        float XValue, float YValue, int ColorValue, int ShadowColorValue, float ShadowOffsetValue, Style StyleValue)
        : PlainTextRenderable
    {
        public void Render(Matrix4x4 pose, VertexConsumer buffer, int packedLightCoords, bool flat)
        {
            var frontDepth = 0.0f;
            if (ShadowColor() != 0)
            {
                RenderSprite(pose, buffer, packedLightCoords, ShadowOffset(), ShadowOffset(), 0.0f, ShadowColor());
                if (!flat) frontDepth += 0.03f;
            }
            RenderSprite(pose, buffer, packedLightCoords, 0.0f, 0.0f, frontDepth, Color());
        }

        public void RenderSprite(Matrix4x4 pose, VertexConsumer buffer, int packedLightCoords,
            float offsetX, float offsetY, float z, int color)
        {
            var x0 = offsetX + Left();
            var x1 = offsetX + Right();
            var y0 = offsetY + Top();
            var y1 = offsetY + Bottom();
            RenderQuad(pose, buffer, packedLightCoords, x0, x1, y0, y1, z, color, 8f, 8f);
            if (Hat) RenderQuad(pose, buffer, packedLightCoords, x0, x1, y0, y1, z, color, 40f, 8f);
        }

        private static void RenderQuad(Matrix4x4 pose, VertexConsumer buffer, int packedLightCoords,
            float x0, float x1, float y0, float y1, float z, int color, float u, float v)
        {
            const float textureWidth = 64f;
            const float textureHeight = 64f;
            const float size = 8f;
            var u0 = u / textureWidth;
            var u1 = (u + size) / textureWidth;
            var v0 = v / textureHeight;
            var v1 = (v + size) / textureHeight;
            buffer.SetColor(color);
            buffer.SetLight(packedLightCoords);
            buffer.SetUv(u0, v0);
            buffer.AddVertex(pose, x0, y0, z);
            buffer.SetColor(color);
            buffer.SetLight(packedLightCoords);
            buffer.SetUv(u0, v1);
            buffer.AddVertex(pose, x0, y1, z);
            buffer.SetColor(color);
            buffer.SetLight(packedLightCoords);
            buffer.SetUv(u1, v1);
            buffer.AddVertex(pose, x1, y1, z);
            buffer.SetColor(color);
            buffer.SetLight(packedLightCoords);
            buffer.SetUv(u1, v0);
            buffer.AddVertex(pose, x1, y0, z);
        }

        public RenderType RenderType(DisplayMode displayMode) => Skin().GetGlyphRenderTypes().SelectRenderType(displayMode);

        public GpuTextureView TextureView() => Skin().TextureView();

        public RenderPipeline GuiPipeline() => Skin().GetGlyphRenderTypes().GuiPipeline;

        public float X() => XValue;

        public float Y() => YValue;

        public int Color() => ColorValue;

        public int ShadowColor() => ShadowColorValue;

        public float ShadowOffset() => ShadowOffsetValue;

        public float Width() => PlainTextRenderable.DefaultWidth;

        public float Height() => PlainTextRenderable.DefaultHeight;

        public float Ascent() => PlainTextRenderable.DefaultAscent;

        public float Left() => X();

        public float Right() => X() + Width();

        public float Top() => Y() + 7.0f - Ascent();

        public float Bottom() => Top() + Height();

        public Style Style => StyleValue;
    }
}

