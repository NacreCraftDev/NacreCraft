namespace NacreCraft.Registry;

//EntityDataAccessor typed metadata key, maps to vanilla net.minecraft.network.syncher.EntityDataAccessor
//SerializerId is the Game layer EntityDataSerializers registry index; the Registry layer only passes it through without interpreting it
public readonly record struct EntityDataAccessor<T>(byte Index, int SerializerId);

//SynchedValue a single entity metadata entry snapshot, maps to vanilla SynchedEntityData.DataValue
public readonly record struct SynchedValue(byte Index, int SerializerId, object Value);

//SynchedEntityData entity metadata container, maps to vanilla net.minecraft.network.syncher.SynchedEntityData
//Subclasses Define entries at construction, Set changes values and marks the entry dirty
//Observers read the dirty set once per tick, then the tracker clears it after every observer has sent
public sealed class SynchedEntityData
{
    //Entry stores the initial value alongside the current one so a newly paired observer can receive the non-default set
    private sealed class Entry
    {
        public required byte Index;
        public required int SerializerId;
        public required object InitialValue;
        public object Value = null!;
        public bool Dirty;
    }

    //Stored ordered by index; the send order matches vanilla's ascending index order
    private readonly SortedDictionary<byte, Entry> _entries = new();

    //_dirty is set by any Set change and cleared by ClearDirty after a tracking round
    private bool _dirty;

    //Version change counter; kept for diagnostics and for callers that poll it
    public int Version { get; private set; }

    //IsDirty whether any entry changed since the last ClearDirty
    public bool IsDirty => _dirty;

    //Define declares a typed entry, maps to vanilla define
    public void Define<T>(EntityDataAccessor<T> accessor, T value)
        => Define(accessor.Index, accessor.SerializerId, value!);

    //Define declares an entry by raw index and serializer id
    public void Define(byte index, int serializerId, object value)
    {
        if (_entries.ContainsKey(index))
            throw new InvalidOperationException($"Entity metadata {index} already defined");
        _entries[index] = new Entry
        {
            Index = index,
            SerializerId = serializerId,
            InitialValue = value,
            Value = value,
        };
    }

    //Get returns the typed entry value; an undeclared index means a wrong index, so it throws instead of silently returning a wrong value
    public T Get<T>(EntityDataAccessor<T> accessor) => (T)Get(accessor.Index);

    //Get returns the entry value by raw index
    public object Get(byte index)
        => _entries.TryGetValue(index, out var entry)
            ? entry.Value
            : throw new InvalidOperationException($"Entity metadata {index} not defined");

    //Set changes a typed entry; no change means no dirty mark, matching vanilla set marking dirty only on difference
    public void Set<T>(EntityDataAccessor<T> accessor, T value, bool forceDirty = false)
        => Set(accessor.Index, value!, forceDirty);

    //Set changes an entry by raw index
    public void Set(byte index, object value) => Set(index, value, false);

    //Set changes an entry by raw index, forceDirty keeps the vanilla escape hatch that resends an unchanged value
    public void Set(byte index, object value, bool forceDirty)
    {
        if (!_entries.TryGetValue(index, out var entry))
            throw new InvalidOperationException($"Entity metadata {index} not defined");
        if (!forceDirty && Equals(entry.Value, value)) return;
        entry.Value = value;
        entry.Dirty = true;
        _dirty = true;
        Version++;
    }

    //CollectAll returns every entry in ascending index order, used when a new observer pairs with the entity
    public List<SynchedValue> CollectAll()
    {
        var result = new List<SynchedValue>(_entries.Count);
        foreach (var entry in _entries.Values)
            result.Add(new SynchedValue(entry.Index, entry.SerializerId, entry.Value));
        return result;
    }

    //GetDirtyValues returns the entries changed since the last ClearDirty without clearing them
    //The tracker reads this for every observer in a tick and clears once at the end, so the first observer does not eat the change
    public List<SynchedValue>? GetDirtyValues()
    {
        if (!_dirty) return null;
        var result = new List<SynchedValue>();
        foreach (var entry in _entries.Values)
            if (entry.Dirty)
                result.Add(new SynchedValue(entry.Index, entry.SerializerId, entry.Value));
        return result;
    }

    //GetNonDefaultValues returns entries whose value differs from the initial one, maps to vanilla getNonDefaultValues
    public List<SynchedValue>? GetNonDefaultValues()
    {
        List<SynchedValue>? result = null;
        foreach (var entry in _entries.Values)
        {
            if (Equals(entry.InitialValue, entry.Value)) continue;
            result ??= new List<SynchedValue>();
            result.Add(new SynchedValue(entry.Index, entry.SerializerId, entry.Value));
        }
        return result;
    }

    //PackDirty returns the dirty set and clears it, maps to vanilla packDirty
    //The tracker uses GetDirtyValues + ClearDirty instead; this stays for callers that own the data exclusively
    public List<SynchedValue>? PackDirty()
    {
        var values = GetDirtyValues();
        ClearDirty();
        return values;
    }

    //ClearDirty clears the dirty marks after a tracking round has sent them to every observer
    public void ClearDirty()
    {
        _dirty = false;
        foreach (var entry in _entries.Values) entry.Dirty = false;
    }
}

//ISyncedEntity an entity with synced metadata
//The tracker only knows this interface and no longer special-cases concrete types like ServerPlayer/ItemEntity
public interface ISyncedEntity
{
    //SyncedData entity metadata container
    SynchedEntityData SyncedData { get; }
}
