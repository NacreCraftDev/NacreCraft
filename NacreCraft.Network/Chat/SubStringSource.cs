using System.Text;
using NacreCraft.Codec;

namespace NacreCraft.Network.Chat;

//SubStringSource splits a formatted text into styled sub-strings, maps to vanilla net.minecraft.network.chat.SubStringSource
//Vanilla walks per-codepoint formatting through StringDecomposer and supports a reverse character modifier; this port records
//one style per character of each visited run, which is enough because NC styles are per run
public sealed class SubStringSource
{
    private readonly string _plainText;
    private readonly List<Style> _charStyles;
    private readonly Func<int, int> _reverseCharModifier;

    private SubStringSource(string plainText, List<Style> charStyles, Func<int, int> reverseCharModifier)
    {
        _plainText = plainText;
        _charStyles = charStyles;
        _reverseCharModifier = reverseCharModifier;
    }

    public string PlainText => _plainText;

    //Substring splits the given character range into runs of equal style, maps to vanilla substring
    public IReadOnlyList<FormattedCharSequence> Substring(int start, int length, bool reverse)
    {
        var parts = new List<FormattedCharSequence>();
        if (length == 0) return parts;
        var currentRunStyle = _charStyles[start];
        var currentRunStart = start;
        for (var i = 1; i < length; i++)
        {
            var actualIndex = start + i;
            var charStyle = _charStyles[actualIndex];
            if (charStyle.Equals(currentRunStyle)) continue;
            parts.Add(Run(_plainText[currentRunStart..actualIndex], currentRunStyle, reverse));
            currentRunStyle = charStyle;
            currentRunStart = actualIndex;
        }
        if (currentRunStart < start + length)
            parts.Add(Run(_plainText[currentRunStart..(start + length)], currentRunStyle, reverse));
        if (reverse) parts.Reverse();
        return parts;
    }

    public static SubStringSource Create(FormattedText text) => Create(text, c => c, s => s);

    public static SubStringSource Create(FormattedText text, Func<string, string> shaper)
        => Create(text, c => c, shaper);

    //Create records one style per character of every visited run, maps to vanilla create
    //reverseCharModifier is applied while walking a reversed run, used for bidi mirroring
    public static SubStringSource Create(FormattedText text, Func<int, int> reverseCharModifier, Func<string, string> shaper)
    {
        var plainText = new StringBuilder();
        var charStyles = new List<Style>();
        text.Visit((style, contents) =>
        {
            plainText.Append(contents);
            for (var i = 0; i < contents.Length; i++)
                charStyles.Add(style);
            return Optional<object>.Empty();
        }, Style.Empty);
        return new SubStringSource(shaper(plainText.ToString()), charStyles, reverseCharModifier);
    }

    private FormattedCharSequence Run(string text, Style style, bool reverse)
        => reverse
            ? FormattedCharSequences.Backward(text, style, _reverseCharModifier)
            : FormattedCharSequences.Forward(text, style);
}
