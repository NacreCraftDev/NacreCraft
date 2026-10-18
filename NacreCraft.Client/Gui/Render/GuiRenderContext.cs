using System.Numerics;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Render;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Resources.Metadata.Gui;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;

namespace NacreCraft.Client.Gui.Render;

//GuiRenderContext submission-layer IGuiRenderContext implementation
//Converts widget DrawQuad/DrawText/DrawImage calls into RenderStates submitted to GuiRenderState
//The coordinate system is uniformly actual pixels; widget scaled coordinates are converted by *guiScale
//Stage 5b replaces VulkanGuiRenderer's submission duty; the render phase is handed to GuiRenderer
//F7 DrawText prefers the GlyphFont dynamic bake path with FontAtlas as fallback
//P0 DrawSprite dispatched by scaling after GuiSpriteManager parses .mcmeta
public sealed class GuiRenderContext : IGuiRenderContext
{
    private readonly GuiRenderState _renderState;
    private readonly int _guiScale;
    private readonly int _surfaceWidth;
    private readonly int _surfaceHeight;
    private readonly GlyphFont? _font;
    private readonly FontAtlas? _fontAtlas;
    private readonly TextureSetup? _fontTexture;
    private readonly Func<int, TextureSetup?> _textureResolver;
    private readonly GuiSpriteManager? _spriteManager;
    //_recordingStack recording stack supporting nested child cache and window cache active at once
    //BeginRecording pushes EndRecording pops Submit writes to all stack levels ReplayRange writes to the top
    //An empty stack means not recording, and Submit only goes to GuiRenderState
    private readonly Stack<List<GuiElementRenderState>> _recordingStack = new();

    //Pose the top stores the actual pose; PushPose's scaled delta is converted to actual then pushed
    private readonly Stack<Matrix3x2> _poseStack = new();
    //Scissor the stack stores actual pixels used directly by vkCmdSetScissor
    private readonly Stack<ScreenRectangle> _scissorStack = new();

    public GuiRenderContext(GuiRenderState renderState, int surfaceWidth, int surfaceHeight, int guiScale,
        GlyphFont? font, FontAtlas? fontAtlas, TextureSetup? fontTexture, Func<int, TextureSetup?> textureResolver,
        GuiSpriteManager? spriteManager = null)
    {
        _renderState = renderState;
        _surfaceWidth = surfaceWidth;
        _surfaceHeight = surfaceHeight;
        _guiScale = guiScale <= 0 ? 1 : guiScale;
        _font = font;
        _fontAtlas = fontAtlas;
        _fontTexture = fontTexture;
        _textureResolver = textureResolver;
        _spriteManager = spriteManager;
    }

    //BeginFrame resets the Pose/Scissor stacks, called by the caller after GuiRenderState.Reset each frame
    public void BeginFrame()
    {
        _poseStack.Clear();
        _poseStack.Push(Matrix3x2.Identity);
        _scissorStack.Clear();
        _scissorStack.Push(new ScreenRectangle(0, 0, _surfaceWidth, _surfaceHeight));
    }

    //DrawQuad draws a solid rectangle, submitting a ColoredRectangleRenderState to the GUI pipeline
    public void DrawQuad(int x, int y, int width, int height, GuiColor color)
    {
        var (ax, ay, ax1, ay1) = ToActual(x, y, width, height);
        var state = new ColoredRectangleRenderState(
            RenderPipelines.GUI, TextureSetup.NoTexture, _poseStack.Peek(),
            ax, ay, ax1, ay1,
            ToIntColor(color), ToIntColor(color), _scissorStack.Peek());
        Submit(state);
    }

    //DrawQuadInverted draws an inverted rectangle, submitting a ColoredRectangleRenderState to the GUI_INVERT pipeline
    public void DrawQuadInverted(int x, int y, int width, int height, GuiColor color)
    {
        var (ax, ay, ax1, ay1) = ToActual(x, y, width, height);
        var state = new ColoredRectangleRenderState(
            RenderPipelines.GUI_INVERT, TextureSetup.NoTexture, _poseStack.Peek(),
            ax, ay, ax1, ay1,
            ToIntColor(color), ToIntColor(color), _scissorStack.Peek());
        Submit(state);
    }

