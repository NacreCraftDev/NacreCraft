namespace NacreCraft.Storage;

//SectionTracker tracks section-level distances from players, maps to vanilla net.minecraft.server.level.SectionTracker
//Vanilla walks a section-level graph like the chunk trackers; NC only needs the stored level per packed section
public sealed class SectionTracker
{
    public const int SectionsPerChunk = 16;
    public const int SectionMask = SectionsPerChunk - 1;

    private readonly Dictionary<long, int> _levels = new();

    //GetLevel the stored level of a packed section position, int.MaxValue when unknown
    public int GetLevel(long sectionPos) => _levels.TryGetValue(sectionPos, out var level) ? level : int.MaxValue;

    public void SetLevel(long sectionPos, int level) => _levels[sectionPos] = level;

    public void Clear() => _levels.Clear();

    public int Count => _levels.Count;
}
