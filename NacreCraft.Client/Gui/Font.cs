using System.Collections.Concurrent;
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

namespace NacreCraft.Client.Gui;

//GlyphFont font rendering entry point, maps to vanilla Font
//Holds the FontSet provider chain + GlyphStitcher baker + BakedGlyph cache
//Draw iterates text codepoints and calls FontSet.GetGlyph → Bake → Render
//maps to vanilla Font.drawInBatch; the shadow color is computed as RGB*0.25 keeping alpha
//The GPU layer avoids the Style domain object and wraps render params in GlyphRenderOptions; bold/italic come from the caller
//F7 simplifies Draw to plain text + shadow; bold/italic are left to the Game layer's rich-text wrapper
//Named GlyphFont to avoid clashing with the NacreCraft.Gpu.Font namespace, a C# language limitation
public sealed class GlyphFont
{
    private readonly FontSet _fontSet;
    //F9 changed to an interface type so tests can inject a mock stitcher; GlyphFont only uses Bake/GetMissing and does not depend on the concrete GlyphStitcher
    private readonly IUnbakedGlyph.Stitcher _stitcher;
    private readonly ConcurrentDictionary<int, BakedGlyph?> _bakedCache = new();
    private readonly int _ascent;
    private readonly int _lineHeight;

    public int Ascent => _ascent;
    public int LineHeight => _lineHeight;

    public GlyphFont(FontSet fontSet, IUnbakedGlyph.Stitcher stitcher, int ascent, int lineHeight)
    {
        _fontSet = fontSet;
        _stitcher = stitcher;
        _ascent = ascent;
        _lineHeight = lineHeight;
    }

    //Draw renders text; x/y are the top of the text in GUI units, matching the glyph offsets from IGlyphBitmap
    //The pen advances by each glyph's advance in the same unit; the context scales the finished quads to device pixels
    //When shadow=true, a non-zero ShadowColor makes BakedGlyph.Render draw a shadow
    //color is an ARGB int, maps to vanilla GlyphInstance.color
    public void Draw(IGuiRenderContext context, string text, float x, float y, int color, bool shadow)
    {
        if (string.IsNullOrEmpty(text)) return;
        float penX = x;
        //The glyph bitmap offsets are already relative to the top of the line, so no ascent is added here; vanilla
        //does the same by passing the glyph instance y straight into BakedSheetGlyph.renderChar
        float penY = y;
        int shadowColor = shadow ? ComputeShadowColor(color) : 0;
        foreach (var ch in text)
        {
            int codepoint = ch;
            var baked = GetBaked(codepoint);
            if (baked != null)
            {
                //Bold/shadow offsets come from the glyph metrics, maps to vanilla glyphInfo.getBoldOffset/getShadowOffset;
                //unihex glyphs use 0.5 while bitmap glyphs keep the default 1.0
                var options = new GlyphRenderOptions(penX, penY, color, shadowColor, false, false,
                    baked.Info.BoldOffset, baked.Info.ShadowOffset);
                baked.Render(context, in options);
                penX += baked.Info.Advance;
            }
            else
            {
                //Missing glyphs use GetMissing as a placeholder, maps to vanilla AllMissingGlyphProvider
                var missing = _stitcher.GetMissing();
                var options = new GlyphRenderOptions(penX, penY, color, shadowColor, false, false,
                    missing.Info.BoldOffset, missing.Info.ShadowOffset);
                missing.Render(context, in options);
                penX += missing.Info.Advance;
            }
        }
    }

    //MeasureText measures the text pixel width for widgets to compute alignment offsets
    public float MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        float width = 0;
        foreach (var ch in text)
        {
            var baked = GetBaked(ch);
            width += baked?.Info.Advance ?? 0;
        }
        return width;
    }

    //DrawGlyph renders a single glyph and returns the advance for the rich-text iterator to accumulate penX
    //maps to the per-glyph rendering of vanilla Font.drawInBatch, called by the Game layer's FormattedTextRenderer while iterating a FormattedCharSequence
    //When a codepoint is missing it calls GetMissing as a placeholder, SpecialGlyphs.Missing purple square
    //options are built by the caller from Style and contain x/y/color/bold/italic; the GPU layer does not depend on Style
    public float DrawGlyph(IGuiRenderContext context, int codepoint, in GlyphRenderOptions options)
    {
        var baked = GetBaked(codepoint) ?? _stitcher.GetMissing();
        baked.Render(context, in options);
        return baked.Info.Advance;
    }

    //GetBaked codepoint → BakedGlyph lazy bake cache, maps to the vanilla Font.getOrCreate glyph cache
    private BakedGlyph? GetBaked(int codepoint)
    {
        return _bakedCache.GetOrAdd(codepoint, cp =>
        {
            var unbaked = _fontSet.GetGlyph(cp);
            if (unbaked == null) return null;
            return unbaked.Bake(_stitcher);
        });
    }

    //ComputeShadowColor shadow color, maps to the shadowColor computation of vanilla Font.drawShadow
    //Vanilla multiplies the RGB of color by 0.25 keeping alpha
    private static int ComputeShadowColor(int color)
    {
        int a = (color >> 24) & 0xFF;
        int r = (int)(((color >> 16) & 0xFF) * 0.25f);
        int g = (int)(((color >> 8) & 0xFF) * 0.25f);
        int b = (int)((color & 0xFF) * 0.25f);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }
}
