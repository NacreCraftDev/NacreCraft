using System.Collections.Concurrent;
using NacreCraft.Logging;
using NacreCraft.Util.Profiling.Metrics;

namespace NacreCraft.Util.Thread;

//BlockableEventLoop event loop base class, maps to vanilla net.minecraft.util.thread.BlockableEventLoop
//Collects tasks from other threads and runs them on the owning thread; a blocking caller can pump the queue while it waits
public abstract class BlockableEventLoop<T> : IExecutor, TaskScheduler<T>, ProfilerMeasured where T : class
{
    //BlockTimeNanos how long a waiting loop parks between polls, maps to vanilla BLOCK_TIME_NANOS
    public const long BlockTimeNanos = 100_000L;

    //_delayedCrash crash report raised on the next poll; static so a worker thread can hand a crash to the main loop
    private static volatile Func<CrashReport>? _delayedCrash;
    private static readonly object DelayedCrashLock = new();

    private readonly bool _propagatesCrashes;
    private readonly string _name;
    private readonly ConcurrentQueue<T> _pendingRunnables = new();
    private int _blockingCount;

    protected BlockableEventLoop(string name, bool propagatesCrashes)
    {
        _propagatesCrashes = propagatesCrashes;
        _name = name;
        MetricsRegistry.Instance.Add(this);
    }

    protected abstract bool ShouldRun(T task);

    //The containing namespace ends in Thread, so the type is written out in full
    protected abstract System.Threading.Thread RunningThread { get; }

    public abstract T WrapRunnable(Action runnable);

    //RunTask runs one task; the vanilla task type carries its own run method
    protected abstract void RunTask(T task);

    public string Name => _name;

    public bool IsSameThread() => System.Threading.Thread.CurrentThread == RunningThread;

    protected virtual bool ScheduleExecutables() => !IsSameThread();

    public int PendingTasksCount => _pendingRunnables.Count;

    public Task<V> Submit<V>(Func<V> supplier)
    {
        if (!ScheduleExecutables()) return Task.FromResult(supplier());
        var tcs = new TaskCompletionSource<V>(TaskCreationOptions.RunContinuationsAsynchronously);
        Execute(() =>
        {
            try { tcs.SetResult(supplier()); }
            catch (Exception e) { tcs.SetException(e); }
        });
        return tcs.Task;
    }

    private Task SubmitAsync(Action runnable)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Execute(() =>
        {
            try { runnable(); tcs.SetResult(); }
            catch (Exception e) { tcs.SetException(e); }
        });
        return tcs.Task;
    }

    public Task Submit(Action runnable)
    {
        if (ScheduleExecutables()) return SubmitAsync(runnable);
        runnable();
        return Task.CompletedTask;
    }

    public void ExecuteBlocking(Action runnable)
    {
        if (!IsSameThread()) SubmitAsync(runnable).GetAwaiter().GetResult();
        else runnable();
    }

    public void Schedule(T task) => _pendingRunnables.Enqueue(task);

    //IExecutor.Execute runs on this thread directly when already on it, otherwise queues
    public void Execute(Action command)
    {
        var task = WrapRunnable(command);
        if (ScheduleExecutables()) Schedule(task);
        else DoRunTask(task);
    }

    public void ExecuteIfPossible(Action command) => Execute(command);

    protected void DropAllTasks()
        => _pendingRunnables.Clear();

    protected void RunAllTasks()
    {
        while (PollTask()) { }
    }

    protected bool ShouldRunAllTasks() => Volatile.Read(ref _blockingCount) > 0;

    protected bool PollTask()
    {
        ThrowDelayedException();
        if (!_pendingRunnables.TryPeek(out var next)) return false;
        if (!ShouldRunAllTasks() && !ShouldRun(next)) return false;
        if (!_pendingRunnables.TryDequeue(out var task)) return false;
        DoRunTask(task);
        return true;
    }

    public void ManagedBlock(Func<bool> condition)
    {
        Interlocked.Increment(ref _blockingCount);
        try
        {
            while (!condition())
            {
                if (PollTask()) continue;
                WaitForTasks();
            }
        }
        finally
        {
            Interlocked.Decrement(ref _blockingCount);
        }
    }

    protected virtual void WaitForTasks()
    {
        System.Threading.Thread.Yield();
        //Vanilla parks for BlockTimeNanos here; .NET has no parkNanos, a zero-timeout sleep hands the CPU over the same way
        System.Threading.Thread.Sleep(0);
    }

    protected virtual void DoRunTask(T task)
    {
        try
        {
            RunTask(task);
        }
        catch (Exception e)
        {
            Log.Error($"Error executing task on {_name} {e}");
            if (IsNonRecoverable(e)) throw;
        }
    }

    public List<MetricSampler> ProfiledMetrics()
        => new() { MetricSampler.Create($"{_name}-pending-tasks", MetricCategory.EventLoops, () => PendingTasksCount) };

    //IsNonRecoverable whether the failure must be propagated, maps to vanilla isNonRecoverable
    public static bool IsNonRecoverable(Exception t)
    {
        if (t is ReportedException reported) return IsNonRecoverable(reported.Report.Exception);
        return t is OutOfMemoryException or StackOverflowException;
    }

    private void ThrowDelayedException()
    {
        var delayedCrash = _delayedCrash;
        if (_propagatesCrashes && delayedCrash is not null) throw new ReportedException(delayedCrash());
    }

    public void DelayCrash(CrashReport crashReport) => _delayedCrash = () => crashReport;

    //RelayDelayCrash hands a crash from a worker thread to the event loop, maps to vanilla relayDelayCrash
    //The existing crash keeps priority; the new exception is attached through an AggregateException since .NET has no addSuppressed
    public static void RelayDelayCrash(CrashReport crashReport)
    {
        lock (DelayedCrashLock)
        {
            var delayedCrash = _delayedCrash;
            if (delayedCrash is null)
            {
                _delayedCrash = () => crashReport;
                return;
            }
            _delayedCrash = () =>
            {
                var first = delayedCrash();
                return new CrashReport(first.Title, new AggregateException(first.Exception, crashReport.Exception));
            };
        }
    }
}
