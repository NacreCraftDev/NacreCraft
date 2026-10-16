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
using NacreCraft.Client.Blaze3d;
using NacreCraft.Logging;
namespace NacreCraft.Client.Gui.Font;

//GlyphStitcher glyph stitcher, maps to vanilla GlyphStitcher
//Manages multiple FontTexture atlases, creating new ones on demand when the first does not fit
//Stitch stitches IGlyphInfo+IGlyphBitmap into the atlas and returns a BakedGlyph
//colored glyphs use RGBA8 atlases, grayscale glyphs use R8 atlases; the two kinds are allocated independently
//F7 wires into GuiResourceManager: when creating a FontTexture it registers the atlas to get a TextureSetup and creates GlyphRenderTypes based on colored
public sealed class GlyphStitcher : IUnbakedGlyph.Stitcher, IDisposable
{
    private readonly GpuDevice _device;
    private readonly GuiResourceManager _resourceManager;
    private readonly List<FontTexture> _textures = new();
    //Counts the traced stitches, keeping the diagnostic output bounded
    private static int s_stitchDiag;

    public GlyphStitcher(GpuDevice device, GuiResourceManager resourceManager)
    {
        _device = device;
        _resourceManager = resourceManager;
    }

    //Stitch walks the existing atlases for the first that fits, creating a new one if none do
    //maps to vanilla GlyphStitcher.stitch: walk textures, call add, create a new one on failure
    public BakedGlyph Stitch(IGlyphInfo info, IGlyphBitmap bitmap)
    {
        //Diagnostic: a glyph that never reaches the atlas build leaves no upload log downstream, so the entry is
        //traced as well
        if (s_stitchDiag < 6)
        {
            s_stitchDiag++;
            Log.Info($"glyph stitch enter thread={Environment.CurrentManagedThreadId} atlases={_textures.Count} color={bitmap.IsColored}");
        }
        foreach (var texture in _textures)
        {
            var glyph = texture.Add(info, bitmap);
            if (glyph != null) return glyph;
        }
        var newTexture = CreateTexture(bitmap.IsColored);
        _textures.Add(newTexture);
        return newTexture.Add(info, bitmap) ?? throw new InvalidOperationException(
            $"glyph {info.Advance} does not fit the new 256×256 atlas pixelSize={bitmap.PixelWidth}x{bitmap.PixelHeight}");
    }

    //CreateTexture creates a new atlas GpuTexture, registers it with GuiResourceManager to get a TextureSetup and creates GlyphRenderTypes
    //colored=true uses an RGBA8 atlas + CreateForColorTexture, colored=false uses an R8 atlas + CreateForGrayscaleTexture
    private FontTexture CreateTexture(bool colored)
    {
                var image = _device.CreateTexture(null, GpuTexture.UsageTextureBinding, colored ? GpuFormat.Rgba8Unorm : GpuFormat.R8Unorm, FontTexture.Size, FontTexture.Size, 1, 1);
        var textureSetup = _resourceManager.RegisterFontTexture(image);
        var renderTypes = colored
            ? GlyphRenderTypes.CreateForColorTexture()
            : GlyphRenderTypes.CreateForGrayscaleTexture();
        return new FontTexture(image, colored, textureSetup, renderTypes);
    }

    //GetMissing returns the missing-glyph placeholder, maps to vanilla AllMissingGlyphProvider returning SpecialGlyphs.MISSING
    //SpecialGlyphs.Missing is a 5x8 white border; Bake calls Stitch to stitch it into an RGBA8 atlas and returns a SheetBakedGlyph
    //At render time the caller's color tints it; the purple square is a tinting effect
    public BakedGlyph GetMissing() => SpecialGlyphs.Missing.Bake(this);

    //Reset releases all atlas textures, maps to vanilla GlyphStitcher.reset
    public void Reset()
    {
        foreach (var texture in _textures)
            texture.Dispose();
        _textures.Clear();
    }

    public void Dispose() => Reset();
}
