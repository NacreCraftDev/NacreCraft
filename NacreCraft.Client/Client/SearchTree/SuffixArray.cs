namespace NacreCraft.Client.SearchTree;

//SuffixArray a suffix array over every indexed string, used as a substring index
//maps to vanilla net.minecraft.client.searchtree.SuffixArray
//Strings are concatenated into one char array separated by the end-of-text marker, and the suffix order is built
//by doubling: sort by the first 2k characters until the whole string is covered
public sealed class SuffixArray<T>
{
    private const int EndOfTextMarker = -1;
    private const int EndOfData = -2;

    private readonly List<T> _list = new();
    private readonly List<int> _chars = new();
    private readonly List<int> _wordStarts = new();
    private List<int> _suffixToT = new();
    private List<int> _offsets = new();
    private int _maxStringLength;

    public void Add(T value, string text)
    {
        _maxStringLength = Math.Max(_maxStringLength, text.Length);
        var index = _list.Count;
        _list.Add(value);
        _wordStarts.Add(_chars.Count);
        for (var i = 0; i < text.Length; i++)
        {
            _suffixToT.Add(index);
            _offsets.Add(i);
            _chars.Add(text[i]);
        }
        _suffixToT.Add(index);
        _offsets.Add(text.Length);
        _chars.Add(EndOfTextMarker);
    }

    public void Generate()
    {
        var charCount = _chars.Count;
        var positions = new int[charCount];
        var lefts = new int[charCount];
        var rights = new int[charCount];
        var order = new int[charCount];
        for (var i = 0; i < charCount; i++)
        {
            positions[i] = _chars[i];
            order[i] = i;
        }

        var count = 1;
        var max = Math.Min(charCount, _maxStringLength);
        while (count * 2 < max)
        {
            for (var i = 0; i < charCount; i++)
            {
                lefts[i] = positions[i];
                rights[i] = i + count < charCount ? positions[i + count] : EndOfData;
                order[i] = i;
            }
            Array.Sort(order, (a, b) =>
            {
                if (lefts[a] != lefts[b]) return lefts[a].CompareTo(lefts[b]);
                return rights[a].CompareTo(rights[b]);
            });
            for (var i = 0; i < charCount; i++)
            {
                var current = order[i];
                var previous = order[i - 1];
                positions[current] = i > 0 && lefts[current] == lefts[previous] && rights[current] == rights[previous]
                    ? positions[previous]
                    : i;
            }
            count *= 2;
        }

        var oldSuffixToT = _suffixToT;
        var oldOffsets = _offsets;
        _suffixToT = new List<int>(oldSuffixToT.Count);
        _offsets = new List<int>(oldOffsets.Count);
        for (var i = 0; i < charCount; i++)
        {
            var index = order[i];
            _suffixToT.Add(oldSuffixToT[index]);
            _offsets.Add(oldOffsets[index]);
        }
    }

    //Compare the search text against the suffix at index, maps to vanilla compare
    private int Compare(string text, int index)
    {
        var start = _wordStarts[_suffixToT[index]];
        var offset = _offsets[index];
        for (var i = 0; i < text.Length; i++)
        {
            var p = _chars[start + offset + i];
            if (p == EndOfTextMarker) return 1;
            var c = text[i];
            if (c < p) return -1;
            if (c > p) return 1;
        }
        return 0;
    }

    public IReadOnlyList<T> Search(string text)
    {
        var suffixCount = _suffixToT.Count;
        var low = 0;
        var high = suffixCount;
        while (low < high)
        {
            var mid = low + (high - low) / 2;
            if (Compare(text, mid) > 0) low = mid + 1;
            else high = mid;
        }
        if (low < 0 || low >= suffixCount) return Array.Empty<T>();
        var lowerBound = low;

        high = suffixCount;
        while (low < high)
        {
            var mid = low + (high - low) / 2;
            if (Compare(text, mid) >= 0) low = mid + 1;
            else high = mid;
        }
        var upperBound = low;

        var matches = new HashSet<int>();
        for (var i = lowerBound; i < upperBound; i++) matches.Add(_suffixToT[i]);
        var indices = matches.ToArray();
        Array.Sort(indices);
        var result = new List<T>(indices.Length);
        foreach (var t in indices) result.Add(_list[t]);
        return result;
    }
}
