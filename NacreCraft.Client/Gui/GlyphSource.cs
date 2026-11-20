using NacreCraft.Client.Gui.Font;
using NacreCraft.Util.Random;

namespace NacreCraft.Client.Gui;

//GlyphSource supplies baked glyphs for a font, maps to vanilla net.minecraft.client.gui.GlyphSource
public interface GlyphSource
{
    BakedGlyph GetGlyph(int codepoint);

    BakedGlyph GetRandomGlyph(RandomSource random, int width);
}
