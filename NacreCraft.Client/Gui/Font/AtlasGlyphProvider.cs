using System.Numerics;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render.Rendertype;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;

namespace NacreCraft.Client.Gui.Font;

//AtlasGlyphProvider turns texture-atlas sprites into glyph sources, maps to vanilla net.minecraft.client.gui.font.AtlasGlyphProvider
//Each sprite becomes a PlainTextRenderable that draws the sprite with its atlas UVs; sprite selection is cached per identifier
public sealed class AtlasGlyphProvider
{
    private static readonly IGlyphInfo GlyphInfo = IGlyphInfo.Simple(8.0f);

    private readonly TextureAtlas _atlas;
    private readonly GlyphRenderTypes _renderTypes;
    private readonly GlyphSource _missingWrapper;
    private readonly Dictionary<Identifier, GlyphSource> _wrapperCache = new();

    public AtlasGlyphProvider(TextureAtlas atlas)
    {
        _atlas = atlas;
        _renderTypes = GlyphRenderTypes.CreateForColorTexture(atlas.Location);
        _missingWrapper = CreateSprite(atlas.MissingSprite);
    }

    //SourceForSprite returns the glyph source drawing the given sprite, falling back to the missing sprite
    public GlyphSource SourceForSprite(Identifier spriteId)
    {
        if (_wrapperCache.TryGetValue(spriteId, out var cached)) return cached;
        var sprite = _atlas.GetSprite(spriteId);
        var source = ReferenceEquals(sprite, _atlas.MissingSprite) ? _missingWrapper : CreateSprite(sprite);
        _wrapperCache[spriteId] = source;
        return source;
    }

    private GlyphSource CreateSprite(TextureAtlasSprite sprite)
        => new SingleSpriteSource(new SpriteGlyph(_atlas, _renderTypes, sprite));

    private sealed class SpriteGlyph(TextureAtlas atlas, GlyphRenderTypes renderTypes, TextureAtlasSprite sprite) : BakedGlyph
    {
        public override IGlyphInfo Info => GlyphInfo;

        public override void Render(IGuiRenderContext context, in GlyphRenderOptions options)
        {
        }

        public override TextRenderable.Styled? CreateGlyph(float x, float y, int color, int shadowColor,
            Style style, float boldOffset, float shadowOffset)
            => new SpriteRenderable(renderTypes, atlas.GetTextureView(), sprite, x, y, color, shadowColor, shadowOffset, style);
    }

    private sealed record SpriteRenderable(
        GlyphRenderTypes RenderTypes, GpuTextureView View, TextureAtlasSprite Sprite,
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
            buffer.SetColor(color);
            buffer.SetLight(packedLightCoords);
            buffer.SetUv(Sprite.U0, Sprite.V0);
            buffer.AddVertex(pose, x0, y0, z);
            buffer.SetColor(color);
            buffer.SetLight(packedLightCoords);
            buffer.SetUv(Sprite.U0, Sprite.V1);
            buffer.AddVertex(pose, x0, y1, z);
            buffer.SetColor(color);
            buffer.SetLight(packedLightCoords);
            buffer.SetUv(Sprite.U1, Sprite.V1);
            buffer.AddVertex(pose, x1, y1, z);
            buffer.SetColor(color);
            buffer.SetLight(packedLightCoords);
            buffer.SetUv(Sprite.U1, Sprite.V0);
            buffer.AddVertex(pose, x1, y0, z);
        }

        public RenderType RenderType(DisplayMode displayMode) => RenderTypes.SelectRenderType(displayMode);

        public GpuTextureView TextureView() => View;

        public RenderPipeline GuiPipeline() => RenderTypes.GuiPipeline;

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