    //DrawText renders text; F7 prefers the Font dynamic bake path with FontAtlas as fallback
    //The Font path calls Font.Draw, iterating text Bake+Render and submitting GlyphBlitRenderState
    //The FontAtlas path submits one BlitRenderState per glyph to the GUI_TEXT pipeline; that path has no shadow
    public void DrawText(int x, int y, string text, GuiColor color, bool shadow = true)
    {
        if (string.IsNullOrEmpty(text)) return;
        var intColor = ToIntColor(color);

        //Font dynamic bake path: BakedGlyph.Render calls DrawGlyphQuad, submitting GlyphBlitRenderState
        if (_font is not null)
        {
            //The pen position stays in GUI units: GlyphFont advances the pen by each glyph's advance, and
            //DrawGlyphQuad scales the finished quad to device pixels
            _font.Draw(this, text, x, y, intColor, shadow);
            return;
        }

        //FontAtlas fallback path
        if (_fontAtlas is null || _fontTexture is null) return;

        //y is the top; convert to baseline with Ascent to avoid the CJK misalignment caused by -descent
        float penX2 = x * _guiScale;
        float penY2 = y * _guiScale + _fontAtlas.Ascent * _guiScale;
        var pose = _poseStack.Peek();
        var scissor = _scissorStack.Peek();

        foreach (var ch in text)
        {
            var g = _fontAtlas.GetGlyph(ch);
            if (g is null)
            {
                penX2 += _fontAtlas.MeasureText("?") * _guiScale;
                continue;
            }
            var glyph = g.Value;
            float px = penX2 + glyph.OffsetX * _guiScale;
            float py = penY2 + glyph.OffsetY * _guiScale;
            float pw = glyph.Width * _guiScale;
            float ph = glyph.Height * _guiScale;
            if (pw > 0 && ph > 0)
            {
                var state = new BlitRenderState(
                    RenderPipelines.GUI_TEXT, _fontTexture, pose,
                    (int)px, (int)py, (int)(px + pw), (int)(py + ph),
                    glyph.U0, glyph.U1, glyph.V0, glyph.V1,
                    intColor, scissor);
                Submit(state);
            }
            penX2 += glyph.Advance * _guiScale;
        }
    }

    //DrawGlyphQuad submits a glyph quad to the render context; 4 float vertices support italic/bold offsets
    //Called by SheetBakedGlyph.Render with glyph metrics, which are in GUI units; this is where they are scaled to
    //device pixels, exactly like the other draw calls scale their coordinates
    //pose/scissor are read from the stack top; color is an ARGB int
    public void DrawGlyphQuad(RenderPipeline pipeline, TextureSetup textureSetup,
        float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
        float u0, float v0, float u1, float v1, int color)
    {
        var scale = _guiScale;
        var state = new GlyphBlitRenderState(pipeline, textureSetup, _poseStack.Peek(),
            x0 * scale, y0 * scale, x1 * scale, y1 * scale, x2 * scale, y2 * scale, x3 * scale, y3 * scale,
            u0, v0, u1, v1, color, _scissorStack.Peek());
        Submit(state);
    }

    //MeasureText returns the scaled pixel width for widget layout
    //Font takes priority, returning Font.MeasureText *guiScale, otherwise uses FontAtlas
    public float MeasureText(string text)
    {
        //Reported in GUI units: the caller combines it with widget coordinates, which are unscaled as well
        if (_font is not null) return _font.MeasureText(text);
        return _fontAtlas?.MeasureText(text) ?? 0;
    }

    //LineHeight returns the line height in GUI units, so widgets can space multi-line text with their own coordinates
    //Font takes priority, otherwise uses FontAtlas
    public int LineHeight => _font?.LineHeight ?? _fontAtlas?.LineHeight ?? 0;

