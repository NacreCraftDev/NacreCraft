namespace NacreCraft.Storage;

//ThrottlingChunkTaskDispatcher dispatcher that limits how many tasks are in flight, maps to vanilla net.minecraft.server.level.ThrottlingChunkTaskDispatcher
//The inner dispatcher runs synchronously here, so the throttle only gates how many tasks are accepted per drain
public sealed class ThrottlingChunkTaskDispatcher : IDisposable
{
    private readonly ChunkTaskDispatcher _inner;
    private readonly int _maxTasks;
    private int _inFlight;

    public ThrottlingChunkTaskDispatcher(ChunkTaskDispatcher inner, int maxTasks)
    {
        _inner = inner;
        _maxTasks = Math.Max(1, maxTasks);
    }

    public void Submit(long key, int level, Action task)
    {
        if (_inFlight >= _maxTasks) return;
        _inFlight++;
        _inner.Submit(key, level, () =>
        {
            try { task(); }
            finally { _inFlight--; }
        });
    }

    public void RunPending() => _inner.RunPending();

    public void Dispose() => _inner.Dispose();
}
