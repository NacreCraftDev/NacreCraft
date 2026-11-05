using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//LevelEntityGetterAdapter routes reads to the id index or the section storage, maps to vanilla net.minecraft.world.level.entity.LevelEntityGetterAdapter
public sealed class LevelEntityGetterAdapter<T> : LevelEntityGetter<T> where T : class, EntityAccess
{
    private readonly EntityLookup<T> _visibleEntities;
    private readonly EntitySectionStorage<T> _sectionStorage;

    public LevelEntityGetterAdapter(EntityLookup<T> visibleEntities, EntitySectionStorage<T> sectionStorage)
    {
        _visibleEntities = visibleEntities;
        _sectionStorage = sectionStorage;
    }

    public T? Get(int id) => _visibleEntities.GetEntity(id);

    public T? Get(Guid id) => _visibleEntities.GetEntity(id);

    public IEnumerable<T> GetAll() => _visibleEntities.GetAllEntities();

    public void Get<U>(EntityTypeTest<T, U> type, AbortableIterationConsumer<U> consumer) where U : T
        => _visibleEntities.GetEntities(type, consumer);

    public void Get(AABB box, Action<T> output)
        => _sectionStorage.GetEntities(box, AbortableIterationConsumers.ForConsumer(output));

    public void Get<U>(EntityTypeTest<T, U> type, AABB box, AbortableIterationConsumer<U> consumer) where U : T
        => _sectionStorage.GetEntities(type, box, consumer);
}
