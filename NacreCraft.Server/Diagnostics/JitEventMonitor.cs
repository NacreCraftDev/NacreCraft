using System.Diagnostics.Tracing;
using NacreCraft.Logging;

namespace NacreCraft.Server.Diagnostics;

//JitEventMonitor subscribes to this process's JIT events to mark points on the memory graph
//Uses EventListener instead of EventPipe, runtime event sources are already handled there and there is no need to add a parsing layer
//Tier is determined by ReJITID in MethodLoadVerbose: 0 is the first compile, 1 is Tier 1, anything higher is a further optimization of the previous tier
public static class JitEventMonitor
{
    //JitKeyword and NGenKeyword, JIT method load events are emitted under these two groups
    private const EventKeywords Keywords = (EventKeywords)(0x10 | 0x20);

    //WhitelistEnabled whether to only count this project's compile events, on by default
    //Set NACRECRAFT_JIT_WHITELIST=0 to count all methods, useful for investigating recompilation in third-party libraries
    private static readonly bool WhitelistEnabled = ReadFlag("NACRECRAFT_JIT_WHITELIST");

    //LogEnabled whether the per-method lines are written at all
    //Set NACRECRAFT_JIT_LOG=0 to drop them; the counters keep running either way, only the text is skipped
    private static readonly bool LogEnabled = ReadFlag("NACRECRAFT_JIT_LOG");

    //ReadFlag treats an absent or unrecognised value as on, so only an explicit off turns a switch off
    private static bool ReadFlag(string name)
        => Environment.GetEnvironmentVariable(name) is not ("0" or "false" or "False" or "FALSE");

    //LogOn the whole condition in one read, so a disabled run allocates nothing for the line
    private static bool LogOn => LogEnabled && Log.DebugEnabled;

    private static readonly Listener Hook = new();
    private static int _pendingTier1;
    private static int _pendingTier2;
    private static long _total;
    private static long _totalTier2;
    private static long _totalEvents;

    //Total cumulative Tier 1 recompile count
    public static long Total => Interlocked.Read(ref _total);

    //TotalEvents cumulative method load events received, used to confirm the subscription chain works
    public static long TotalEvents => Interlocked.Read(ref _totalEvents);

    //Start triggers the subscription, it is already attached during static field initialization, this only fills in the entry point to mirror the GC side
    public static void Start() => _ = Hook;

    //TakePending takes the Tier1 and Tier2+ counts accumulated between two samples and resets them
    public static (int Tier1, int Tier2) TakePending()
        => (Interlocked.Exchange(ref _pendingTier1, 0), Interlocked.Exchange(ref _pendingTier2, 0));

    //IsTracked namespace whitelist, only counts this project's compile events
    //Runtime and third-party recompilation is high volume and unrelated to nc, mixing it in would smear the baseline dots together
    //Once disabled by NACRECRAFT_JIT_WHITELIST=0 everything passes
    public static bool IsTracked(string? methodNamespace)
        => !WhitelistEnabled
            || (methodNamespace is not null && methodNamespace.StartsWith("NacreCraft", StringComparison.Ordinal));

    //Listener event callback, all state lives in static fields
    //The EventListener base constructor already calls back into OnEventSourceCreated, when instance fields are not initialized yet
    private sealed class Listener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "Microsoft-Windows-DotNETRuntime")
                EnableEvents(source, EventLevel.Verbose, Keywords);
        }

        protected override void OnEventWritten(EventWrittenEventArgs e)
        {
            //MethodLoadVerbose means the compile has produced code, ReJITID is only available once loading completes
            if (e.EventName is null || !e.EventName.StartsWith("MethodLoadVerbose", StringComparison.Ordinal))
                return;
            Interlocked.Increment(ref _totalEvents);
            var ns = Payload(e, "MethodNamespace") as string;
            if (!IsTracked(ns)) return;
            var reJitId = Payload(e, "ReJITID") is ulong value ? value : 0;
            if (reJitId == 0) return;
            if (reJitId == 1)
            {
                Interlocked.Increment(ref _pendingTier1);
                var total = Interlocked.Increment(ref _total);
                if (LogOn) Log.Debug($"[JIT] Tier1 {Name(ns, e)} #{total}");
                return;
            }
            //Tier 2 and above, a further optimization of the previous tier's result, counted and colored separately
            Interlocked.Increment(ref _pendingTier2);
            var totalTier2 = Interlocked.Increment(ref _totalTier2);
            if (LogOn) Log.Debug($"[JIT] Tier{reJitId} recompiled {Name(ns, e)} #{totalTier2}");
        }

        //Name builds the display name only when a line is actually written
        private static string Name(string? ns, EventWrittenEventArgs e)
            => $"{ns}.{Payload(e, "MethodName")}";

        //Payload fetches the raw value by field name, returns null for a missing field
        private static object? Payload(EventWrittenEventArgs e, string field)
        {
            var names = e.PayloadNames;
            var payload = e.Payload;
            if (names is null || payload is null) return null;
            for (var i = 0; i < names.Count && i < payload.Count; i++)
                if (names[i] == field) return payload[i];
            return null;
        }
    }
}
