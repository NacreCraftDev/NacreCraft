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

//SpaceGlyphProvider maps to vanilla SpaceProvider
//Spaces have only advance, no bitmap; the advances Map<int,float> converts to an EmptyGlyph dictionary
//The advances field of a "space" provider in font/*.json defines the width of each space character
//e.g. U+0020 regular space advance=4.0, U+00A0 no-break space advance=4.0
public sealed class SpaceGlyphProvider : IGlyphProvider
{
    private readonly Dictionary<int, EmptyGlyph> _glyphs;

    public SpaceGlyphProvider(IReadOnlyDictionary<int, float> advances)
    {
        _glyphs = new Dictionary<int, EmptyGlyph>(advances.Count);
        foreach (var (codepoint, advance) in advances)
            _glyphs[codepoint] = new EmptyGlyph(advance);
    }

    public IReadOnlySet<int> GetSupportedGlyphs() => _supported ??= new HashSet<int>(_glyphs.Keys);

    private HashSet<int>? _supported;

    public IUnbakedGlyph? GetGlyph(int codepoint)
    {
        return _glyphs.TryGetValue(codepoint, out var glyph) ? glyph : null;
    }

    public void Dispose() { }
}
