using NacreCraft.Logging;

namespace NacreCraft.Util.Concurrency;

//Shared priority pool, modelled on the Moonrise worker pool: threads are created on demand and exit when idle
//Subsystems submit work here instead of owning a fixed set of threads, so the total never exceeds the core budget
public sealed class PrioritisedWorkerPool
{
    private const int IdleTimeoutMs = 5000;

    //WorkItem a submitted task; the handle lets the owner raise its priority while it is still queued
    public sealed class WorkItem
    {
        internal readonly Action Work;

        //Priority the key it was last queued with, so a raise that changes nothing costs no re-queue
        internal long Priority;

        //Version bumped on every raise; the entry it leaves behind is skipped when dequeued
        internal int Version;

        //Claimed set by the worker that took the newest entry, so a late raise cannot run the task twice
        internal bool Claimed;

        internal WorkItem(Action work) => Work = work;
    }

    private readonly record struct QueueEntry(WorkItem Item, int Version);

    private readonly string _name;
    private readonly int _maxThreads;
    private readonly PriorityQueue<QueueEntry, long> _queue = new();
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
    public WorkItem Enqueue(Action work, long priority)
    {
        var item = new WorkItem(work) { Priority = priority };
        lock (_gate)
        {
            _queue.Enqueue(new QueueEntry(item, item.Version), priority);
            //Only grow when no idle thread can pick the item up, which keeps the pool near the actual load
            if (_idle == 0 && _threads < _maxThreads) StartWorkerLocked();
        }
        _work.Release();
        return item;
    }

    //Raise moves a still queued item to a new priority; already running work cannot be moved and is left alone
    public void Raise(WorkItem item, long priority)
    {
        lock (_gate)
        {
            if (item.Claimed || item.Priority == priority) return;
            item.Priority = priority;
            item.Version++;
            _queue.Enqueue(new QueueEntry(item, item.Version), priority);
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
                //Entries superseded by a raise are dropped here; the semaphore count they consumed was already taken
                while (_queue.TryDequeue(out var entry, out _))
                {
                    if (entry.Version != entry.Item.Version || entry.Item.Claimed) continue;
                    entry.Item.Claimed = true;
                    work = entry.Item.Work;
                    _idle--;
                    break;
                }
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
