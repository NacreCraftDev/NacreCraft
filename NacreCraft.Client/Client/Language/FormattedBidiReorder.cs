using System.Globalization;
using NacreCraft.Network.Chat;

namespace NacreCraft.Client.Language;

//FormattedBidiReorder turns logical-order formatted text into visual order, maps to vanilla net.minecraft.client.resources.language.FormattedBidiReorder
//Vanilla delegates to ICU Bidi and ArabicShaping; NacreCraft has no ICU dependency, so this port resolves strong, weak and neutral
//runs itself and mirrors paired punctuation, which covers the ordinary mixed Arabic/Hebrew cases
//Arabic joining forms are left to the font: Shape is the identity here, which is also what vanilla does when ICU shaping throws
public static class FormattedBidiReorder
{
    public static FormattedCharSequence Reorder(FormattedText text, bool defaultRightToLeft)
    {
        var source = SubStringSource.Create(text, Mirror, Shape);
        var parts = new List<FormattedCharSequence>();
        foreach (var run in BuildVisualRuns(source.PlainText, defaultRightToLeft))
            parts.AddRange(source.Substring(run.Start, run.Length, run.IsOddRun));
        return FormattedCharSequences.Composite(parts);
    }

    //Shape applies Arabic shaping in vanilla, not available without ICU
    private static string Shape(string text) => text;

    //Mirror swaps paired punctuation, maps to ICU UCharacter.getMirror for the common pairs
    private static int Mirror(int codepoint) => codepoint switch
    {
        '(' => ')', ')' => '(',
        '[' => ']', ']' => '[',
        '{' => '}', '}' => '{',
        '<' => '>', '>' => '<',
        '\u00AB' => '\u00BB', '\u00BB' => '\u00AB',
        '\u2039' => '\u203A', '\u203A' => '\u2039',
        0x2045 => 0x2046, 0x2046 => 0x2045,
        0x207D => 0x207E, 0x207E => 0x207D,
        0x208D => 0x208E, 0x208E => 0x208D,
        0x2329 => 0x232A, 0x232A => 0x2329,
        0x2768 => 0x2769, 0x2769 => 0x2768,
        0x276A => 0x276B, 0x276B => 0x276A,
        0x276C => 0x276D, 0x276D => 0x276C,
        0x276E => 0x276F, 0x276F => 0x276E,
        0x2770 => 0x2771, 0x2771 => 0x2770,
        0x2772 => 0x2773, 0x2773 => 0x2772,
        0x2774 => 0x2775, 0x2775 => 0x2774,
        0x3008 => 0x3009, 0x3009 => 0x3008,
        0x300A => 0x300B, 0x300B => 0x300A,
        0x300C => 0x300D, 0x300D => 0x300C,
        0x300E => 0x300F, 0x300F => 0x300E,
        0x3010 => 0x3011, 0x3011 => 0x3010,
        0x3014 => 0x3015, 0x3015 => 0x3014,
        0x3016 => 0x3017, 0x3017 => 0x3016,
        0x3018 => 0x3019, 0x3019 => 0x3018,
        0x301A => 0x301B, 0x301B => 0x301A,
        _ => codepoint
    };

    private readonly record struct BidiItem(int Start, int Length, BidiClass Class, int Level);

    private readonly record struct VisualRun(int Start, int Length, bool IsOddRun);

    private enum BidiClass { LeftToRight, RightToLeft, EuropeanNumber, ArabicNumber, Neutral }

    //BuildVisualRuns splits the text into runs in visual order; odd runs are emitted backwards by SubStringSource
    private static List<VisualRun> BuildVisualRuns(string text, bool defaultRightToLeft)
    {
        var items = new List<BidiItem>(text.Length);
        for (var i = 0; i < text.Length;)
        {
            var length = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
            var codepoint = length == 2 ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];
            items.Add(new BidiItem(i, length, Classify(codepoint), 0));
            i += length;
        }
        if (items.Count == 0) return new List<VisualRun>();

        var baseLevel = defaultRightToLeft ? 1 : 0;
        ResolveNeutrals(items, baseLevel);

        var levels = new int[items.Count];
        var maxLevel = 0;
        var lowestOdd = int.MaxValue;
        for (var i = 0; i < items.Count; i++)
        {
            var level = items[i].Class switch
            {
                BidiClass.LeftToRight => baseLevel == 0 ? 0 : 2,
                BidiClass.RightToLeft => 1,
                BidiClass.EuropeanNumber or BidiClass.ArabicNumber => 2,
                _ => baseLevel
            };
            levels[i] = level;
            if (level > maxLevel) maxLevel = level;
            if ((level & 1) == 1 && level < lowestOdd) lowestOdd = level;
        }

