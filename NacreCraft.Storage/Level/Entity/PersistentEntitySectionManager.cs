using NacreCraft.Logging;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Storage;

//PersistentEntitySectionManager the saved entity section manager, maps to vanilla net.minecraft.world.level.entity.PersistentEntitySectionManager
//Chunk visibility drives tracking/ticking; chunk load status drives when a chunk's entities may be read or written
public sealed class PersistentEntitySectionManager<T> : IDisposable where T : class, EntityAccess
{
    private readonly HashSet<Guid> _knownUuids = new();
    private readonly LevelCallback<T> _callbacks;
    private readonly EntityPersistentStorage<T> _permanentStorage;
    private readonly EntityLookup<T> _visibleEntityStorage = new();
    private readonly EntitySectionStorage<T> _sectionStorage;
    private readonly LevelEntityGetter<T> _entityGetter;
    private readonly Dictionary<long, Visibility> _chunkVisibility = new();
    private readonly Dictionary<long, ChunkLoadStatus> _chunkLoadStatuses = new();
    private readonly HashSet<long> _chunksToUnload = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<ChunkEntities<T>> _loadingInbox = new();

    public PersistentEntitySectionManager(Type entityClass, LevelCallback<T> callbacks, EntityPersistentStorage<T> permanentStorage)
    {
        _sectionStorage = new EntitySectionStorage<T>(entityClass,
            chunkKey => _chunkVisibility.GetValueOrDefault(chunkKey, Visibility.HIDDEN));
        _callbacks = callbacks;
        _permanentStorage = permanentStorage;
        _entityGetter = new LevelEntityGetterAdapter<T>(_visibleEntityStorage, _sectionStorage);
    }

    private void RemoveSectionIfEmpty(long sectionPos, EntitySection<T> section)
    {
        if (section.IsEmpty) _sectionStorage.Remove(sectionPos);
    }

    private bool AddEntityUuid(T entity)
    {
        if (_knownUuids.Add(entity.Uuid)) return true;
        Log.Warning($"UUID of added entity already exists: {entity}");
        return false;
    }

    public bool AddNewEntity(T entity) => AddEntity(entity, false);

    private bool AddEntity(T entity, bool loaded)
    {
        if (!AddEntityUuid(entity)) return false;
        var sectionKey = SectionPos.Of(entity.BlockPosition()).AsLong();
        var entitySection = _sectionStorage.GetOrCreateSection(sectionKey);
        entitySection.Add(entity);
        entity.SetLevelCallback(new Callback(this, entity, sectionKey, entitySection));
        if (!loaded) _callbacks.OnCreated(entity);
        var status = GetEffectiveStatus(entity, entitySection.GetStatus());
        if (status.IsAccessible()) StartTracking(entity);
        if (status.IsTicking()) StartTicking(entity);
        return true;
    }

    private static Visibility GetEffectiveStatus(T entity, Visibility status)
        => entity.IsAlwaysTicking ? Visibility.TICKING : status;

    public bool IsTicking(ChunkPos pos) => _chunkVisibility.GetValueOrDefault(pos.Pack(), Visibility.HIDDEN).IsTicking();

    public void AddLegacyChunkEntities(IEnumerable<T> entities)
    {
        foreach (var entity in entities) AddEntity(entity, true);
    }

    public void AddWorldGenChunkEntities(IEnumerable<T> entities)
    {
        foreach (var entity in entities) AddEntity(entity, false);
    }

    private void StartTicking(T entity) => _callbacks.OnTickingStart(entity);

    private void StopTicking(T entity) => _callbacks.OnTickingEnd(entity);

    private void StartTracking(T entity)
    {
        _visibleEntityStorage.Add(entity);
        _callbacks.OnTrackingStart(entity);
    }

    private void StopTracking(T entity)
    {
        _callbacks.OnTrackingEnd(entity);
        _visibleEntityStorage.Remove(entity);
    }

    public void UpdateChunkStatus(ChunkPos pos, FullChunkStatus fullChunkStatus)
        => UpdateChunkStatus(pos, Visibilities.FromFullChunkStatus(fullChunkStatus));

