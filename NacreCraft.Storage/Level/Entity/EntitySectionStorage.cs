using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//EntitySectionStorage all sections of one level, maps to vanilla net.minecraft.world.level.entity.EntitySectionStorage
//Section keys are sorted so a chunk column can be addressed with one range query, the same trick the vanilla LongAVLTreeSet uses
public sealed class EntitySectionStorage<T> where T : class, EntityAccess
{
    public const int ChonkyEntitySearchGrace = 2;
    public const int MaxNonChonkyEntitySize = 4;

    private readonly Type _entityClass;
    private readonly Func<long, Visibility> _initialSectionVisibility;
    private readonly Dictionary<long, EntitySection<T>> _sections = new();
    private readonly SortedSet<long> _sectionIds = new();

    public EntitySectionStorage(Type entityClass, Func<long, Visibility> initialSectionVisibility)
    {
        _entityClass = entityClass;
        _initialSectionVisibility = initialSectionVisibility;
    }

    //ForEachAccessibleNonEmptySection visits sections intersecting the box, maps to vanilla forEachAccessibleNonEmptySection
    public void ForEachAccessibleNonEmptySection(AABB bb, AbortableIterationConsumer<EntitySection<T>> output)
    {
        var xMin = SectionPos.PosToSectionCoord(bb.Min.X - ChonkyEntitySearchGrace);
        var yMin = SectionPos.PosToSectionCoord(bb.Min.Y - MaxNonChonkyEntitySize);
        var zMin = SectionPos.PosToSectionCoord(bb.Min.Z - ChonkyEntitySearchGrace);
        var xMax = SectionPos.PosToSectionCoord(bb.Max.X + ChonkyEntitySearchGrace);
        var yMax = SectionPos.PosToSectionCoord(bb.Max.Y);
        var zMax = SectionPos.PosToSectionCoord(bb.Max.Z + ChonkyEntitySearchGrace);
        for (var x = xMin; x <= xMax; x++)
        {
            var lowestAbsoluteSectionKey = SectionPos.AsLong(x, 0, 0);
            var highestAbsoluteSectionKey = SectionPos.AsLong(x, -1, -1);
            foreach (var sectionKey in _sectionIds.GetViewBetween(lowestAbsoluteSectionKey, highestAbsoluteSectionKey + 1L))
            {
                var y = SectionPos.GetY(sectionKey);
                var z = SectionPos.GetZ(sectionKey);
                if (y < yMin || y > yMax || z < zMin || z > zMax) continue;
                if (!_sections.TryGetValue(sectionKey, out var section) || section.IsEmpty || !section.GetStatus().IsAccessible()) continue;
                if (output.Accept(section).ShouldAbort()) return;
            }
        }
    }

    //GetExistingSectionPositionsInChunk the section keys of one chunk, maps to vanilla getExistingSectionPositionsInChunk
    public IEnumerable<long> GetExistingSectionPositionsInChunk(long chunkKey)
    {
        var x = ChunkPos.GetX(chunkKey);
        var z = ChunkPos.GetZ(chunkKey);
        var lowest = SectionPos.AsLong(x, 0, z);
        var highest = SectionPos.AsLong(x, -1, z);
        return _sectionIds.GetViewBetween(lowest, highest + 1L);
    }

    //GetExistingSectionsInChunk the sections of one chunk, maps to vanilla getExistingSectionsInChunk
    public IEnumerable<EntitySection<T>> GetExistingSectionsInChunk(long chunkKey)
    {
        foreach (var sectionKey in GetExistingSectionPositionsInChunk(chunkKey))
            if (_sections.TryGetValue(sectionKey, out var section))
                yield return section;
    }

    private static long GetChunkKeyFromSectionKey(long sectionPos)
        => ChunkPos.Pack(SectionPos.GetX(sectionPos), SectionPos.GetZ(sectionPos));

    //GetOrCreateSection the section at the key, creating it with the chunk's initial visibility, maps to vanilla getOrCreateSection
    public EntitySection<T> GetOrCreateSection(long key)
    {
        if (_sections.TryGetValue(key, out var existing)) return existing;
        var chunkStatus = _initialSectionVisibility(GetChunkKeyFromSectionKey(key));
        var section = new EntitySection<T>(_entityClass, chunkStatus);
        _sections[key] = section;
        _sectionIds.Add(key);
        return section;
    }

    public EntitySection<T>? GetSection(long key) => _sections.GetValueOrDefault(key);

    //GetAllChunksWithExistingSections the chunk keys that own at least one section, maps to vanilla getAllChunksWithExistingSections
    public HashSet<long> GetAllChunksWithExistingSections()
    {
        var chunks = new HashSet<long>();
        foreach (var sectionKey in _sections.Keys) chunks.Add(GetChunkKeyFromSectionKey(sectionKey));
        return chunks;
    }

    public void GetEntities(AABB bb, AbortableIterationConsumer<T> output)
        => ForEachAccessibleNonEmptySection(bb, new SectionVisitor<T>(bb, output));

    public void GetEntities<U>(EntityTypeTest<T, U> type, AABB bb, AbortableIterationConsumer<U> consumer) where U : T
        => ForEachAccessibleNonEmptySection(bb, new TypedSectionVisitor<T, U>(type, bb, consumer));

    public void Remove(long sectionKey)
    {
        _sections.Remove(sectionKey);
        _sectionIds.Remove(sectionKey);
    }

    public int Count => _sectionIds.Count;

    //SectionVisitor forwards the box query to every visited section, maps to the vanilla lambda
    private sealed class SectionVisitor<TSection>(AABB bb, AbortableIterationConsumer<TSection> output)
        : AbortableIterationConsumer<EntitySection<TSection>> where TSection : class, EntityAccess
    {
        public IterationContinuation Accept(EntitySection<TSection> section) => section.GetEntities(bb, output);
    }

    private sealed class TypedSectionVisitor<TSection, TTyped>(EntityTypeTest<TSection, TTyped> type, AABB bb,
        AbortableIterationConsumer<TTyped> consumer) : AbortableIterationConsumer<EntitySection<TSection>>
        where TSection : class, EntityAccess where TTyped : TSection
    {
        public IterationContinuation Accept(EntitySection<TSection> section) => section.GetEntities(type, bb, consumer);
    }
}
