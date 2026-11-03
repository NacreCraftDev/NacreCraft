namespace NacreCraft.Util.Thread;

//ParallelMapTransform runs a map transform in parallel over an executor, maps to vanilla net.minecraft.util.thread.ParallelMapTransform
//The input is split into batches so a large map does not flood the executor with one task per entry
public static class ParallelMapTransform
{
    //DefaultTasksPerThread how many batches each worker thread gets by default, maps to vanilla DEFAULT_TASKS_PER_THREAD
    private const int DefaultTasksPerThread = 16;

    public static Task<Dictionary<K, V>> Schedule<K, U, V>(IReadOnlyDictionary<K, U> input, Func<K, U, V?> operation, int maxTaskCount, IExecutor executor)
        where V : class
    {
        var inputSize = input.Count;
        if (inputSize == 0) return Task.FromResult(new Dictionary<K, V>());
        if (inputSize == 1)
        {
            var element = input.First();
            return RunOn(executor, () =>
            {
                var result = operation(element.Key, element.Value);
                return result is null
                    ? new Dictionary<K, V>()
                    : new Dictionary<K, V> { [element.Key] = result };
            });
        }
        SplitterBase<K, U, V> splitter = inputSize <= maxTaskCount
            ? new SingleTaskSplitter<K, U, V>(operation, inputSize)
            : new BatchedTaskSplitter<K, U, V>(operation, inputSize, maxTaskCount);
        return splitter.ScheduleTasks(input, executor);
    }

    public static Task<Dictionary<K, V>> Schedule<K, U, V>(IReadOnlyDictionary<K, U> input, Func<K, U, V?> operation, IExecutor executor)
        where V : class
        => Schedule(input, operation, NacreCraft.Util.Util.MaxAllowedExecutorThreads() * DefaultTasksPerThread, executor);

    private static Task<T> RunOn<T>(IExecutor executor, Func<T> action)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        executor.Execute(() =>
        {
            try { tcs.SetResult(action()); }
            catch (Exception e) { tcs.SetException(e); }
        });
        return tcs.Task;
    }

    private static Task RunOn(IExecutor executor, Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        executor.Execute(() =>
        {
            try { action(); tcs.SetResult(); }
            catch (Exception e) { tcs.SetException(e); }
        });
        return tcs.Task;
    }

    private sealed class SingleTaskSplitter<K, U, V> : SplitterBase<K, U, V> where V : class
    {
        public SingleTaskSplitter(Func<K, U, V?> operation, int size) : base(operation, size, size) { }

        protected override int BatchSize(int index) => 1;

        protected override Task ScheduleBatch(Container<K, U, V> container, int startIndex, int endIndex, IExecutor executor)
            => RunOn(executor, () => container.ApplyOperation(startIndex));

        protected override async Task<Dictionary<K, V>> ScheduleFinalOperation(Task allTasksDone, Container<K, U, V> container)
        {
            await allTasksDone.ConfigureAwait(false);
            var result = new Dictionary<K, V>(container.Size);
            for (var i = 0; i < container.Size; i++) container.CopyOut(i, result);
            return result;
        }
    }

    private sealed class BatchedTaskSplitter<K, U, V> : SplitterBase<K, U, V> where V : class
    {
        private readonly Dictionary<K, V> _result;
        private readonly int _batchSize;
        private readonly int _firstUndersizedBatchIndex;

        public BatchedTaskSplitter(Func<K, U, V?> operation, int size, int maxTasks) : base(operation, size, maxTasks)
        {
            _result = new Dictionary<K, V>(size);
            _batchSize = Mth.PositiveCeilDiv(size, maxTasks);
            var fullCapacity = _batchSize * maxTasks;
            var leftoverCapacity = fullCapacity - size;
            _firstUndersizedBatchIndex = maxTasks - leftoverCapacity;
        }

        protected override Task ScheduleBatch(Container<K, U, V> container, int startIndex, int endIndex, IExecutor executor)
            => RunOn(executor, () =>
            {
                for (var i = startIndex; i < endIndex; i++) container.ApplyOperation(i);
                lock (_result)
                {
                    for (var i = startIndex; i < endIndex; i++) container.CopyOut(i, _result);
                }
            });

        protected override int BatchSize(int index)
            => index < _firstUndersizedBatchIndex ? _batchSize : _batchSize - 1;

        protected override async Task<Dictionary<K, V>> ScheduleFinalOperation(Task allTasksDone, Container<K, U, V> container)
        {
            await allTasksDone.ConfigureAwait(false);
            return _result;
        }
    }

    private abstract class SplitterBase<K, U, V> where V : class
    {
        private readonly Container<K, U, V> _container;
        private readonly Task[] _tasks;
        private int _lastScheduledIndex;
        private int _currentIndex;
        private int _batchIndex;

        protected SplitterBase(Func<K, U, V?> operation, int size, int taskCount)
        {
            _container = new Container<K, U, V>(operation, size);
            _tasks = new Task[taskCount];
        }

        private int PendingBatchSize() => _currentIndex - _lastScheduledIndex;

        public Task<Dictionary<K, V>> ScheduleTasks(IReadOnlyDictionary<K, U> input, IExecutor executor)
        {
            foreach (var (key, value) in input)
            {
                _container.Put(_currentIndex++, key, value);
                if (PendingBatchSize() != BatchSize(_batchIndex)) continue;
                _tasks[_batchIndex++] = ScheduleBatch(_container, _lastScheduledIndex, _currentIndex, executor);
                _lastScheduledIndex = _currentIndex;
            }
            return ScheduleFinalOperation(Task.WhenAll(_tasks), _container);
        }

        protected abstract int BatchSize(int index);

        protected abstract Task ScheduleBatch(Container<K, U, V> container, int startIndex, int endIndex, IExecutor executor);

        protected abstract Task<Dictionary<K, V>> ScheduleFinalOperation(Task allTasksDone, Container<K, U, V> container);
    }

    //Container holds the keys and in-place values for one transform, maps to vanilla ParallelMapTransform.Container
    private sealed class Container<K, U, V> where V : class
    {
        private readonly Func<K, U, V?> _operation;
        private readonly object?[] _keys;
        private readonly object?[] _values;

        public Container(Func<K, U, V?> operation, int size)
        {
            _operation = operation;
            _keys = new object[size];
            _values = new object[size];
        }

        public void Put(int index, K key, U input)
        {
            _keys[index] = key;
            _values[index] = input;
        }

        public void ApplyOperation(int index)
        {
            var key = (K)_keys[index]!;
            var input = (U)_values[index]!;
            _values[index] = _operation(key, input);
        }

        public void CopyOut(int index, Dictionary<K, V> output)
        {
            if (_values[index] is not V value) return;
            output[(K)_keys[index]!] = value;
        }

        public int Size => _keys.Length;
    }
}
