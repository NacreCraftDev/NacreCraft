namespace NacreCraft.Util.Thread;

//TaskScheduler task scheduler interface, maps to vanilla net.minecraft.util.thread.TaskScheduler
//T is the wrapped task type; subclasses decide how a plain action is turned into one
public interface TaskScheduler<T> where T : class
{
    string Name { get; }

    void Schedule(T task);

    //WrapRunnable turns a plain action into this scheduler's task type, maps to vanilla wrapRunnable
    T WrapRunnable(Action runnable);

    //Close stops accepting work; the default implementation does nothing, maps to the vanilla default close
    void Close() { }

    //ScheduleWithResult schedules a task that completes the returned task, maps to vanilla scheduleWithResult
    Task<TSource> ScheduleWithResult<TSource>(Action<TaskCompletionSource<TSource>> futureConsumer)
    {
        var tcs = new TaskCompletionSource<TSource>(TaskCreationOptions.RunContinuationsAsynchronously);
        Schedule(WrapRunnable(() => futureConsumer(tcs)));
        return tcs.Task;
    }

    //WrapExecutor adapts a plain executor to a scheduler, maps to vanilla TaskScheduler.wrapExecutor
    static TaskScheduler<Action> WrapExecutor(string name, IExecutor executor)
        => new ExecutorTaskScheduler(name, executor);
}

//ExecutorTaskScheduler schedules straight onto an executor, maps to the anonymous scheduler of vanilla TaskScheduler.wrapExecutor
internal sealed class ExecutorTaskScheduler : TaskScheduler<Action>
{
    private readonly IExecutor _executor;

    public ExecutorTaskScheduler(string name, IExecutor executor)
    {
        Name = name;
        _executor = executor;
    }

    public string Name { get; }

    public void Schedule(Action task) => _executor.Execute(task);

    public Action WrapRunnable(Action runnable) => runnable;

    public override string ToString() => Name;
}