    //DrawImage draws a texture sub-region, submitting a BlitRenderState to the GUI_TEXTURED pipeline
    //UVs are computed from src pixels and the GpuTexture size; textureId is resolved to a TextureSetup via textureResolver
    //srcW/srcH<=0 means the full texture size, so the caller need not know the texture size
    public void DrawImage(int textureId, int x, int y, int width, int height,
        int srcX, int srcY, int srcW, int srcH, GuiColor tint)
    {
        var texture = _textureResolver(textureId);
        if (texture?.Texture0 is not { } img) return;
        var imgWidth = img.Width;
        var imgHeight = img.Height;
        if (imgWidth <= 0 || imgHeight <= 0) return;
        if (srcW <= 0) srcW = imgWidth;
        if (srcH <= 0) srcH = imgHeight;

        float u0 = srcX / (float)imgWidth;
        float v0 = srcY / (float)imgHeight;
        float u1 = (srcX + srcW) / (float)imgWidth;
        float v1 = (srcY + srcH) / (float)imgHeight;

        var (ax, ay, ax1, ay1) = ToActual(x, y, width, height);
        var state = new BlitRenderState(
            RenderPipelines.GUI_TEXTURED, texture, _poseStack.Peek(),
            ax, ay, ax1, ay1,
            u0, u1, v0, v1,
            ToIntColor(tint), _scissorStack.Peek());
        Submit(state);
    }

    //DrawImageNinePatch legacy API with a single border keeps the original nine-slice implementation and does not delegate to the new API
    //A uniform border on all sides with a fixed stretched center, equivalent to stretchInner=true
    //When the target/source size is under 2*border it falls back to DrawImage to avoid negative regions, matching existing button behavior
    public void DrawImageNinePatch(int textureId, int x, int y, int width, int height,
        int srcX, int srcY, int srcW, int srcH, int border, GuiColor tint)
    {
        if (srcW <= 0 || srcH <= 0)
        {
            var tex = _textureResolver(textureId);
            if (tex?.Texture0 is not { } full) return;
            if (srcW <= 0) srcW = full.Width;
            if (srcH <= 0) srcH = full.Height;
        }
        if (border <= 0 || width < border * 2 || height < border * 2 || srcW < border * 2 || srcH < border * 2)
        {
            DrawImage(textureId, x, y, width, height, srcX, srcY, srcW, srcH, tint);
            return;
        }
        var dw = width - border * 2;
        var dh = height - border * 2;
        var sw = srcW - border * 2;
        var sh = srcH - border * 2;
        var dxL = x;
        var dxM = x + border;
        var dxR = x + width - border;
        var dyT = y;
        var dyM = y + border;
        var dyB = y + height - border;
        var sxL = srcX;
        var sxM = srcX + border;
        var sxR = srcX + srcW - border;
        var syT = srcY;
        var syM = srcY + border;
        var syB = srcY + srcH - border;
        //4 corners fixed
        DrawImage(textureId, dxL, dyT, border, border, sxL, syT, border, border, tint);
        DrawImage(textureId, dxR, dyT, border, border, sxR, syT, border, border, tint);
        DrawImage(textureId, dxL, dyB, border, border, sxL, syB, border, border, tint);
        DrawImage(textureId, dxR, dyB, border, border, sxR, syB, border, border, tint);
        //Top/bottom edges dw×border stretched horizontally
        DrawImage(textureId, dxM, dyT, dw, border, sxM, syT, sw, border, tint);
        DrawImage(textureId, dxM, dyB, dw, border, sxM, syB, sw, border, tint);
        //Left/right edges border×dh stretched vertically
        DrawImage(textureId, dxL, dyM, border, dh, sxL, syM, border, sh, tint);
        DrawImage(textureId, dxR, dyM, border, dh, sxR, syM, border, sh, tint);
        //Center dw×dh stretched both ways
        DrawImage(textureId, dxM, dyM, dw, dh, sxM, syM, sw, sh, tint);
    }

