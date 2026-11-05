using System.Collections.Concurrent;
using NacreCraft.DataFixer;
using NacreCraft.Logging;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Game.Util.WorldUpdate;

//UpgradeProgress shared progress state of one world upgrade, maps to vanilla net.minecraft.util.worldupdate.UpgradeProgress
public class UpgradeProgress
{
    private volatile bool _finished;
    private readonly FileFixStats _totalFileFixStats = new();
    private readonly FileFixStats _typeFileFixStats = new();
    private readonly FileFixStats _runningFileFixerStats = new();
    private volatile float _totalProgress;
    private readonly AtomicCounter _totalChunks = new();
    private readonly AtomicCounter _converted = new();
    private readonly AtomicCounter _skipped = new();
    private readonly ConcurrentDictionary<ResourceKey<Level>, float> _progressMap = new();
    private volatile bool _canceled;
    private volatile DataFixTypes? _dataFixType;
    private volatile Status _status = Status.COUNTING;
    private Type? _type;
    private long _lastLoggedProgressTime;

    public bool IsFinished() => _finished;

    public virtual void SetFinished(bool finished) => _finished = finished;

    public FileFixStats GetTotalFileFixStats() => _totalFileFixStats;

    public FileFixStats GetTypeFileFixStats() => _typeFileFixStats;

    public FileFixStats GetRunningFileFixerStats() => _runningFileFixerStats;

    public virtual void AddTotalFileFixOperations(int additionalFileFixOperations)
    {
        _totalFileFixStats.TotalOperations.Add(additionalFileFixOperations);
        _typeFileFixStats.TotalOperations.Add(additionalFileFixOperations);
    }

    public float GetTotalProgress() => _totalProgress;

    public virtual void SetTotalProgress(float totalProgress) => _totalProgress = totalProgress;

    public int GetTotalChunks() => _totalChunks.Value;

    public virtual void AddTotalChunks(int additionalTotalChunks) => _totalChunks.Add(additionalTotalChunks);

    public int GetConverted() => _converted.Value;

    public virtual void SetDimensionProgress(ResourceKey<Level> dimensionKey, float currentProgress)
        => _progressMap[dimensionKey] = currentProgress;

    public float GetDimensionProgress(ResourceKey<Level> dimensionKey)
        => _progressMap.GetValueOrDefault(dimensionKey);

    public virtual void IncrementConverted() => _converted.Increment();

    public int GetSkipped() => _skipped.Value;

    public virtual void IncrementSkipped() => _skipped.Increment();

    public virtual void IncrementFinishedOperations() => IncrementFinishedOperationsBy(1);

    public virtual void IncrementFinishedOperationsBy(int count)
    {
        _totalFileFixStats.FinishedOperations.Add(count);
        _typeFileFixStats.FinishedOperations.Add(count);
        LogProgress();
    }

    public virtual void SetCanceled() => _canceled = true;

    public bool IsCanceled() => _canceled;

    public Status GetStatus() => _status;

    public virtual void SetStatus(Status status) => _status = status;

    public DataFixTypes? GetDataFixType() => _dataFixType;

    public void SetType(Type type)
    {
        _type = type;
        _typeFileFixStats.Reset();
    }

    public Type? GetType() => _type;

    public virtual void SetApplicableFixerAmount(int amount) => _runningFileFixerStats.TotalOperations.Set(amount);

    public virtual void IncrementRunningFileFixer() => _runningFileFixerStats.FinishedOperations.Increment();

    public virtual void Reset(DataFixTypes dataFixType)
    {
        _totalFileFixStats.Reset();
        _typeFileFixStats.Reset();
        _totalChunks.Set(0);
        _converted.Set(0);
        _skipped.Set(0);
        _dataFixType = dataFixType;
    }

    //LogProgress prints at most once a second, maps to vanilla logProgress
    public void LogProgress()
    {
        var now = Environment.TickCount64;
        var last = Volatile.Read(ref _lastLoggedProgressTime);
        if (now < last + 1000L) return;
        Interlocked.Exchange(ref _lastLoggedProgressTime, now);
        var finished = _totalFileFixStats.FinishedOperationsValue;
        var total = _totalFileFixStats.TotalOperationsValue;
        var progress = total == 0 ? 0.0f : (float)finished / total;
        Log.Info($"Upgrading progress: {(int)(progress * 100.0f)}%");
    }

    public enum Status
    {
        COUNTING,
        FAILED,
        FINISHED,
        UPGRADING
    }

    public enum Type
    {
        FILES,
        LEGACY_STRUCTURES,
        REGIONS
    }

    public sealed class FileFixStats
    {
        internal readonly AtomicCounter FinishedOperations = new();
        internal readonly AtomicCounter TotalOperations = new();

        public int FinishedOperationsValue => FinishedOperations.Value;

        public int TotalOperationsValue => TotalOperations.Value;

        public void Reset()
        {
            FinishedOperations.Set(0);
            TotalOperations.Set(0);
        }

        public float GetProgress()
        {
            var total = TotalOperationsValue;
            if (total == 0) return 0.0f;
            return Mth.Clamp((float)FinishedOperationsValue / total, 0.0f, 1.0f);
        }
    }

    //Noop progress used when no upgrade UI is attached, maps to vanilla UpgradeProgress.Noop
    public sealed class Noop : UpgradeProgress
    {
        public override void SetFinished(bool finished)
        {
        }

        public override void AddTotalFileFixOperations(int additionalFileFixOperations)
        {
        }

        public override void SetTotalProgress(float totalProgress)
        {
        }

        public override void AddTotalChunks(int additionalTotalChunks)
        {
        }

        public override void SetDimensionProgress(ResourceKey<Level> dimensionKey, float currentProgress)
        {
        }

        public override void IncrementConverted()
        {
        }

        public override void IncrementSkipped()
        {
        }

        public override void IncrementFinishedOperations()
        {
        }

        public override void IncrementFinishedOperationsBy(int count)
        {
        }

        public override void SetCanceled()
        {
        }

        public override void SetStatus(Status status)
        {
        }

        public override void SetApplicableFixerAmount(int amount)
        {
        }

        public override void IncrementRunningFileFixer()
        {
        }

        public override void Reset(DataFixTypes dataFixType)
        {
        }
    }
}

//AtomicCounter small atomic integer wrapper, the C# stand-in for the Java AtomicInteger used throughout UpgradeProgress
internal sealed class AtomicCounter
{
    private int _value;

    public int Value => Volatile.Read(ref _value);

    public void Add(int count) => Interlocked.Add(ref _value, count);

    public void Increment() => Interlocked.Increment(ref _value);

    public void Set(int value) => Interlocked.Exchange(ref _value, value);
}

//UpgradeTypes labels of the three upgrade types, maps to the label field of vanilla UpgradeProgress.Type
public static class UpgradeTypes
{
    public static string GetSerializedName(this UpgradeProgress.Type type) => type switch
    {
        UpgradeProgress.Type.FILES => "files",
        UpgradeProgress.Type.LEGACY_STRUCTURES => "legacy_structures",
        _ => "regions"
    };

    public static Component Label(this UpgradeProgress.Type type)
        => Component.Translatable($"upgradeWorld.progress.type.{type.GetSerializedName()}");
}

