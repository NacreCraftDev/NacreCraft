using NacreCraft.Logging;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//EntityLookup the id/uuid index over tracked entities, maps to vanilla net.minecraft.world.level.entity.EntityLookup
//The non-generic EntityLookup in this project is the simplified spatial index; this generic form is the vanilla interface
public sealed class EntityLookup<T> where T : class, EntityAccess
{
    private readonly Dictionary<int, T> _byId = new();
    private readonly Dictionary<Guid, T> _byUuid = new();

    public void GetEntities<U>(EntityTypeTest<T, U> type, AbortableIterationConsumer<U> consumer) where U : T
    {
        foreach (var entity in _byId.Values)
        {
            var maybeEntity = type.TryCast(entity);
            if (maybeEntity is null || !consumer.Accept(maybeEntity).ShouldAbort()) continue;
            return;
        }
    }

    public IReadOnlyCollection<T> GetAllEntities() => _byId.Values;

    public void Add(T entity)
    {
        if (!_byUuid.TryAdd(entity.Uuid, entity))
        {
            Log.Warning($"Duplicate entity UUID {entity.Uuid}: {entity}");
            return;
        }
        _byId[entity.Id] = entity;
    }

    public void Remove(T entity)
    {
        _byUuid.Remove(entity.Uuid);
        _byId.Remove(entity.Id);
    }

    public T? GetEntity(int id) => _byId.GetValueOrDefault(id);

    public T? GetEntity(Guid id) => _byUuid.GetValueOrDefault(id);

    public int Count => _byUuid.Count;
}
