using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using NacreCraft.Logging;

namespace NacreCraft.Server.Diagnostics;

//GcEventMonitor subscribes to this process's runtime GC events to mark points on the memory graph baseline
//Goes through the EventPipe Microsoft-Windows-DotNETRuntime provider with only GCKeyword enabled
//CoreCLR's NativeRuntimeEventSource is an empty implementation outside NativeAOT, so EventListener cannot receive these native events
public static class GcEventMonitor
{
    private const string ProviderName = "Microsoft-Windows-DotNETRuntime";
    //GCKeyword, the runtime GC event group
    private const long GcKeyword = 0x1;
    //GC events are very low volume, a small buffer is enough and a larger one just wastes memory
    private const int CircularBufferMb = 16;

    private static readonly Lock Sync = new();
    private static EventPipeSession? _session;
    private static bool _started;
    //_pending GC count since the last sample, the GUI takes it every 500ms
    private static int _pending;
    private static long _total;

    //Total cumulative GC count
    public static long Total => Interlocked.Read(ref _total);

    //LogEnabled whether the per-collection lines are written at all
    //Set NACRECRAFT_GC_LOG=0 to drop them; the counters keep running either way, only the text is skipped
    private static readonly bool LogEnabled =
        Environment.GetEnvironmentVariable("NACRECRAFT_GC_LOG") is not ("0" or "false" or "False" or "FALSE");

    //Start starts the subscription, effective only once
    //If it fails, degrade to "no red dots", the listener is a debug feature and should not take the server down with it
    public static void Start()
    {
        lock (Sync)
        {
            if (_started) return;
            _started = true;
            try
            {
                var providers = new[]
                {
                    new EventPipeProvider(ProviderName, EventLevel.Informational, GcKeyword, null),
                };
                _session = new DiagnosticsClient(Environment.ProcessId)
                    .StartEventPipeSession(providers, requestRundown: false, circularBufferMB: CircularBufferMb);
                new Thread(Pump) { Name = "NacreCraft-GcEvents", IsBackground = true }
                    .Start(_session.EventStream);
            }
            catch (Exception e)
            {
                Log.Warning($"GC event subscription failed, the memory graph baseline will have no red dot: {e.Message}");
            }
        }
    }

    //Stop ends the subscription, idempotent
    public static void Stop()
    {
        lock (Sync)
        {
            if (!_started) return;
            _started = false;
            try { _session?.Stop(); }
            catch { }
            _session = null;
        }
    }

    //TakePending takes the GC count accumulated between two samples and resets it
    public static int TakePending() => Interlocked.Exchange(ref _pending, 0);

    //Pump reads the event stream on a background thread, independent of the caller thread
    private static void Pump(object? state)
    {
        var source = new EventPipeEventSource((Stream)state!);
        source.Clr.GCStart += OnGcStart;
        try
        {
            //Process reads until the session closes, events are handled in callbacks along the way
            source.Process();
        }
        catch (Exception e)
        {
            Log.Warning($"GC event stream broken, the memory graph baseline stops updating: {e.Message}");
        }
    }

    //OnGcStart, a GC starts
    //Depth is the generation, 0/1/2 are Gen0/Gen1/Gen2
    private static void OnGcStart(GCStartTraceData data)
    {
        Interlocked.Increment(ref _pending);
        var total = Interlocked.Increment(ref _total);
        if (LogEnabled && Log.DebugEnabled)
            Log.Debug($"[GC] Gen{data.Depth} {data.Reason} #{total}");
    }
}
