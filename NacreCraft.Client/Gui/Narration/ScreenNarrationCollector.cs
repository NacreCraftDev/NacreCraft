using System.Text;

namespace NacreCraft.Client.Gui.Narration;

//ScreenNarrationCollector gathers one screen's narration and only speaks changed entries, maps to vanilla ScreenNarrationCollector
public sealed class ScreenNarrationCollector
{
    private int _generation;
    private readonly SortedDictionary<(NarratedElementType Type, int Depth), NarrationEntry> _entries = new();

    public void Update(Action<NarrationElementOutput> updater)
    {
        _generation++;
        updater(new Output(this, 0));
    }

    //CollectNarrationText joins entries changed since the last generation, force ignores the already narrated flag
    public string CollectNarrationText(bool force)
    {
        var result = new StringBuilder();
        var firstEntry = true;
        void Append(string text)
        {
            if (!firstEntry) result.Append(". ");
            firstEntry = false;
            result.Append(text);
        }
        foreach (var entry in _entries.Values)
        {
            if (entry.Generation != _generation || (!force && entry.AlreadyNarrated)) continue;
            entry.Contents.GetText(Append);
            entry.AlreadyNarrated = true;
        }
        return result.ToString();
    }

    private sealed class Output(ScreenNarrationCollector owner, int depth) : NarrationElementOutput
    {
        public void Add(NarratedElementType type, NarrationThunk contents)
        {
            if (!owner._entries.TryGetValue((type, depth), out var entry))
            {
                entry = new NarrationEntry();
                owner._entries[(type, depth)] = entry;
            }
            entry.Update(owner._generation, contents);
        }

        public NarrationElementOutput Nest() => new Output(owner, depth + 1);
    }

    private sealed class NarrationEntry
    {
        public NarrationThunk Contents { get; private set; } = NarrationThunk.Empty;
        public int Generation { get; private set; } = -1;
        public bool AlreadyNarrated { get; set; }

        public NarrationEntry Update(int generation, NarrationThunk contents)
        {
            if (!Contents.Equals(contents))
            {
                Contents = contents;
                AlreadyNarrated = false;
            }
            else if (Generation + 1 != generation)
            {
                AlreadyNarrated = false;
            }
            Generation = generation;
            return this;
        }
    }
}
