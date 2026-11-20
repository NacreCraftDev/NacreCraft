using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Logging;
using NacreCraft.Registry;
using NacreCraft.Resources;

namespace NacreCraft.Client.Gui.Font;

//FontManager owns the loaded font sets and creates GlyphFonts, maps to vanilla net.minecraft.client.gui.font.FontManager
//NC loads a font set lazily from its font/<name>.json definition; the missing set always supplies AllMissingGlyphProvider
public sealed class FontManager : IDisposable
{
    public static readonly Identifier MissingFont = Identifier.WithDefaultNamespace("missing");
    public static readonly Identifier DefaultFont = Identifier.WithDefaultNamespace("default");

    private readonly ResourceManager _resources;
    private readonly ResourceFontAccessor _accessor;
    private readonly GlyphStitcher _stitcher;
    private readonly Dictionary<Identifier, FontSet> _fontSets = new();
    private readonly FontSet _missingFontSet;
    private IReadOnlySet<FontOption> _options = new HashSet<FontOption>();
    private bool _disposed;

    public FontManager(ResourceManager resources, GlyphStitcher stitcher)
    {
        _resources = resources;
        _stitcher = stitcher;
        _accessor = new ResourceFontAccessor(resources);
        _missingFontSet = new FontSet();
        _missingFontSet.Reload(
            new[] { new IGlyphProvider.Conditional(new AllMissingGlyphProvider(), FontOptionFilter.AlwaysPass) },
            new HashSet<FontOption>());
    }

    //Reload drops every loaded set and re-applies the active font options; missing/default are recreated on demand
    public void Reload(IReadOnlySet<FontOption> options)
    {
        _options = options;
        foreach (var set in _fontSets.Values) set.Dispose();
        _fontSets.Clear();
    }

    //GetFontSet returns the named set, loading it on first use and falling back to the missing set
    public FontSet GetFontSet(Identifier id)
    {
        if (id == MissingFont) return _missingFontSet;
        if (_fontSets.TryGetValue(id, out var set)) return set;

        var resourceId = $"{id.Namespace}:font/{id.Path}.json";
        var stream = _accessor.OpenResource(resourceId);
        if (stream is null)
        {
            _fontSets[id] = _missingFontSet;
            return _missingFontSet;
        }

        List<IGlyphProvider.Conditional> providers;
        using (stream)
            providers = FontProviderDefinitionLoader.Load(stream, _accessor);

        set = new FontSet();
        set.Reload(providers, _options);
        _fontSets[id] = set;
        Log.Debug($"font {id} providers={providers.Count} active={set.ActiveProviders.Count}");
        return set;
    }

    //CreateFont builds a GlyphFont over the named set, maps to vanilla createFont
    public GlyphFont CreateFont(Identifier id) => new(GetFontSet(id), _stitcher, 7, 9);

    //CreateFontFilterFishy uses the same set; the fishy-glyph distinction is not modelled yet
    public GlyphFont CreateFontFilterFishy(Identifier id) => CreateFont(id);

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var set in _fontSets.Values)
            if (!ReferenceEquals(set, _missingFontSet)) set.Dispose();
        _fontSets.Clear();
        _missingFontSet.Dispose();
        _disposed = true;
    }
}
