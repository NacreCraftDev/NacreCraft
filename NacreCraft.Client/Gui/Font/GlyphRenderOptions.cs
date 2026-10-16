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
namespace NacreCraft.Client.Gui.Font;

//GlyphRenderOptions glyph render params, maps to vanilla BakedSheetGlyph$GlyphInstance
//Wraps x/y/color/shadowColor/bold/italic/boldOffset/shadowOffset
//Replaces the vanilla Style domain object; the GPU layer avoids rich-text semantics
//HasShadow is determined by ShadowColor!=0, maps to vanilla hasShadow=shadowColor()!=0
public readonly record struct GlyphRenderOptions(
    float X,
    float Y,
    int Color,
    int ShadowColor,
    bool Bold,
    bool Italic,
    float BoldOffset,
    float ShadowOffset)
{
    //HasShadow a non-zero shadow color means a shadow must be drawn, maps to vanilla GlyphInstance.hasShadow
    public bool HasShadow => ShadowColor != 0;

    //Simple creates plain glyph params with no shadow or styling
    public static GlyphRenderOptions Simple(float x, float y, int color)
        => new(x, y, color, 0, false, false, 1.0f, 1.0f);

    //WithShadow creates glyph params with a shadow; shadowColor is the shadow color and shadowOffset the offset
    public GlyphRenderOptions WithShadow(int shadowColor, float shadowOffset)
        => this with { ShadowColor = shadowColor, ShadowOffset = shadowOffset };
}