    //DrawImageNinePatch per-side border + stretchInner new API, maps to vanilla blitNineSlicedSprite
    //Independent borders per side supporting per-side border widgets like slider_handle/tab
    //border is clamped to half the target size to avoid negative regions, maps to vanilla Math.min(border, width/2)
    //stretchInner=true stretches the center, false tiles it at sw×sh, corresponding to the vanilla stretch_inner field
    public void DrawImageNinePatch(int textureId, int x, int y, int width, int height,
        int srcX, int srcY, int srcW, int srcH,
        int borderLeft, int borderTop, int borderRight, int borderBottom,
        bool stretchInner, GuiColor tint)
    {
        if (srcW <= 0 || srcH <= 0)
        {
            var tex = _textureResolver(textureId);
            if (tex?.Texture0 is not { } full) return;
            if (srcW <= 0) srcW = full.Width;
            if (srcH <= 0) srcH = full.Height;
        }
        //Border clamping, maps to vanilla Math.min(border, width/2) to avoid negative regions without falling back to DrawImage
        int bl = Math.Min(borderLeft, width / 2);
        int bt = Math.Min(borderTop, height / 2);
        int br = Math.Min(borderRight, width / 2);
        int bb = Math.Min(borderBottom, height / 2);
        //Vanilla shortcuts: when a dimension already matches the source, three pieces are enough and the middle band
        //keeps the full source size on the other axis. Skipping them splits an unchanged dimension into three bands,
        //which is what puts seams and a squashed center inside an otherwise intact widget texture
        if (width == srcW && height == srcH)
        {
            DrawImage(textureId, x, y, width, height, srcX, srcY, srcW, srcH, tint);
            return;
        }
        if (height == srcH)
        {
            DrawImage(textureId, x, y, bl, height, srcX, srcY, bl, srcH, tint);
            BlitInnerSegment(textureId, x + bl, y, width - br - bl, height,
                srcX + bl, srcY, srcW - br - bl, srcH, stretchInner, tint);
            DrawImage(textureId, x + width - br, y, br, height,
                srcX + srcW - br, srcY, br, srcH, tint);
            return;
        }
        if (width == srcW)
        {
            DrawImage(textureId, x, y, width, bt, srcX, srcY, srcW, bt, tint);
            BlitInnerSegment(textureId, x, y + bt, width, height - bb - bt,
                srcX, srcY + bt, srcW, srcH - bb - bt, stretchInner, tint);
            DrawImage(textureId, x, y + height - bb, width, bb,
                srcX, srcY + srcH - bb, srcW, bb, tint);
            return;
        }
        //When the source size is under the sum of borders it falls back to DrawImage
        if (srcW < bl + br || srcH < bt + bb)
        {
            DrawImage(textureId, x, y, width, height, srcX, srcY, srcW, srcH, tint);
            return;
        }
        var dw = width - bl - br;
        var dh = height - bt - bb;
        var sw = srcW - bl - br;
        var sh = srcH - bt - bb;
        //4 corners fixed
        DrawImage(textureId, x, y, bl, bt, srcX, srcY, bl, bt, tint);
        DrawImage(textureId, x + width - br, y, br, bt,
            srcX + srcW - br, srcY, br, bt, tint);
        DrawImage(textureId, x, y + height - bb, bl, bb,
            srcX, srcY + srcH - bb, bl, bb, tint);
        DrawImage(textureId, x + width - br, y + height - bb, br, bb,
            srcX + srcW - br, srcY + srcH - bb, br, bb, tint);
        //Top/bottom edges fixed-stretched; stretchInner only affects the center
        DrawImage(textureId, x + bl, y, dw, bt, srcX + bl, srcY, sw, bt, tint);
        DrawImage(textureId, x + bl, y + height - bb, dw, bb,
            srcX + bl, srcY + srcH - bb, sw, bb, tint);
        //Left/right edges fixed-stretched; stretchInner only affects the center
        DrawImage(textureId, x, y + bt, bl, dh, srcX, srcY + bt, bl, sh, tint);
        DrawImage(textureId, x + width - br, y + bt, br, dh,
            srcX + srcW - br, srcY + bt, br, sh, tint);
        //Center stretchInner=true stretches, false tiles, maps to vanilla stretch_inner
        BlitInnerSegment(textureId, x + bl, y + bt, dw, dh,
            srcX + bl, srcY + bt, sw, sh, stretchInner, tint);
    }

    //DrawTiledSprite tiles the texture by tileWidth/tileHeight, submitting a TiledBlitRenderState
    //maps to vanilla blitTiledSprite, a double loop tiling and cropping UVs proportionally at the edges
    //The NineSlice stretchInner=false center-tile path also goes through this method
    public void DrawTiledSprite(int textureId, int srcW, int srcH,
        int x, int y, int width, int height, GuiColor tint)
        => DrawTiledSprite(textureId, srcW, srcH, x, y, width, height, 0f, 1f, 0f, 1f, tint);

