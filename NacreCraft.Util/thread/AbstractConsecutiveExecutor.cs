using NacreCraft.Logging;
using NacreCraft.Util.Profiling.Metrics;

namespace NacreCraft.Util.Thread;

//Consecutive executor abstract base class, maps to vanilla AbstractConsecutiveExecutor
//State machine SLEEPING/RUNNING/CLOSED switched by CAS to guarantee single-threaded serial execution
//Core mechanism: schedule enqueues then CAS-wakes executor.Execute(this.Run) to trigger the execution loop
public abstract class AbstractConsecutiveExecutor<T> : TaskScheduler<T>, ProfilerMeasured where T : class
{
    private const int Sleeping = 0;
    private const int Running = 1;
    private const int Closed = 2;

    private volatile int _status = Sleeping;
    private readonly StrictQueue<T> _queue;
    private readonly IExecutor _executor;
    private readonly string _name;

    protected AbstractConsecutiveExecutor(StrictQueue<T> queue, IExecutor executor, string name)
    {
        _queue = queue;
        _executor = executor;
        _name = name;
        MetricsRegistry.Instance.Add(this);
    }

    public string Name => _name;
    public int Size => _queue.Size;
    public bool HasWork => IsRunning && !_queue.IsEmpty;
    protected bool IsRunning => _status == Running;
    protected bool IsClosed => _status == Closed;

    public abstract T WrapRunnable(Action runnable);

    //RunTask runs one task; the strict queue's task type decides how
    protected abstract void RunTask(T task);

    private bool CanBeScheduled() => !IsClosed && !_queue.IsEmpty;

    public void Close() => Interlocked.Exchange(ref _status, Closed);

    private bool PollTask()
    {
        if (_status != Running) return false;
        var task = _queue.Pop();
        if (task is null) return false;
        try
        {
            NacreCraft.Util.Util.RunNamed(() => RunTask(task), _name);
        }
        catch (Exception e)
        {
            //Vanilla lets the exception reach the executor thread; a .NET threadpool worker would take the process down,
            //so it is recorded here and the loop keeps going
            Log.Exception(e, $"Error executing task on {_name}");
        }
        return true;
    }

    //The executor callback entry runs one task at a time then returns to sleep and registers the next
    public void Run()
    {
        try { PollTask(); }
        finally
        {
            Interlocked.CompareExchange(ref _status, Sleeping, Running);
            RegisterForExecution();
        }
    }

    public void RunAll()
    {
        try
        {
            while (PollTask()) { }
        }
        finally
        {
            Interlocked.CompareExchange(ref _status, Sleeping, Running);
            RegisterForExecution();
        }
    }

    public void Schedule(T task)
    {
        _queue.Push(task);
        RegisterForExecution();
    }

    private void RegisterForExecution()
    {
        if (!CanBeScheduled()) return;
        if (Interlocked.CompareExchange(ref _status, Running, Sleeping) != Sleeping) return;
        try { _executor.Execute(Run); }
        catch (Exception)
        {
            try { _executor.Execute(Run); }
            catch (Exception e2) { Log.Exception(e2, $"Could not schedule ConsecutiveExecutor {_name}"); }
        }
    }

    public List<MetricSampler> ProfiledMetrics()
        => new() { MetricSampler.Create($"{_name}-queue-size", MetricCategory.ConsecutiveExecutors, () => Size) };

    public override string ToString() => $"{_name} {_status} {_queue.IsEmpty}";
}
