using NacreCraft.Codec;
using NacreCraft.DataFixer.Util;
using NacreCraft.Network.Chat;

namespace NacreCraft.Client;

//StringSplitter measures and wraps formatted text, maps to vanilla net.minecraft.client.StringSplitter
//Widths come from the injected WidthProvider, normally Font; the wrapping rules follow vanilla (last space wins, newline breaks, style carries across parts)
public sealed class StringSplitter
{
    private readonly WidthProvider _widthProvider;

    public StringSplitter(WidthProvider widthProvider)
    {
        _widthProvider = widthProvider;
    }

    public delegate float WidthProvider(int codepoint, Style style);

    public delegate void LinePosConsumer(Style style, int start, int end);

    public float StringWidth(string? str)
    {
        if (str is null) return 0.0f;
        var result = 0.0f;
        StringDecomposer.IterateFormatted(str, Style.Empty, (position, style, codepoint) =>
        {
            result += _widthProvider(codepoint, style);
            return true;
        });
        return result;
    }

    public float StringWidth(FormattedText text)
    {
        var result = 0.0f;
        text.Visit((style, contents) =>
        {
            StringDecomposer.IterateFormatted(contents, style, (position, s, codepoint) =>
            {
                result += _widthProvider(codepoint, s);
                return true;
            });
            return Optional<Unit>.Empty();
        }, Style.Empty);
        return result;
    }

    public float StringWidth(FormattedCharSequence text)
    {
        var result = 0.0f;
        text((position, style, codepoint) =>
        {
            result += _widthProvider(codepoint, style);
            return true;
        });
        return result;
    }

    public int PlainIndexAtWidth(string str, int maxWidth, Style style)
    {
        var output = new WidthLimitedCharSink(this, maxWidth);
        StringDecomposer.Iterate(str, style, output.Accept);
        return output.Position;
    }

    public string PlainHeadByWidth(string str, int maxWidth, Style style)
        => str[..PlainIndexAtWidth(str, maxWidth, style)];

    public string PlainTailByWidth(string str, int maxWidth, Style style)
    {
        var currentWidth = 0.0f;
        var result = str.Length;
        StringDecomposer.IterateBackwards(str, style, (position, s, codepoint) =>
        {
            currentWidth += _widthProvider(codepoint, s);
            if (currentWidth > maxWidth) return false;
            result = position;
            return true;
        });
        return str[result..];
    }

    public FormattedText HeadByWidth(FormattedText text, int width, Style initialStyle)
    {
        var output = new WidthLimitedCharSink(this, width);
        var collector = new ComponentCollector();
        return text.Visit((style, contents) =>
        {
            output.ResetPosition();
            if (!StringDecomposer.IterateFormatted(contents, style, output.Accept))
            {
                var partial = contents[..output.Position];
                if (partial.Length > 0)
                    collector.Append(FormattedText.Of(partial, style));
                return Optional<FormattedText>.Of(collector.GetResultOrEmpty());
            }

            if (contents.Length > 0)
                collector.Append(FormattedText.Of(contents, style));
            return Optional<FormattedText>.Empty();
        }, initialStyle).OrElse(text);
    }

    public int FindLineBreak(string input, int max, Style initialStyle)
    {
        var finder = new LineBreakFinder(this, max);
        StringDecomposer.IterateFormatted(input, initialStyle, finder.Accept);
        return finder.GetSplitPosition();
    }

    //GetWordPosition moves by words in a plain string, maps to vanilla getWordPosition
    public static int GetWordPosition(string text, int dir, int from, bool stripSpaces)
    {
        var result = from;
        var reverse = dir < 0;
        var count = Math.Abs(dir);
        for (var i = 0; i < count; i++)
        {
            if (reverse)
            {
                while (stripSpaces && result > 0 && (text[result - 1] == ' ' || text[result - 1] == '\n')) result--;
                while (result > 0 && text[result - 1] != ' ' && text[result - 1] != '\n') result--;
                continue;
            }

            var length = text.Length;
            var index1 = text.IndexOf(' ', result);
            var index2 = text.IndexOf('\n', result);
            result = index1 == -1 && index2 == -1 ? -1 : index1 != -1 && index2 != -1 ? Math.Min(index1, index2) : index1 != -1 ? index1 : index2;
            if (result == -1)
            {
                result = length;
                continue;
            }

            while (stripSpaces && result < length && (text[result] == ' ' || text[result] == '\n')) result++;
        }

        return result;
    }