    public void UpdateChunkStatus(ChunkPos pos, Visibility chunkStatus)
    {
        var chunkPosKey = pos.Pack();
        if (chunkStatus == Visibility.HIDDEN)
        {
            _chunkVisibility.Remove(chunkPosKey);
            _chunksToUnload.Add(chunkPosKey);
        }
        else
        {
            _chunkVisibility[chunkPosKey] = chunkStatus;
            _chunksToUnload.Remove(chunkPosKey);
            EnsureChunkQueuedForLoad(chunkPosKey);
        }
        foreach (var section in _sectionStorage.GetExistingSectionsInChunk(chunkPosKey))
        {
            var previousStatus = section.UpdateChunkStatus(chunkStatus);
            var wasAccessible = previousStatus.IsAccessible();
            var isAccessible = chunkStatus.IsAccessible();
            var wasTicking = previousStatus.IsTicking();
            var isTicking = chunkStatus.IsTicking();
            if (wasTicking && !isTicking)
                foreach (var entity in section.GetEntities())
                    if (!entity.IsAlwaysTicking) StopTicking(entity);
            if (wasAccessible && !isAccessible)
            {
                foreach (var entity in section.GetEntities())
                    if (!entity.IsAlwaysTicking) StopTracking(entity);
            }
            else if (!wasAccessible && isAccessible)
            {
                foreach (var entity in section.GetEntities())
                    if (!entity.IsAlwaysTicking) StartTracking(entity);
            }
            if (!wasTicking && isTicking)
                foreach (var entity in section.GetEntities())
                    if (!entity.IsAlwaysTicking) StartTicking(entity);
        }
    }

    private void EnsureChunkQueuedForLoad(long chunkPos)
    {
        if (_chunkLoadStatuses.GetValueOrDefault(chunkPos, ChunkLoadStatus.FRESH) == ChunkLoadStatus.FRESH)
            RequestChunkLoad(chunkPos);
    }

    private bool StoreChunkSections(long chunkPos, Action<T> savedEntityVisitor)
    {
        var chunkLoadStatus = _chunkLoadStatuses.GetValueOrDefault(chunkPos, ChunkLoadStatus.FRESH);
        if (chunkLoadStatus == ChunkLoadStatus.PENDING) return false;
        var rootEntitiesToSave = _sectionStorage.GetExistingSectionsInChunk(chunkPos)
            .SelectMany(section => section.GetEntities())
            .Where(entity => entity.ShouldBeSaved)
            .ToList();
        if (rootEntitiesToSave.Count == 0)
        {
            if (chunkLoadStatus == ChunkLoadStatus.LOADED)
                _permanentStorage.StoreEntities(new ChunkEntities<T>(ChunkPos.Unpack(chunkPos), new List<T>()));
            return true;
        }
        if (chunkLoadStatus == ChunkLoadStatus.FRESH)
        {
            RequestChunkLoad(chunkPos);
            return false;
        }
        _permanentStorage.StoreEntities(new ChunkEntities<T>(ChunkPos.Unpack(chunkPos), rootEntitiesToSave));
        foreach (var entity in rootEntitiesToSave) savedEntityVisitor(entity);
        return true;
    }

