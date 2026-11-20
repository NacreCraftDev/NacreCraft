using NacreCraft.Client.Gui.Font.Glyphs;

namespace NacreCraft.Client.Gui.Font;

//AllMissingGlyphProvider returns the missing-glyph placeholder for every codepoint, maps to vanilla net.minecraft.client.gui.font.AllMissingGlyphProvider
public sealed class AllMissingGlyphProvider : IGlyphProvider
{
    private static readonly IUnbakedGlyph MissingInstance = new MissingGlyph();

    public IReadOnlySet<int> GetSupportedGlyphs() => EmptyGlyphs;

    public IUnbakedGlyph? GetGlyph(int codepoint) => MissingInstance;

    public void Dispose()
    {
    }

    private static readonly IReadOnlySet<int> EmptyGlyphs = new HashSet<int>();

    private sealed class MissingGlyph : IUnbakedGlyph
    {
        public IGlyphInfo Info => SpecialGlyphs.Missing.Info;

        public BakedGlyph Bake(IUnbakedGlyph.Stitcher stitcher) => stitcher.GetMissing();
    }
}