    public void SplitLines(string input, int maxWidth, Style initialStyle, bool includeAll, LinePosConsumer output)
    {
        var start = 0;
        var size = input.Length;
        var workStyle = initialStyle;
        while (start < size)
        {
            var finder = new LineBreakFinder(this, maxWidth);
            var endOfText = StringDecomposer.IterateFormatted(input, start, workStyle, initialStyle, finder.Accept);
            if (endOfText)
            {
                output(workStyle, start, size);
                break;
            }

            var lineBreak = finder.GetSplitPosition();
            var firstTailChar = input[lineBreak];
            var adjustedBreak = firstTailChar == '\n' || firstTailChar == ' ' ? lineBreak + 1 : lineBreak;
            output(workStyle, start, includeAll ? adjustedBreak : lineBreak);
            start = adjustedBreak;
            workStyle = finder.GetSplitStyle();
        }
    }

    public List<FormattedText> SplitLines(string input, int maxWidth, Style initialStyle)
    {
        var result = new List<FormattedText>();
        SplitLines(input, maxWidth, initialStyle, false, (style, start, end) =>
            result.Add(FormattedText.Of(input[start..end], style)));
        return result;
    }

    public List<FormattedText> SplitLines(FormattedText input, int maxWidth, Style initialStyle)
    {
        var result = new List<FormattedText>();
        SplitLines(input, maxWidth, initialStyle, (text, wrapped) => result.Add(text));
        return result;
    }

    public void SplitLines(FormattedText input, int maxWidth, Style initialStyle, Action<FormattedText, bool> output)
    {
        var partList = new List<LineComponent>();
        input.Visit((style, contents) =>
        {
            if (contents.Length > 0) partList.Add(new LineComponent(contents, style));
            return Optional<Unit>.Empty();
        }, initialStyle);
        var parts = new FlatComponents(partList);
        var shouldRestart = true;
        var forceNewLine = false;
        var isWrapped = false;
        while (shouldRestart)
        {
            shouldRestart = false;
            var finder = new LineBreakFinder(this, maxWidth);
            foreach (var part in parts.Parts)
            {
                var endOfText = StringDecomposer.IterateFormatted(part.Contents, 0, part.Style, initialStyle, finder.Accept);
                if (!endOfText)
                {
                    var lineBreak = finder.GetSplitPosition();
                    var lineBreakStyle = finder.GetSplitStyle();
                    var firstTailChar = parts.CharAt(lineBreak);
                    var isNewLine = firstTailChar == '\n';
                    var skipNextChar = isNewLine || firstTailChar == ' ';
                    forceNewLine = isNewLine;
                    var result = parts.SplitAt(lineBreak, skipNextChar ? 1 : 0, lineBreakStyle);
                    output(result, isWrapped);
                    isWrapped = !isNewLine;
                    shouldRestart = true;
                    break;
                }

                finder.AddToOffset(part.Contents.Length);
            }
        }

        var lastLine = parts.GetRemainder();
        if (lastLine is not null) output(lastLine, isWrapped);
        else if (forceNewLine) output(FormattedText.EMPTY, false);
    }

    private static int CharCount(int codepoint) => codepoint >= 0x10000 ? 2 : 1;

    private sealed class WidthLimitedCharSink(StringSplitter owner, float maxWidth)
    {
        private float _maxWidth = maxWidth;
        public int Position { get; private set; }

        public bool Accept(int position, Style style, int codepoint)
        {
            var remaining = _maxWidth - owner._widthProvider(codepoint, style);
            _maxWidth = remaining;
            if (remaining >= 0.0f)
            {
                Position = position + CharCount(codepoint);
                return true;
            }

            return false;
        }

