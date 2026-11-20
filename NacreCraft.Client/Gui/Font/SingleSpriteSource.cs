using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Util.Random;

namespace NacreCraft.Client.Gui.Font;

//SingleSpriteSource returns one baked glyph for every codepoint, maps to vanilla net.minecraft.client.gui.font.SingleSpriteSource
//AtlasGlyphProvider uses it for a sprite that represents a whole text run
public sealed record SingleSpriteSource(BakedGlyph Glyph) : GlyphSource
{
    public BakedGlyph GetGlyph(int codepoint) => Glyph;

    public BakedGlyph GetRandomGlyph(RandomSource random, int width) => Glyph;
}
