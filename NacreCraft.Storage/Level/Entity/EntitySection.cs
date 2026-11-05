using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//EntitySection the entities of one 16x16x16 section, maps to vanilla net.minecraft.world.level.entity.EntitySection
public sealed class EntitySection<T> where T : class, EntityAccess
{
    private readonly ClassInstanceMultiMap<T> _storage;
    private Visibility _chunkStatus;

    public EntitySection(Type entityClass, Visibility chunkStatus)
    {
        _chunkStatus = chunkStatus;
        _storage = new ClassInstanceMultiMap<T>(entityClass);
    }

    public void Add(T entity) => _storage.Add(entity);

    public bool Remove(T entity) => _storage.Remove(entity);

    //GetEntities visits the entities intersecting the box, maps to vanilla getEntities(AABB, consumer)
    public IterationContinuation GetEntities(AABB bb, AbortableIterationConsumer<T> consumer)
    {
        foreach (var entity in _storage)
        {
            if (!entity.GetBoundingBox().Intersects(bb) || !consumer.Accept(entity).ShouldAbort()) continue;
            return IterationContinuation.ABORT;
        }
        return IterationContinuation.CONTINUE;
    }

    //GetEntities visits the entities matching the type test, maps to vanilla getEntities(EntityTypeTest, AABB, consumer)
    public IterationContinuation GetEntities<U>(EntityTypeTest<T, U> type, AABB bb, AbortableIterationConsumer<U> consumer)
        where U : T
    {
        var found = _storage.Find(type.GetBaseClass());
        if (found.Count == 0) return IterationContinuation.CONTINUE;
        foreach (var entity in found)
        {
            var maybeEntity = type.TryCast(entity);
            if (maybeEntity is null || !entity.GetBoundingBox().Intersects(bb) || !consumer.Accept(maybeEntity).ShouldAbort()) continue;
            return IterationContinuation.ABORT;
        }
        return IterationContinuation.CONTINUE;
    }

    public bool IsEmpty => _storage.IsEmpty;

    public IEnumerable<T> GetEntities() => _storage;

    public Visibility GetStatus() => _chunkStatus;

    public Visibility UpdateChunkStatus(Visibility chunkStatus)
    {
        var previous = _chunkStatus;
        _chunkStatus = chunkStatus;
        return previous;
    }

    public int Size => _storage.Count;
}