        public void ResetPosition() => Position = 0;
    }

    private sealed class LineBreakFinder(StringSplitter owner, float maxWidth)
    {
        private readonly float _maxWidth = Math.Max(maxWidth, 1.0f);
        private int _lineBreak = -1;
        private Style _lineBreakStyle = Style.Empty;
        private bool _hadNonZeroWidthChar;
        private float _width;
        private int _lastSpace = -1;
        private Style _lastSpaceStyle = Style.Empty;
        private int _nextChar;
        private int _offset;

        public bool Accept(int position, Style style, int codepoint)
        {
            var adjustedPosition = position + _offset;
            switch (codepoint)
            {
                case 10:
                    return FinishIteration(adjustedPosition, style);
                case 32:
                _lastSpace = adjustedPosition;
                _lastSpaceStyle = style;
                break;
            }

            var charWidth = owner._widthProvider(codepoint, style);
            _width += charWidth;
            if (_hadNonZeroWidthChar && _width > _maxWidth)
            {
                if (_lastSpace != -1) return FinishIteration(_lastSpace, _lastSpaceStyle);
                return FinishIteration(adjustedPosition, style);
            }

            _hadNonZeroWidthChar |= charWidth != 0.0f;
            _nextChar = adjustedPosition + CharCount(codepoint);
            return true;
        }

        private bool FinishIteration(int lineBreak, Style style)
        {
            _lineBreak = lineBreak;
            _lineBreakStyle = style;
            return false;
        }

        public int GetSplitPosition() => _lineBreak != -1 ? _lineBreak : _nextChar;

        public Style GetSplitStyle() => _lineBreakStyle;

        public void AddToOffset(int delta) => _offset += delta;
    }

    private sealed class FlatComponents
    {
        public List<LineComponent> Parts { get; }
        private string _flatParts;

        public FlatComponents(List<LineComponent> parts)
        {
            Parts = parts;
            _flatParts = string.Concat(parts.Select(p => p.Contents));
        }

        public char CharAt(int position) => _flatParts[position];

        public FormattedText SplitAt(int skipPosition, int skipSize, Style splitStyle)
        {
            var result = new ComponentCollector();
            var position = skipPosition;
            var inSkip = false;
            for (var i = 0; i < Parts.Count; i++)
            {
                var element = Parts[i];
                var contents = element.Contents;
                var contentsSize = contents.Length;
                if (!inSkip)
                {
                    if (position > contentsSize)
                    {
                        result.Append(element);
                        Parts.RemoveAt(i--);
                        position -= contentsSize;
                        continue;
                    }

                    var beforeSplit = contents[..position];
                    if (beforeSplit.Length > 0) result.Append(FormattedText.Of(beforeSplit, element.Style));
                    position += skipSize;
                    inSkip = true;
                }

                if (position > contentsSize)
                {
                    Parts.RemoveAt(i--);
                    position -= contentsSize;
                    continue;
                }

                var afterSplit = contents[position..];
                if (afterSplit.Length == 0)
                {
                    Parts.RemoveAt(i);
                    break;
                }

                Parts[i] = new LineComponent(afterSplit, splitStyle);
                break;
            }

            _flatParts = _flatParts[(skipPosition + skipSize)..];
            return result.GetResultOrEmpty();
        }

        public FormattedText? GetRemainder()
        {
            var result = new ComponentCollector();
            foreach (var part in Parts) result.Append(part);
            Parts.Clear();
            return result.GetResult();
        }
    }

    private sealed class LineComponent(string contents, Style style) : FormattedText
    {
        public string Contents { get; } = contents;
        public Style Style { get; } = style;

        public Optional<T> Visit<T>(FormattedText.ContentConsumer<T> output) => output(Contents);

        public Optional<T> Visit<T>(FormattedText.StyledContentConsumer<T> output, Style parentStyle)
            => output(Style.ApplyTo(parentStyle), Contents);
    }
}
