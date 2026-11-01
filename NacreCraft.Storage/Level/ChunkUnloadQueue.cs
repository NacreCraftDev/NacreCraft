using NacreCraft.Primitives;

namespace NacreCraft.Storage;

//ChunkUnloadQueue holds the chunks whose ticket level left the load range, bucketed into 32x32 chunk sections,
//maps to Paper's ChunkUnloadQueue. A player only moves the edge of the view, so the tick walks the few buckets that
//hold candidates instead of every holder the world has.
public sealed class ChunkUnloadQueue
{
    //SectionShift sections are 32x32 chunks, the same region size Paper locks ticket updates with
    private const int SectionShift = 5;

    private readonly Dictionary<long, HashSet<long>> _sections = new();

    //Count total candidates, for the tick budget
    public int Count { get; private set; }

    //Add files a chunk for unload, returns whether it was not already queued
    public bool Add(long packedPos)
    {
        var sectionKey = SectionKey(ChunkPos.Unpack(packedPos));
        if (!_sections.TryGetValue(sectionKey, out var chunks))
        {
            chunks = new HashSet<long>();
            _sections[sectionKey] = chunks;
        }
        if (!chunks.Add(packedPos)) return false;
        Count++;
        return true;
    }

    //Remove drops a chunk from the queue, used when its ticket comes back or its holder goes away by another path
    public bool Remove(long packedPos)
    {
        var sectionKey = SectionKey(ChunkPos.Unpack(packedPos));
        if (!_sections.TryGetValue(sectionKey, out var chunks) || !chunks.Remove(packedPos)) return false;
        Count--;
        if (chunks.Count == 0) _sections.Remove(sectionKey);
        return true;
    }

    //Snapshot copies the candidates out, so the caller can unload while the queue keeps changing
    //The order groups by section, which is the order Paper drains its buckets in
    public List<long> Snapshot()
    {
        var result = new List<long>(Count);
        foreach (var chunks in _sections.Values) result.AddRange(chunks);
        return result;
    }

    private static long SectionKey(ChunkPos pos)
        => ((long)(pos.X >> SectionShift) << 32) | (uint)(pos.Z >> SectionShift);
}