    //DrawTiledSprite with an explicit UV sub-rect, maps to vanilla blitTiledSprite's textureX/textureY/spriteSize
    //The nine-slice center must tile only its own band of the source texture; using the full 0..1 range would repeat
    //the whole widget texture, borders included, inside the middle segment
    public void DrawTiledSprite(int textureId, int srcW, int srcH,
        int x, int y, int width, int height, float u0, float u1, float v0, float v1, GuiColor tint)
    {
        var texture = _textureResolver(textureId);
        if (texture is null || srcW <= 0 || srcH <= 0) return;
        var (ax, ay, ax1, ay1) = ToActual(x, y, width, height);
        //The tile size arrives in GUI units while the target rectangle is already in device pixels, so it is scaled
        //too; without this a tile is repeated GuiScale times more often than intended and nine-slice centers show a seam
        var state = new TiledBlitRenderState(
            RenderPipelines.GUI_TEXTURED, texture, _poseStack.Peek(),
            srcW * _guiScale, srcH * _guiScale, ax, ay, ax1, ay1,
            u0, u1, v0, v1, ToIntColor(tint), _scissorStack.Peek());
        Submit(state);
    }

    //BlitInnerSegment center segment stretch or tile, maps to vanilla blitNineSliceInnerSegment
    //stretchInner=true goes to DrawImage stretch, false to DrawTiledSprite tile
    //dw/dh<=0 is skipped to avoid submitting an empty region
    private void BlitInnerSegment(int textureId, int dx, int dy, int dw, int dh,
        int sx, int sy, int sw, int sh, bool stretchInner, GuiColor tint)
    {
        if (dw <= 0 || dh <= 0) return;
        if (stretchInner)
        {
            DrawImage(textureId, dx, dy, dw, dh, sx, sy, sw, sh, tint);
            return;
        }
        var texture = _textureResolver(textureId);
        if (texture?.Texture0 is not { } img || img.Width <= 0 || img.Height <= 0) return;
        DrawTiledSprite(textureId, sw, sh, dx, dy, dw, dh,
            sx / (float)img.Width, (sx + sw) / (float)img.Width,
            sy / (float)img.Height, (sy + sh) / (float)img.Height, tint);
    }

    //DrawSprite looks up a sprite by identifier and dispatches by scaling, maps to vanilla blitSprite
    //The sprite is a region of the shared GUI atlas, so the UVs come from its atlas rectangle; the standalone-texture
    //overloads below stay available for callers that draw a whole registered texture
    public void DrawSprite(string identifier, int x, int y, int width, int height, GuiColor tint)
    {
        if (_spriteManager is null) return;
        var sprite = _spriteManager.GetSprite(identifier);
        if (sprite is null) return;
        switch (sprite.Scaling)
        {
            case StretchScaling:
                BlitSprite(sprite, x, y, width, height, 0, 0, sprite.Width, sprite.Height, tint);
                break;
            case TileScaling tile:
                BlitSpriteTiled(sprite, x, y, width, height, 0, 0, tile.Width, tile.Height, tint);
                break;
            case NineSliceScaling nineSlice:
                BlitSpriteNineSlice(sprite, x, y, width, height, nineSlice, tint);
                break;
        }
    }

    //BlitSprite draws one region of a sprite; sx/sy are sprite-local pixels, converted to atlas UVs here
    private void BlitSprite(GuiSprite sprite, int x, int y, int width, int height,
        int sx, int sy, int sw, int sh, GuiColor tint)
    {
        if (width <= 0 || height <= 0 || sw <= 0 || sh <= 0) return;
        var (ax, ay, ax1, ay1) = ToActual(x, y, width, height);
        var state = new BlitRenderState(
            RenderPipelines.GUI_TEXTURED, sprite.Texture, _poseStack.Peek(),
            ax, ay, ax1, ay1,
            (sprite.AtlasX + sx) / (float)sprite.AtlasWidth, (sprite.AtlasX + sx + sw) / (float)sprite.AtlasWidth,
            (sprite.AtlasY + sy) / (float)sprite.AtlasHeight, (sprite.AtlasY + sy + sh) / (float)sprite.AtlasHeight,
            ToIntColor(tint), _scissorStack.Peek());
        Submit(state);
    }

