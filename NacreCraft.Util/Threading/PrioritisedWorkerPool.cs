using NacreCraft.Logging;

namespace NacreCraft.Util.Concurrency;

//Shared priority pool, modelled on the Moonrise worker pool: threads are created on demand and exit when idle
//Subsystems submit work here instead of owning a fixed set of threads, so the total never exceeds the core budget
public sealed class PrioritisedWorkerPool
{
    private const int IdleTimeoutMs = 5000;

    private readonly string _name;
    private readonly int _maxThreads;
    private readonly PriorityQueue<Action, long> _queue = new();
    private readonly object _gate = new();
    private readonly System.Threading.SemaphoreSlim _work = new(0);
    private int _threads;
    private int _idle;

    public PrioritisedWorkerPool(string name, int maxThreads)
    {
        _name = name;
        _maxThreads = Math.Max(1, maxThreads);
    }

    //ThreadCount current live worker count, for diagnostics
    public int ThreadCount => Volatile.Read(ref _threads);

    //PendingCount queued work items, for diagnostics
    public int PendingCount
    {
        get
        {
            lock (_gate) return _queue.Count;
        }
    }

    //Enqueue submits work; lower priority values run first, equal values keep submission order
    public void Enqueue(Action work, long priority)
    {
        lock (_gate)
        {
            _queue.Enqueue(work, priority);
            //Only grow when no idle thread can pick the item up, which keeps the pool near the actual load
            if (_idle == 0 && _threads < _maxThreads) StartWorkerLocked();
        }
        _work.Release();
    }

    private void StartWorkerLocked()
    {
        _threads++;
        _idle++;
        var thread = new System.Threading.Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = $"{_name}-{_threads}",
            Priority = System.Threading.ThreadPriority.BelowNormal,
        };
        thread.Start();
    }

    private void WorkerLoop()
    {
        while (true)
        {
            if (!_work.Wait(IdleTimeoutMs))
            {
                lock (_gate)
                {
                    //Retire this thread when the queue has drained, so an idle server does not keep a full pool alive
                    if (_queue.Count == 0)
                    {
                        _threads--;
                        _idle--;
                        return;
                    }
                }
                continue;
            }

            Action? work = null;
            lock (_gate)
            {
                if (_queue.TryDequeue(out work, out _)) _idle--;
            }
            if (work is null) continue;
            try
            {
                work();
            }
            catch (Exception exception)
            {
                Log.Warning($"{_name} task failed: {exception}");
            }
            finally
            {
                lock (_gate) _idle++;
            }
        }
    }
}
