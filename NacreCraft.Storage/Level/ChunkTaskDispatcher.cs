namespace NacreCraft.Storage;

//ChunkTaskDispatcher serializes chunk tasks through one executor, maps to vanilla net.minecraft.server.level.ChunkTaskDispatcher
//Tasks are queued per chunk and drained in priority order; the executor decides on which thread they run
public class ChunkTaskDispatcher : IDisposable
{
    private readonly ChunkTaskPriorityQueue<Action> _queue = new();
    private readonly Func<Action, Task> _executor;
    private readonly object _gate = new();
    private bool _draining;
    private bool _disposed;

    public ChunkTaskDispatcher(Func<Action, Task> executor) => _executor = executor;

    //Submit queues a task for a chunk key at a level
    public void Submit(long key, int level, Action task)
    {
        if (_disposed) return;
        _queue.Push(task, key, level);
    }

    //RunPending drains queued tasks through the executor one at a time, maps to vanilla pollTask
    public void RunPending()
    {
        lock (_gate)
        {
            if (_draining || _disposed) return;
            _draining = true;
        }
        try
        {
            while (!_disposed && _queue.TryPop(out var task, out _))
            {
                if (task is null) continue;
                _executor(task).GetAwaiter().GetResult();
            }
        }
        finally
        {
            _draining = false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _queue.Clear();
    }
}
