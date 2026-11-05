using NacreCraft.Logging;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Storage;

//TransientEntitySectionManager the non-persistent entity section manager, maps to vanilla net.minecraft.world.level.entity.TransientEntitySectionManager
//Used by client levels and by levels that never save entities; there is no storage side here
public sealed class TransientEntitySectionManager<T> where T : class, EntityAccess
{
    private readonly LevelCallback<T> _callbacks;
    private readonly EntityLookup<T> _entityStorage = new();
    private readonly EntitySectionStorage<T> _sectionStorage;
    private readonly HashSet<long> _tickingChunks = new();
    private readonly LevelEntityGetter<T> _entityGetter;

    public TransientEntitySectionManager(Type entityClass, LevelCallback<T> callbacks)
    {
        _sectionStorage = new EntitySectionStorage<T>(entityClass,
            chunkKey => _tickingChunks.Contains(chunkKey) ? Visibility.TICKING : Visibility.TRACKED);
        _callbacks = callbacks;
        _entityGetter = new LevelEntityGetterAdapter<T>(_entityStorage, _sectionStorage);
    }

    public void StartTicking(ChunkPos pos)
    {
        var chunkKey = pos.Pack();
        _tickingChunks.Add(chunkKey);
        foreach (var section in _sectionStorage.GetExistingSectionsInChunk(chunkKey))
        {
            var previousStatus = section.UpdateChunkStatus(Visibility.TICKING);
            if (!previousStatus.IsTicking())
                foreach (var entity in section.GetEntities())
                    if (!entity.IsAlwaysTicking) _callbacks.OnTickingStart(entity);
        }
    }

    public void StopTicking(ChunkPos pos)
    {
        var chunkKey = pos.Pack();
        _tickingChunks.Remove(chunkKey);
        foreach (var section in _sectionStorage.GetExistingSectionsInChunk(chunkKey))
        {
            var previousStatus = section.UpdateChunkStatus(Visibility.TRACKED);
            if (previousStatus.IsTicking())
                foreach (var entity in section.GetEntities())
                    if (!entity.IsAlwaysTicking) _callbacks.OnTickingEnd(entity);
        }
    }

    public LevelEntityGetter<T> GetEntityGetter() => _entityGetter;

    public void AddEntity(T entity)
    {
        _entityStorage.Add(entity);
        var sectionKey = SectionPos.Of(entity.BlockPosition()).AsLong();
        var entitySection = _sectionStorage.GetOrCreateSection(sectionKey);
        entitySection.Add(entity);
        entity.SetLevelCallback(new Callback(this, entity, sectionKey, entitySection));
        _callbacks.OnCreated(entity);
        _callbacks.OnTrackingStart(entity);
        if (entity.IsAlwaysTicking || entitySection.GetStatus().IsTicking()) _callbacks.OnTickingStart(entity);
    }

    public int Count() => _entityStorage.Count;

    public string GatherStats() => $"{_entityStorage.Count},{_sectionStorage.Count},{_tickingChunks.Count}";

    private void RemoveSectionIfEmpty(long sectionPos, EntitySection<T> section)
    {
        if (section.IsEmpty) _sectionStorage.Remove(sectionPos);
    }

    private sealed class Callback(TransientEntitySectionManager<T> manager, T entity, long currentSectionKey, EntitySection<T> currentSection)
        : EntityInLevelCallback
    {
        public void OnMove()
        {
            var pos = entity.BlockPosition();
            var newSectionPos = SectionPos.Of(pos).AsLong();
            if (newSectionPos == currentSectionKey) return;
            var previousStatus = currentSection.GetStatus();
            if (!currentSection.Remove(entity))
                Log.Warning($"Entity {entity} wasn't found in section {SectionPos.Of(currentSectionKey)} (moving to {newSectionPos})");
            manager.RemoveSectionIfEmpty(currentSectionKey, currentSection);
            var newSection = manager._sectionStorage.GetOrCreateSection(newSectionPos);
            newSection.Add(entity);
            currentSection = newSection;
            currentSectionKey = newSectionPos;
            manager._callbacks.OnSectionChange(entity);
            if (entity.IsAlwaysTicking) return;
            var wasTicking = previousStatus.IsTicking();
            var isTicking = newSection.GetStatus().IsTicking();
            if (wasTicking && !isTicking) manager._callbacks.OnTickingEnd(entity);
            else if (!wasTicking && isTicking) manager._callbacks.OnTickingStart(entity);
        }

        public void OnRemove(RemovalReason reason)
        {
            if (!currentSection.Remove(entity))
                Log.Warning($"Entity {entity} wasn't found in section {SectionPos.Of(currentSectionKey)} (destroying due to {reason})");
            var status = currentSection.GetStatus();
            if (status.IsTicking() || entity.IsAlwaysTicking) manager._callbacks.OnTickingEnd(entity);
            manager._callbacks.OnTrackingEnd(entity);
            manager._callbacks.OnDestroyed(entity);
            manager._entityStorage.Remove(entity);
            entity.SetLevelCallback(EntityInLevelCallbacks.NULL);
            manager.RemoveSectionIfEmpty(currentSectionKey, currentSection);
        }
    }
}
