using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraftEntity = NacreCraft.Registry.Entity;

namespace NacreCraft.Storage;

//EntityLookup, entity section index, maps to vanilla net.minecraft.world.level.entity.EntityLookup
//Buckets entities by SectionPos, supporting AABB range queries and per-chunk queries
//Replaces linear scans over List<Entity>, improving entity query performance in large worlds
public sealed class EntityLookup
{
    //Bucketed by SectionPos.AsLong; each bucket holds the entity list for that section
    private readonly Dictionary<long, List<NacreCraftEntity>> _bySection = new();
    //Reverse index from entity to section, so Remove locates the bucket quickly without scanning
    private readonly Dictionary<NacreCraftEntity, long> _entityToSection = new(ReferenceEqualityComparer.Instance);

    public int Count => _entityToSection.Count;

    //Add adds an entity to its section bucket
    //When entity.Pos changes, Remove then Add to re-bucket
    public void Add(NacreCraftEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (_entityToSection.ContainsKey(entity)) return;
        var sectionKey = SectionPosOf(entity.Pos);
        if (!_bySection.TryGetValue(sectionKey, out var list))
        {
            list = new List<NacreCraftEntity>();
            _bySection[sectionKey] = list;
        }
        list.Add(entity);
        _entityToSection[entity] = sectionKey;
    }

    //Remove removes an entity, returns whether it succeeded
    public bool Remove(NacreCraftEntity entity)
    {
        if (!_entityToSection.TryGetValue(entity, out var sectionKey)) return false;
        if (_bySection.TryGetValue(sectionKey, out var list))
        {
            list.Remove(entity);
            if (list.Count == 0) _bySection.Remove(sectionKey);
        }
        _entityToSection.Remove(entity);
        return true;
    }

    //GetInRange returns all entities in the AABB range, maps to vanilla get(AABB)
    //min/max are the two AABB corners; iterate the covered section buckets and collect entities
    public IEnumerable<NacreCraftEntity> GetInRange(Vec3 min, Vec3 max)
    {
        var minX = SectionPos.BlockToSectionCoord(min.X);
        var minY = SectionPos.BlockToSectionCoord(min.Y);
        var minZ = SectionPos.BlockToSectionCoord(min.Z);
        var maxX = SectionPos.BlockToSectionCoord(max.X);
        var maxY = SectionPos.BlockToSectionCoord(max.Y);
        var maxZ = SectionPos.BlockToSectionCoord(max.Z);
        for (var x = minX; x <= maxX; x++)
            for (var y = minY; y <= maxY; y++)
                for (var z = minZ; z <= maxZ; z++)
                {
                    var key = SectionPos.AsLong(x, y, z);
                    if (!_bySection.TryGetValue(key, out var list)) continue;
                    foreach (var entity in list)
                        yield return entity;
                }
    }

    //GetInChunk returns all entities in the given chunk
    public IEnumerable<NacreCraftEntity> GetInChunk(ChunkPos pos)
    {
        foreach (var (key, list) in _bySection)
        {
            var sx = SectionPos.GetX(key);
            var sz = SectionPos.GetZ(key);
            if (sx == pos.X && sz == pos.Z)
                foreach (var entity in list)
                    yield return entity;
        }
    }

    //GetAll returns all entities, used by tick traversal
    public IEnumerable<NacreCraftEntity> GetAll() => _entityToSection.Keys;

    //Clear clears all entities
    public void Clear()
    {
        _bySection.Clear();
        _entityToSection.Clear();
    }

    //SectionPosOf computes the owning section's packed long from a Vec3
    private static long SectionPosOf(Vec3 pos)
        => SectionPos.AsLong(
            SectionPos.BlockToSectionCoord(pos.X),
            SectionPos.BlockToSectionCoord(pos.Y),
            SectionPos.BlockToSectionCoord(pos.Z));
}