    private void RequestChunkLoad(long chunkKey)
    {
        _chunkLoadStatuses[chunkKey] = ChunkLoadStatus.PENDING;
        var pos = ChunkPos.Unpack(chunkKey);
        _permanentStorage.LoadEntities(pos).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Log.Error($"Failed to read chunk {pos}: {task.Exception}");
                return;
            }
            _loadingInbox.Enqueue(task.Result);
        }, TaskScheduler.Default);
    }

    private bool ProcessChunkUnload(long chunkKey)
    {
        var storeSuccessful = StoreChunkSections(chunkKey, entity =>
        {
            foreach (var passenger in entity.GetPassengersAndSelf()) UnloadEntity(passenger);
        });
        if (!storeSuccessful) return false;
        _chunkLoadStatuses.Remove(chunkKey);
        return true;
    }

    private static void UnloadEntity(EntityAccess entity)
    {
        entity.SetRemoved(RemovalReason.UNLOADED_TO_CHUNK);
        entity.SetLevelCallback(EntityInLevelCallbacks.NULL);
    }

    private void ProcessUnloads()
    {
        List<long>? finished = null;
        foreach (var chunkKey in _chunksToUnload)
        {
            if (_chunkVisibility.GetValueOrDefault(chunkKey, Visibility.HIDDEN) != Visibility.HIDDEN)
            {
                (finished ??= new List<long>()).Add(chunkKey);
                continue;
            }
            if (ProcessChunkUnload(chunkKey)) (finished ??= new List<long>()).Add(chunkKey);
        }
        if (finished is null) return;
        foreach (var chunkKey in finished) _chunksToUnload.Remove(chunkKey);
    }

    public void ProcessPendingLoads()
    {
        while (_loadingInbox.TryDequeue(out var loadedChunk))
        {
            foreach (var entity in loadedChunk.GetEntities()) AddEntity(entity, true);
            _chunkLoadStatuses[loadedChunk.GetPos().Pack()] = ChunkLoadStatus.LOADED;
        }
    }

    public void Tick()
    {
        ProcessPendingLoads();
        ProcessUnloads();
    }

    private HashSet<long> GetAllChunksToSave()
    {
        var result = _sectionStorage.GetAllChunksWithExistingSections();
        foreach (var (chunkKey, status) in _chunkLoadStatuses)
            if (status == ChunkLoadStatus.LOADED) result.Add(chunkKey);
        return result;
    }

    public void AutoSave()
    {
        foreach (var chunkKey in GetAllChunksToSave())
        {
            if (_chunkVisibility.GetValueOrDefault(chunkKey, Visibility.HIDDEN) == Visibility.HIDDEN) ProcessChunkUnload(chunkKey);
            else StoreChunkSections(chunkKey, _ => { });
        }
    }

    public void SaveAll()
    {
        var chunksToSave = GetAllChunksToSave();
        while (chunksToSave.Count > 0)
        {
            _permanentStorage.Flush(false).GetAwaiter().GetResult();
            ProcessPendingLoads();
            List<long>? finished = null;
            foreach (var chunkKey in chunksToSave)
            {
                var shouldUnload = _chunkVisibility.GetValueOrDefault(chunkKey, Visibility.HIDDEN) == Visibility.HIDDEN;
                var done = shouldUnload ? ProcessChunkUnload(chunkKey) : StoreChunkSections(chunkKey, _ => { });
                if (done) (finished ??= new List<long>()).Add(chunkKey);
            }
            if (finished is null) break;
            foreach (var chunkKey in finished) chunksToSave.Remove(chunkKey);
        }
        _permanentStorage.Flush(true).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        SaveAll();
        _permanentStorage.Dispose();
    }

    public bool IsLoaded(Guid uuid) => _knownUuids.Contains(uuid);

    public LevelEntityGetter<T> GetEntityGetter() => _entityGetter;

    public bool CanPositionTick(BlockPos pos) => CanPositionTick(new ChunkPos(pos.X >> 4, pos.Z >> 4));

    public bool CanPositionTick(ChunkPos pos) => _chunkVisibility.GetValueOrDefault(pos.Pack(), Visibility.HIDDEN).IsTicking();

    public bool AreEntitiesLoaded(long chunkKey) => _chunkLoadStatuses.GetValueOrDefault(chunkKey, ChunkLoadStatus.FRESH) == ChunkLoadStatus.LOADED;

    public string GatherStats()
        => $"{_knownUuids.Count},{_visibleEntityStorage.Count},{_sectionStorage.Count},{_chunkLoadStatuses.Count},{_chunkVisibility.Count},{_loadingInbox.Count},{_chunksToUnload.Count}";

    public int Count() => _visibleEntityStorage.Count;

    private enum ChunkLoadStatus
    {
        FRESH,
        PENDING,
        LOADED
    }

    private sealed class Callback(PersistentEntitySectionManager<T> manager, T entity, long currentSectionKey, EntitySection<T> currentSection)
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
            UpdateStatus(previousStatus, newSection.GetStatus());
        }

        private void UpdateStatus(Visibility previousStatus, Visibility newStatus)
        {
            var effectivePreviousStatus = GetEffectiveStatus(entity, previousStatus);
            var effectiveNewStatus = GetEffectiveStatus(entity, newStatus);
            if (effectivePreviousStatus == effectiveNewStatus)
            {
                if (effectiveNewStatus.IsAccessible()) manager._callbacks.OnSectionChange(entity);
                return;
            }
            var wasAccessible = effectivePreviousStatus.IsAccessible();
            var isAccessible = effectiveNewStatus.IsAccessible();
            if (wasAccessible && !isAccessible) manager.StopTracking(entity);
            else if (!wasAccessible && isAccessible) manager.StartTracking(entity);
            var wasTicking = effectivePreviousStatus.IsTicking();
            var isTicking = effectiveNewStatus.IsTicking();
            if (wasTicking && !isTicking) manager.StopTicking(entity);
            else if (!wasTicking && isTicking) manager.StartTicking(entity);
            if (isAccessible) manager._callbacks.OnSectionChange(entity);
        }

        public void OnRemove(RemovalReason reason)
        {
            if (!currentSection.Remove(entity))
                Log.Warning($"Entity {entity} wasn't found in section {SectionPos.Of(currentSectionKey)} (destroying due to {reason})");
            var status = GetEffectiveStatus(entity, currentSection.GetStatus());
            if (status.IsTicking()) manager.StopTicking(entity);
            if (status.IsAccessible()) manager.StopTracking(entity);
            if (reason.ShouldDestroy()) manager._callbacks.OnDestroyed(entity);
            manager._knownUuids.Remove(entity.Uuid);
            entity.SetLevelCallback(EntityInLevelCallbacks.NULL);
            manager.RemoveSectionIfEmpty(currentSectionKey, currentSection);
        }
    }
}


