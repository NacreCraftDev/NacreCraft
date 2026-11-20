using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Rendertype;
using NacreCraft.Registry;
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

//DisplayMode glyph render mode, maps to vanilla Font.DisplayMode
//NORMAL plain text SEE_THROUGH see-through rendering POLYGON_OFFSET polygon offset used for shadows
public enum DisplayMode
{
    Normal,
    SeeThrough,
    PolygonOffset
}

//GlyphRenderTypes glyph render pipeline and render type set, maps to vanilla GlyphRenderTypes
//The GUI glyph path still binds RenderPipelines directly; the RenderType members are the world-text path used by
//TextRenderable/PlainTextRenderable and are created from the atlas identifier
public sealed class GlyphRenderTypes
{
    public RenderPipeline Normal { get; }
    public RenderPipeline SeeThrough { get; }
    public RenderPipeline PolygonOffset { get; }
    public RenderPipeline GuiPipeline { get; }

    public RenderType? NormalRenderType { get; }
    public RenderType? SeeThroughRenderType { get; }
    public RenderType? PolygonOffsetRenderType { get; }

    public GlyphRenderTypes(RenderPipeline normal, RenderPipeline seeThrough, RenderPipeline polygonOffset, RenderPipeline guiPipeline)
        : this(normal, seeThrough, polygonOffset, guiPipeline, null, null, null)
    {
    }

    public GlyphRenderTypes(RenderPipeline normal, RenderPipeline seeThrough, RenderPipeline polygonOffset, RenderPipeline guiPipeline,
        RenderType? normalRenderType, RenderType? seeThroughRenderType, RenderType? polygonOffsetRenderType)
    {
        Normal = normal;
        SeeThrough = seeThrough;
        PolygonOffset = polygonOffset;
        GuiPipeline = guiPipeline;
        NormalRenderType = normalRenderType;
        SeeThroughRenderType = seeThroughRenderType;
        PolygonOffsetRenderType = polygonOffsetRenderType;
    }

    //createForGrayscaleTexture grayscale atlases use the GUI_TEXT_GRAYSCALE pipeline series
    public static GlyphRenderTypes CreateForGrayscaleTexture()
        => new(RenderPipelines.GUI_TEXT_GRAYSCALE,
            RenderPipelines.GUI_TEXT_GRAYSCALE_SEE_THROUGH,
            RenderPipelines.GUI_TEXT_GRAYSCALE_POLYGON_OFFSET,
            RenderPipelines.GUI_TEXT_GRAYSCALE);

    //createForColorTexture color atlases use the GUI_TEXT pipeline series
    public static GlyphRenderTypes CreateForColorTexture()
        => new(RenderPipelines.GUI_TEXT,
            RenderPipelines.GUI_TEXT_SEE_THROUGH,
            RenderPipelines.GUI_TEXT_POLYGON_OFFSET,
            RenderPipelines.GUI_TEXT);

    //Identifier overloads add the world-text RenderTypes used by TextRenderable
    public static GlyphRenderTypes CreateForGrayscaleTexture(Identifier name)
        => new(RenderPipelines.GUI_TEXT_GRAYSCALE,
            RenderPipelines.GUI_TEXT_GRAYSCALE_SEE_THROUGH,
            RenderPipelines.GUI_TEXT_GRAYSCALE_POLYGON_OFFSET,
            RenderPipelines.GUI_TEXT_GRAYSCALE,
            RenderTypes.TextGrayscale(name),
            RenderTypes.TextGrayscaleSeeThrough(name),
            RenderTypes.TextGrayscalePolygonOffset(name));

    public static GlyphRenderTypes CreateForColorTexture(Identifier name)
        => new(RenderPipelines.GUI_TEXT,
            RenderPipelines.GUI_TEXT_SEE_THROUGH,
            RenderPipelines.GUI_TEXT_POLYGON_OFFSET,
            RenderPipelines.GUI_TEXT,
            RenderTypes.Text(name),
            RenderTypes.TextSeeThrough(name),
            RenderTypes.TextPolygonOffset(name));

    //Select returns the matching GUI pipeline for a DisplayMode
    public RenderPipeline Select(DisplayMode mode) => mode switch
    {
        DisplayMode.Normal => Normal,
        DisplayMode.SeeThrough => SeeThrough,
        DisplayMode.PolygonOffset => PolygonOffset,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    //SelectRenderType returns the matching world RenderType for a DisplayMode, maps to vanilla GlyphRenderTypes.select
    public RenderType SelectRenderType(DisplayMode mode)
        => mode switch
        {
            DisplayMode.Normal => NormalRenderType,
            DisplayMode.SeeThrough => SeeThroughRenderType,
            DisplayMode.PolygonOffset => PolygonOffsetRenderType,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        } ?? throw new InvalidOperationException($"GlyphRenderTypes has no RenderType for {mode}");
}
