using System.Diagnostics;
using NacreCraft.Logging;

namespace NacreCraft.Resources;

//SimpleReloadInstance, a stepped reload scheduler, a trimmed-down version of the vanilla class of the same name
//Walks listeners in registration order, one Pump per listener, and reports progress from how many of them finished
//Vanilla uses chained CompletableFuture scheduling plus weighted counters; NC keeps every listener on the caller's thread
public sealed class SimpleReloadInstance : ReloadInstance
{
    private readonly ResourceManager _rm;
    private readonly IReadOnlyList<PreparableReloadListener> _listeners;
    private int _index;
    private Exception? _failure;
    private string? _failedListener;

    private SimpleReloadInstance(ResourceManager rm, IReadOnlyList<PreparableReloadListener> listeners)
    {
        _rm = rm;
        _listeners = listeners;
    }

    //Create starts a reload; the caller drives it with Pump and polls IsDone/ActualProgress between steps
    public static ReloadInstance Create(ResourceManager rm, IReadOnlyList<PreparableReloadListener> listeners)
        => new SimpleReloadInstance(rm, listeners);

    //Run is the blocking form used at startup, equivalent to Create followed by pumping until done
    //A failing listener aborts immediately, later listeners do not run, and the failure is rethrown here
    public static void Run(ResourceManager rm, IReadOnlyList<PreparableReloadListener> listeners)
    {
        var instance = Create(rm, listeners);
        while (!instance.IsDone) instance.Pump();
        instance.CheckExceptions();
    }

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
        var ctx = new ReloadContext(listener.GetType().Name, _index, _listeners.Count);
        _index++;
        //Per-listener timing, so the reload budget can be attributed without a profiler
        var watch = Stopwatch.StartNew();
        try
        {
            listener.Reload(_rm, ctx);
            Log.Info($"[reload] {ctx.Name} {watch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            //Recorded rather than thrown so the caller can poll IsDone and surface the failure where it has context
            _failure = ex;
            _failedListener = listener.GetType().Name;
            Log.Warning($"Reload listener failed: {_failedListener}: {ex.Message}");
        }
    }
}
