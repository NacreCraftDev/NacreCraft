using System.Diagnostics;
using NacreCraft.Logging;

namespace NacreCraft.Resources;

//ProfiledReloadInstance a reload that reports how long each listener took
//maps to vanilla net.minecraft.server.packs.resources.ProfiledReloadInstance
//Vanilla splits every listener into preparation and reload tasks and counts both; NC runs one synchronous call per listener, so only the wall time is reported
public sealed class ProfiledReloadInstance : ReloadInstance
{
    private readonly ResourceManager _rm;
    private readonly IReadOnlyList<PreparableReloadListener> _listeners;
    private readonly List<ListenerTiming> _timings = new();
    private int _index;
    private long _totalNanos;
    private Exception? _failure;
    private string? _failedListener;

    private ProfiledReloadInstance(ResourceManager rm, IReadOnlyList<PreparableReloadListener> listeners)
    {
        _rm = rm;
        _listeners = listeners;
    }

    //Create starts a profiled reload; the caller drives it with Pump and polls IsDone/ActualProgress
    public static ReloadInstance Create(ResourceManager rm, IReadOnlyList<PreparableReloadListener> listeners)
        => new ProfiledReloadInstance(rm, listeners);

    public bool IsDone => _failure is not null || _index >= _listeners.Count;

    public float ActualProgress => _listeners.Count == 0 ? 1f : Math.Min(1f, _index / (float)_listeners.Count);

    public void CheckExceptions()
    {
        if (_failure is null) return;
        throw new InvalidOperationException($"Resource reload failed at listener {_failedListener}", _failure);
    }

    public void Pump()
    {
        if (IsDone) return;
        var listener = _listeners[_index];
        var name = listener.GetType().Name;
        var ctx = new ReloadContext(name, _index, _listeners.Count);
        _index++;
        var watch = Stopwatch.StartNew();
        try
        {
            listener.Reload(_rm, ctx);
        }
        catch (Exception ex)
        {
            _failure = ex;
            _failedListener = name;
            Log.Warning($"Reload listener failed: {name}: {ex.Message}");
            return;
        }
        watch.Stop();
        var nanos = watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);
        _timings.Add(new ListenerTiming(name, nanos));
        _totalNanos += nanos;
        if (_index >= _listeners.Count) Finish();
    }

    //Finish prints the per-listener and total blocking time, maps to the tail of vanilla SimpleReloadInstance
    private void Finish()
    {
        foreach (var timing in _timings)
        {
            var milliseconds = (timing.Nanos / 1_000_000.0).ToString("F3");
            Log.Info($"{timing.Name} took {milliseconds} ms");
        }
        Log.Info($"Total blocking time: {(_totalNanos / 1_000_000.0).ToString("F3")} ms");
    }

    private sealed record ListenerTiming(string Name, long Nanos);
}
