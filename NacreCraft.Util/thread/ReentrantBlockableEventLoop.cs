namespace NacreCraft.Util.Thread;

//Reentrant event loop, maps to vanilla net.minecraft.util.thread.ReentrantBlockableEventLoop
//While a task is running, newly submitted work is queued again instead of executed inline, which keeps the nesting depth flat
public abstract class ReentrantBlockableEventLoop<T> : BlockableEventLoop<T> where T : class
{
    private int _reentrantCount;

    protected ReentrantBlockableEventLoop(string name, bool propagatesCrashes) : base(name, propagatesCrashes) { }

    protected override bool ScheduleExecutables() => RunningTask() || base.ScheduleExecutables();

    protected bool RunningTask() => Volatile.Read(ref _reentrantCount) != 0;

    protected override void DoRunTask(T task)
    {
        Interlocked.Increment(ref _reentrantCount);
        try
        {
            base.DoRunTask(task);
        }
        finally
        {
            Interlocked.Decrement(ref _reentrantCount);
        }
    }
}
