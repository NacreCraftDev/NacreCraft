namespace NacreCraft.Client.Blaze3d.Systems;

//TimerQuery measures one GPU pass over a rotating set of timestamp queries, maps to vanilla com.mojang.blaze3d.systems.TimerQuery
//Three rotations keep the CPU from waiting on a frame the GPU is still writing
public sealed class TimerQuery : IDisposable
{
    private const int Rotations = 3;

    private readonly GpuQueryPool _queryPool;
    private readonly long[] _results = new long[Rotations];
    private readonly GpuDevice _device;
    private CommandEncoder? _activeEncoder;
    private int _currentRotationIndex;
    private Status _status = Status.NotRecording;

    public TimerQuery(GpuDevice device)
    {
        _device = device;
        _queryPool = device.CreateTimestampQueryPool(Rotations * 2);
    }

    public void BeginProfile()
    {
        if (_status != Status.NotRecording)
            throw new InvalidOperationException("Current profile not ended");

        _currentRotationIndex = (_currentRotationIndex + 1) % Rotations;
        _activeEncoder = _device.CreateCommandEncoder();
        _activeEncoder.WriteTimestamp(_queryPool, _currentRotationIndex * 2);
        _status = Status.Started;
    }

    public void EndProfile()
    {
        if (_status != Status.Started || _activeEncoder is null)
            throw new InvalidOperationException("EndProfile called before BeginProfile");

        _activeEncoder.WriteTimestamp(_queryPool, _currentRotationIndex * 2 + 1);
        _activeEncoder = null;
        _status = Status.AwaitingValues;
    }

    public long Get() => _results.Sum() / _results.Length;

    public Status GetStatus()
    {
        if (_status != Status.AwaitingValues) return _status;

        var timestamps = _queryPool.GetValues(_currentRotationIndex * 2, 2);
        if (timestamps[0] is { } startValue && timestamps[1] is { } endValue)
        {
            var delta = endValue - startValue;
            _results[_currentRotationIndex] = (long)(delta * _device.GetDeviceInfo().TimestampPeriod);
            _status = Status.NotRecording;
        }
        return _status;
    }

    public void Dispose() => _queryPool.Dispose();

    public enum Status
    {
        NotRecording,
        Started,
        AwaitingValues
    }
}
