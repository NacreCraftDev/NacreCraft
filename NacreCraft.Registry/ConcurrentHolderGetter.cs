using System.Collections.Concurrent;

namespace NacreCraft.Registry;

//ConcurrentHolderGetter holder lookup that caches results under a lock, maps to vanilla net.minecraft.core.registries.ConcurrentHolderGetter
//Worldgen workers share one holder source; the lock guards the source while the cache keeps repeated lookups off it
public sealed class ConcurrentHolderGetter<T> : HolderLookup<T> where T : class
{
    private readonly object _gate;
    private readonly HolderLookup<T> _original;
    private readonly ConcurrentDictionary<ResourceKey<T>, Holder<T>?> _elementCache = new();

    public ConcurrentHolderGetter(object gate, HolderLookup<T> original)
    {
        _gate = gate;
        _original = original;
    }

    public Holder<T>? Get(ResourceKey<T> key)
        => _elementCache.GetOrAdd(key, id =>
        {
            lock (_gate) return _original.Get(id);
        });

    public IEnumerable<Holder<T>> ListElements() => _original.ListElements();

    public IEnumerable<KeyValuePair<TagKey<T>, HolderSet<T>>> ListTags() => _original.ListTags();

    public bool CanSerializeIn(HolderOwner<T> owner) => _original.CanSerializeIn(owner);
}
