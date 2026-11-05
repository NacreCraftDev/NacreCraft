namespace NacreCraft.Storage;

//ChunkTaskPriorityQueue priority queue of chunk tasks, maps to vanilla net.minecraft.server.level.ChunkTaskPriorityQueue
//Lower level runs first; equal levels run in submission order
public sealed class ChunkTaskPriorityQueue<T>
{
    private readonly PriorityQueue<(T Item, long Key), (int Level, long Sequence)> _queue = new();
    private long _sequence;

    public int Size => _queue.Count;

    public bool IsEmpty => _queue.Count == 0;

    //Push queues a task for a chunk key at a level, maps to vanilla push
    public void Push(T item, long key, int level)
        => _queue.Enqueue((item, key), (level, _sequence++));

    //TryPop takes the highest-priority task, maps to vanilla pop
    public bool TryPop(out T? item, out long key)
    {
        if (_queue.Count == 0)
        {
            item = default;
            key = 0;
            return false;
        }
        var entry = _queue.Dequeue();
        item = entry.Item;
        key = entry.Key;
        return true;
    }

    public void Clear() => _queue.Clear();
}