    //BlitSpriteTiled tiles one sprite region at tileWidth/tileHeight GUI units, maps to vanilla blitTiledSprite
    private void BlitSpriteTiled(GuiSprite sprite, int x, int y, int width, int height,
        int sx, int sy, int tileWidth, int tileHeight, GuiColor tint)
    {
        if (width <= 0 || height <= 0 || tileWidth <= 0 || tileHeight <= 0) return;
        var (ax, ay, ax1, ay1) = ToActual(x, y, width, height);
        var state = new TiledBlitRenderState(
            RenderPipelines.GUI_TEXTURED, sprite.Texture, _poseStack.Peek(),
            tileWidth * _guiScale, tileHeight * _guiScale, ax, ay, ax1, ay1,
            (sprite.AtlasX + sx) / (float)sprite.AtlasWidth, (sprite.AtlasX + sx + tileWidth) / (float)sprite.AtlasWidth,
            (sprite.AtlasY + sy) / (float)sprite.AtlasHeight, (sprite.AtlasY + sy + tileHeight) / (float)sprite.AtlasHeight,
            ToIntColor(tint), _scissorStack.Peek());
        Submit(state);
    }

    //BlitSpriteNineSlice maps to vanilla blitNineSlicedSprite, including the three piece shortcuts and the
    //stretch_inner rule; all coordinates are sprite-local pixels
    private void BlitSpriteNineSlice(GuiSprite sprite, int x, int y, int width, int height,
        NineSliceScaling nineSlice, GuiColor tint)
    {
        var border = nineSlice.Border;
        var srcW = sprite.Width;
        var srcH = sprite.Height;
        int bl = Math.Min(border.Left, width / 2);
        int bt = Math.Min(border.Top, height / 2);
        int br = Math.Min(border.Right, width / 2);
        int bb = Math.Min(border.Bottom, height / 2);
        if (width == srcW && height == srcH)
        {
            BlitSprite(sprite, x, y, width, height, 0, 0, srcW, srcH, tint);
            return;
        }
        if (height == srcH)
        {
            BlitSprite(sprite, x, y, bl, height, 0, 0, bl, srcH, tint);
            BlitSpriteInner(sprite, x + bl, y, width - br - bl, height,
                bl, 0, srcW - br - bl, srcH, nineSlice.StretchInner, tint);
            BlitSprite(sprite, x + width - br, y, br, height, srcW - br, 0, br, srcH, tint);
            return;
        }
        if (width == srcW)
        {
            BlitSprite(sprite, x, y, width, bt, 0, 0, srcW, bt, tint);
            BlitSpriteInner(sprite, x, y + bt, width, height - bb - bt,
                0, bt, srcW, srcH - bb - bt, nineSlice.StretchInner, tint);
            BlitSprite(sprite, x, y + height - bb, width, bb, 0, srcH - bb, srcW, bb, tint);
            return;
        }
        BlitSprite(sprite, x, y, bl, bt, 0, 0, bl, bt, tint);
        BlitSpriteInner(sprite, x + bl, y, width - br - bl, bt,
            bl, 0, srcW - br - bl, bt, nineSlice.StretchInner, tint);
        BlitSprite(sprite, x + width - br, y, br, bt, srcW - br, 0, br, bt, tint);
        BlitSprite(sprite, x, y + height - bb, bl, bb, 0, srcH - bb, bl, bb, tint);
        BlitSpriteInner(sprite, x + bl, y + height - bb, width - br - bl, bb,
            bl, srcH - bb, srcW - br - bl, bb, nineSlice.StretchInner, tint);
        BlitSprite(sprite, x + width - br, y + height - bb, br, bb, srcW - br, srcH - bb, br, bb, tint);
        BlitSpriteInner(sprite, x, y + bt, bl, height - bb - bt,
            0, bt, bl, srcH - bb - bt, nineSlice.StretchInner, tint);
        BlitSpriteInner(sprite, x + bl, y + bt, width - br - bl, height - bb - bt,
            bl, bt, srcW - br - bl, srcH - bb - bt, nineSlice.StretchInner, tint);
        BlitSpriteInner(sprite, x + width - br, y + bt, br, height - bb - bt,
            srcW - br, bt, br, srcH - bb - bt, nineSlice.StretchInner, tint);
    }

    //BlitSpriteInner draws a nine-slice middle band, stretched or tiled at its own source size
    private void BlitSpriteInner(GuiSprite sprite, int x, int y, int width, int height,
        int sx, int sy, int sw, int sh, bool stretchInner, GuiColor tint)
    {
        if (width <= 0 || height <= 0) return;
        if (stretchInner)
            BlitSprite(sprite, x, y, width, height, sx, sy, sw, sh, tint);
        else
            BlitSpriteTiled(sprite, x, y, width, height, sx, sy, sw, sh, tint);
    }