        var order = new int[items.Count];
        for (var i = 0; i < order.Length; i++) order[i] = i;
        if (lowestOdd != int.MaxValue)
        {
            //UAX#9 reordering: reverse every slice at or above each level, from the deepest level back to the lowest odd one
            for (var level = maxLevel; level >= lowestOdd; level--)
            {
                for (var i = 0; i < order.Length;)
                {
                    if (levels[order[i]] < level)
                    {
                        i++;
                        continue;
                    }
                    var j = i;
                    while (j < order.Length && levels[order[j]] >= level) j++;
                    Array.Reverse(order, i, j - i);
                    i = j;
                }
            }
        }

        var runs = new List<VisualRun>();
        for (var i = 0; i < order.Length;)
        {
            var level = levels[order[i]];
            var start = order[i];
            var end = start;
            var j = i + 1;
            while (j < order.Length && levels[order[j]] == level)
            {
                var next = order[j];
                //Inside a reversed run the logical indices descend, inside a forward run they ascend
                if ((level & 1) == 0 ? next != end + 1 : next != start - 1) break;
                start = Math.Min(start, next);
                end = Math.Max(end, next);
                j++;
            }
            runs.Add(new VisualRun(start, end - start + 1, (level & 1) == 1));
            i = j;
        }
        return runs;
    }

    //ResolveNeutrals gives every neutral run the direction of its neighbours, falling back to the paragraph direction
    private static void ResolveNeutrals(List<BidiItem> items, int baseLevel)
    {
        var baseClass = baseLevel == 0 ? BidiClass.LeftToRight : BidiClass.RightToLeft;
        var i = 0;
        while (i < items.Count)
        {
            if (items[i].Class != BidiClass.Neutral)
            {
                i++;
                continue;
            }
            var start = i;
            while (i < items.Count && items[i].Class == BidiClass.Neutral) i++;
            var previous = start > 0 ? StrongDirection(items[start - 1].Class) : baseClass;
            var next = i < items.Count ? StrongDirection(items[i].Class) : baseClass;
            var resolved = previous == next ? previous : baseClass;
            for (var j = start; j < i; j++)
                items[j] = items[j] with { Class = resolved };
        }
    }

    //StrongDirection folds numbers into the right-to-left side for neutral resolution, matching UAX#9 N1
    private static BidiClass StrongDirection(BidiClass cls) => cls switch
    {
        BidiClass.RightToLeft or BidiClass.EuropeanNumber or BidiClass.ArabicNumber => BidiClass.RightToLeft,
        BidiClass.LeftToRight => BidiClass.LeftToRight,
        _ => BidiClass.Neutral
    };

    private static BidiClass Classify(int codepoint)
    {
        if (IsRightToLeft(codepoint)) return BidiClass.RightToLeft;
        if (codepoint is >= '0' and <= '9') return BidiClass.EuropeanNumber;
        if (codepoint is >= 0x0660 and <= 0x0669) return BidiClass.ArabicNumber;
        if (codepoint is >= 0x06F0 and <= 0x06F9) return BidiClass.EuropeanNumber;

        var category = CharUnicodeInfo.GetUnicodeCategory(codepoint);
        return category switch
        {
            UnicodeCategory.SpaceSeparator or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator
                or UnicodeCategory.DashPunctuation or UnicodeCategory.OpenPunctuation or UnicodeCategory.ClosePunctuation
                or UnicodeCategory.InitialQuotePunctuation or UnicodeCategory.FinalQuotePunctuation
                or UnicodeCategory.OtherPunctuation or UnicodeCategory.ConnectorPunctuation
                or UnicodeCategory.MathSymbol or UnicodeCategory.CurrencySymbol or UnicodeCategory.ModifierSymbol
                or UnicodeCategory.OtherSymbol
                or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark
                or UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.OtherNotAssigned
                => BidiClass.Neutral,
            _ => BidiClass.LeftToRight
        };
    }

    //IsRightToLeft the right-to-left script blocks, kept to the ranges the game actually renders
    private static bool IsRightToLeft(int codepoint) => codepoint is
        >= 0x0590 and <= 0x05FF or
        >= 0x0600 and <= 0x06FF or
        >= 0x0700 and <= 0x074F or
        >= 0x0750 and <= 0x077F or
        >= 0x0780 and <= 0x07BF or
        >= 0x07C0 and <= 0x07FF or
        >= 0x0800 and <= 0x08FF or
        >= 0xFB1D and <= 0xFB4F or
        >= 0xFB50 and <= 0xFDFF or
        >= 0xFE70 and <= 0xFEFF or
        >= 0x1E800 and <= 0x1EFFF;
}
