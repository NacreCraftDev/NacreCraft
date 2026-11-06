namespace NacreCraft.Client.Blaze3d.Systems;

//TracyGpuProfiler timestamp-based GPU zone profiler, maps to vanilla com.mojang.blaze3d.systems.TracyGpuProfiler
//Vanilla submits the timestamps to a Tracy client; this project has no Tracy integration, so the ring bookkeeping stays
//and endFrame drains the ready values without forwarding them
public sealed class TracyGpuProfiler : IDisposable
{
    private const int MaxQueries = 1024;

    private readonly GpuQueryPool _queries;
    private int _head;
    private int _tail;

    public TracyGpuProfiler(GpuDevice device)
    {
        _queries = device.CreateTimestampQueryPool(MaxQueries);
    }

    public void PushZone(CommandEncoder encoder, string name)
    {
        var queryId = NextQueryId();
        encoder.WriteTimestamp(_queries, queryId);
    }

    public void PopZone(CommandEncoder encoder)
    {
        var queryId = NextQueryId();
        encoder.WriteTimestamp(_queries, queryId);
    }

    //EndFrame advances the tail over every timestamp the GPU has written, keeping the ring from filling up
    public void EndFrame()
    {
        if (_head < _tail)
        {
            var timestamps = _queries.GetValues(_tail, MaxQueries - _tail);
            for (var i = 0; i < timestamps.Length; i++)
            {
                if (timestamps[i] is null) return;
                _tail = (_tail + 1) % MaxQueries;
            }
        }
        if (_tail < _head)
        {
            var timestamps = _queries.GetValues(_tail, _head - _tail);
            for (var i = 0; i < timestamps.Length; i++)
            {
                if (timestamps[i] is null) return;
                _tail = (_tail + 1) % MaxQueries;
            }
        }
    }

    private int NextQueryId()
    {
        var id = _head;
        _head = (_head + 1) % MaxQueries;
        return id;
    }

    public void Dispose() => _queries.Dispose();
}
