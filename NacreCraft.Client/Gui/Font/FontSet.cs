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

namespace NacreCraft.Client.Gui.Font;

//FontSet maps to vanilla FontSet, a single font set
//Holds the provider chain and looks up codepoints in order, returning the first hit as IUnbakedGlyph
//Vanilla FontSet.getGlyph returns BakedGlyph holding a wrappedStitcher for lazy Bake
//The F3 stage does not wire the Stitcher bakery yet, the caller handles it; refactored once F6 injects the Stitcher
public sealed class FontSet : IDisposable
{
    private List<IGlyphProvider.Conditional> _allProviders = new();
    private List<IGlyphProvider> _activeProviders = new();
    private readonly ConcurrentDictionary<int, IUnbakedGlyph?> _cache = new();

    public FontSet() { }

    //Reload receives the full providers list and the currently active FontOption set
    //maps to vanilla reload(providers, options)
    public void Reload(IEnumerable<IGlyphProvider.Conditional> providers, IReadOnlySet<FontOption> options)
    {
        _allProviders = providers.ToList();
        Reload(options);
    }

    //Reload only re-selects active providers by options
    //maps to vanilla reload(options)
    public void Reload(IReadOnlySet<FontOption> options)
    {
        _activeProviders = _allProviders
            .Where(c => c.Filter.Apply(options))
            .Select(c => c.Provider)
            .ToList();
        _cache.Clear();
    }

    //ActiveProviders exposes the active provider list for F4/F6 to build the glyphsByWidth index
    public IReadOnlyList<IGlyphProvider> ActiveProviders => _activeProviders;

    //GetGlyph looks up the first matching codepoint in provider order
    //maps to a simplified vanilla computeGlyphInfo, dropping the fishy advance check and the nonFishy double cache
    public IUnbakedGlyph? GetGlyph(int codepoint)
    {
        return _cache.GetOrAdd(codepoint, ComputeGlyph);
    }

    private IUnbakedGlyph? ComputeGlyph(int codepoint)
    {
        foreach (var provider in _activeProviders)
        {
            var glyph = provider.GetGlyph(codepoint);
            if (glyph != null)
                return glyph;
        }
        return null;
    }

    public void Dispose()
    {
        foreach (var p in _allProviders)
            p.Provider.Dispose();
        _allProviders.Clear();
        _activeProviders.Clear();
        _cache.Clear();
    }
}