    //PushPose converts the scaled delta to actual then multiplies with the top; child widgets position relative to the parent
    //translation *guiScale rotation/scale unchanged keeping the rotation and scale ratio
    public void PushPose(Matrix3x2 delta)
    {
        var actualDelta = new Matrix3x2(delta.M11, delta.M12, delta.M21, delta.M22,
            delta.M31 * _guiScale, delta.M32 * _guiScale);
        _poseStack.Push(_poseStack.Peek() * actualDelta);
    }

    public void PopPose()
    {
        if (_poseStack.Count > 1) _poseStack.Pop();
    }

    //PushScissor scaled pixels *guiScale to actual, intersected with the top so sub-containers clip within the parent
    public void PushScissor(int x, int y, int width, int height)
    {
        var ax = x * _guiScale;
        var ay = y * _guiScale;
        var aw = width * _guiScale;
        var ah = height * _guiScale;
        var current = _scissorStack.Peek();
        var ix = Math.Max(current.X, ax);
        var iy = Math.Max(current.Y, ay);
        var ir = Math.Min(current.Right, ax + aw);
        var ib = Math.Min(current.Bottom, ay + ah);
        var iw = Math.Max(0, ir - ix);
        var ih = Math.Max(0, ib - iy);
        _scissorStack.Push(new ScreenRectangle(ix, iy, iw, ih));
    }

    public void PopScissor()
    {
        if (_scissorStack.Count > 1) _scissorStack.Pop();
    }

    //BeginRecording pushes a recording level; during Submit it writes into this cache, nesting child-level caches
    public void BeginRecording(List<GuiElementRenderState> cache)
        => _recordingStack.Push(cache);

    //EndRecording pops the top recording level; later Submits no longer write into that cache
    public void EndRecording()
        => _recordingStack.Pop();

    //ReplayRange re-submits the cached RenderState list into the current frame's GuiRenderState
    //Non-dirty widgets skip Render and replay the previous frame's recorded result
    //While recording it also writes to the top cache so the parent cache collects the child's replayed RenderStates
    public void ReplayRange(IReadOnlyList<GuiElementRenderState> cached)
    {
        var top = _recordingStack.Count > 0 ? _recordingStack.Peek() : null;
        foreach (var s in cached)
        {
            _renderState.AddGuiElement(s);
            top?.Add(s);
        }
    }

    //Submit submits a RenderState to GuiRenderState; while recording it writes to all active stack levels
    //When a child is dirty the top is the child cache and the bottom the window cache; both are written
    private void Submit(GuiElementRenderState state)
    {
        _renderState.AddGuiElement(state);
        foreach (var cache in _recordingStack) cache.Add(state);
    }

    //BlurBeforeThisStratum opens a new stratum and marks prior strata as the pre-blur segment
    //It does not call Submit or record to cache; GuiWindow forces blur frames to skip the cache so it is called every frame
    public void BlurBeforeThisStratum()
    {
        _renderState.NextStratum();
        _renderState.BlurBeforeThisStratum();
    }

    //AddPictureInPicture submits PIP state to GuiRenderState for GuiRenderer.Prepare to call renderer.Prepare
    //maps to vanilla addPicturesInPictureState, not recorded to cache; PIP is re-submitted every frame
    public void AddPictureInPicture(PictureInPictureRenderState pip)
        => _renderState.AddPictureInPicture(pip);

    //ToActual converts scaled pixels to actual and returns the top-left and bottom-right coordinate pair
    private (int Ax, int Ay, int Ax1, int Ay1) ToActual(int x, int y, int width, int height)
    {
        var ax = x * _guiScale;
        var ay = y * _guiScale;
        var ax1 = ax + width * _guiScale;
        var ay1 = ay + height * _guiScale;
        return (ax, ay, ax1, ay1);
    }

    //ToIntColor converts GuiColor float 0-1 to an ARGB int 0xAARRGGBB
    //StagedVertexBuffer extracts the RGBA byte order matching the UByte4Norm vertex format
    private static int ToIntColor(GuiColor c)
    {
        var r = (byte)Math.Clamp((int)(c.R * 255), 0, 255);
        var g = (byte)Math.Clamp((int)(c.G * 255), 0, 255);
        var b = (byte)Math.Clamp((int)(c.B * 255), 0, 255);
        var a = (byte)Math.Clamp((int)(c.A * 255), 0, 255);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }
}
