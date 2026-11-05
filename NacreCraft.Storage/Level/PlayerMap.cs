using System.Collections.Concurrent;

namespace NacreCraft.Storage;

//PlayerMap per-player value table, maps to vanilla net.minecraft.server.level.PlayerMap
//Looked up by player uuid on the game thread and by chunk workers, so the backing dictionary is concurrent
public sealed class PlayerMap<V>
{
    private readonly ConcurrentDictionary<Guid, V> _values = new();

    public V? Get(Guid playerId) => _values.TryGetValue(playerId, out var value) ? value : default;

    public void Put(Guid playerId, V value) => _values[playerId] = value;

    public V? Remove(Guid playerId) => _values.TryRemove(playerId, out var value) ? value : default;

    public IReadOnlyCollection<KeyValuePair<Guid, V>> Entries => _values.ToArray();

    public int Count => _values.Count;

    public void Clear() => _values.Clear();
}
