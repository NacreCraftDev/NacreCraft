using NacreCraft.Logging;
using NacreCraft.Util;

namespace NacreCraft.Client.RealmsClient.Gui.Task;

//DataFetcher periodically recomputes values off the caller thread and hands them to subscribers
//maps to com.mojang.realmsclient.gui.task.DataFetcher
//Vanilla uses CompletableFuture with a polling getNow; here the task is polled through IsCompleted, same behavior
public sealed class DataFetcher
{
    private readonly TaskScheduler _executor;
    private readonly TimeUnit _resolution;
    private readonly TimeSource _timeSource;

    public DataFetcher(TaskScheduler executor, TimeUnit resolution, TimeSource timeSource)
    {
        _executor = executor;
        _resolution = resolution;
        _timeSource = timeSource;
    }

    public Task<T> CreateTask<T>(string id, Func<T> updater, TimeSpan period, RepeatedDelayStrategy repeatStrategy)
    {
        var periodInUnit = ConvertDuration(_resolution, period);
        if (periodInUnit == 0)
            throw new ArgumentOutOfRangeException(nameof(period), $"Period of {period} too short for selected resolution of {_resolution}");
        return new Task<T>(this, id, updater, periodInUnit, repeatStrategy);
    }

    public Subscription CreateSubscription() => new(this);

    //ConvertDuration expresses a duration in the given unit, maps to Java's TimeUnit.convert(Duration)
    private static long ConvertDuration(TimeUnit unit, TimeSpan duration)
        => TimeSource.ConvertNanos(unit, duration.Ticks * 100L);

    public sealed class Task<T>
    {
        private readonly DataFetcher _owner;
        private readonly string _id;
        private readonly Func<T> _updater;
        private readonly long _period;
        private readonly RepeatedDelayStrategy _repeatStrategy;
        private System.Threading.Tasks.Task<ComputationResult<T>>? _pendingTask;
        private SuccessfulComputationResult<T>? _lastResult;
        private long _nextUpdate = -1;

        internal Task(DataFetcher owner, string id, Func<T> updater, long period, RepeatedDelayStrategy repeatStrategy)
        {
            _owner = owner;
            _id = id;
            _updater = updater;
            _period = period;
            _repeatStrategy = repeatStrategy;
        }

        internal SuccessfulComputationResult<T>? LastResult => _lastResult;

        internal void UpdateIfNeeded(long currentTime)
        {
            if (_pendingTask is not null)
            {
                if (!_pendingTask.IsCompleted) return;
                var result = _pendingTask.Result;
                _pendingTask = null;
                if (result.Error is null)
                {
                    _lastResult = new SuccessfulComputationResult<T>(result.Value!, result.Time);
                    _nextUpdate = result.Time + _period * _repeatStrategy.DelayCyclesAfterSuccess();
                }
                else
                {
                    var cycles = _repeatStrategy.DelayCyclesAfterFailure();
                    Log.Warning($"Failed to process task {_id}, will repeat after {cycles} cycles: {result.Error.Message}");
                    _nextUpdate = result.Time + _period * cycles;
                }
            }
            if (_nextUpdate > currentTime) return;
            _pendingTask = System.Threading.Tasks.Task.Factory.StartNew(RunUpdater, CancellationToken.None, TaskCreationOptions.None, _owner._executor);
        }

        private ComputationResult<T> RunUpdater()
        {
            try
            {
                return new ComputationResult<T>(_updater(), null, _owner._timeSource.Get(_owner._resolution));
            }
            catch (System.Exception exception)
            {
                return new ComputationResult<T>(default, exception, _owner._timeSource.Get(_owner._resolution));
            }
        }

        public void Reset()
        {
            _pendingTask = null;
            _lastResult = null;
            _nextUpdate = -1;
        }
    }

    public sealed class Subscription
    {
        private readonly DataFetcher _owner;
        private readonly List<SubscribedTask> _subscriptions = new();

        internal Subscription(DataFetcher owner) => _owner = owner;

        public void Subscribe<T>(Task<T> task, Action<T> output)
        {
            var subscription = new SubscribedTask<T>(task, output);
            _subscriptions.Add(subscription);
            subscription.RunCallbackIfNeeded();
        }

        public void ForceUpdate()
        {
            foreach (var subscription in _subscriptions) subscription.RunCallback();
        }

        public void Tick()
        {
            var currentTime = _owner._timeSource.Get(_owner._resolution);
            foreach (var subscription in _subscriptions) subscription.Update(currentTime);
        }

        public void Reset()
        {
            foreach (var subscription in _subscriptions) subscription.Reset();
        }
    }

    private abstract class SubscribedTask
    {
        public abstract void Update(long currentTime);

        public abstract void RunCallbackIfNeeded();

        public abstract void RunCallback();

        public abstract void Reset();
    }

    private sealed class SubscribedTask<T>(Task<T> task, Action<T> output) : SubscribedTask
    {
        private long _lastCheckTime = -1;

        public override void Update(long currentTime)
        {
            task.UpdateIfNeeded(currentTime);
            RunCallbackIfNeeded();
        }

        public override void RunCallbackIfNeeded()
        {
            if (task.LastResult is { } lastResult && _lastCheckTime < lastResult.Time)
            {
                output(lastResult.Value);
                _lastCheckTime = lastResult.Time;
            }
        }

        public override void RunCallback()
        {
            if (task.LastResult is not { } lastResult) return;
            output(lastResult.Value);
            _lastCheckTime = lastResult.Time;
        }

        public override void Reset()
        {
            task.Reset();
            _lastCheckTime = -1;
        }
    }

    internal readonly record struct SuccessfulComputationResult<T>(T Value, long Time);

    private readonly record struct ComputationResult<T>(T? Value, System.Exception? Error, long Time);
}
