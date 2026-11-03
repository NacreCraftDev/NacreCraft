using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;

namespace NacreCraft.Util.Tracing;

//ClientTraceEventSource emits client phase events into a runtime trace capture
//It lives in the shared utilities because the network layer needs to mark its receive and send work too
//EventPipe only collects it while a capture subscribes the provider, so the calls stay cheap otherwise
[EventSource(Name = "NacreCraft-Client")]
public sealed class ClientTraceEventSource : EventSource
{
    public static readonly ClientTraceEventSource Log = new();

    private int _nextId;

    public int NextId() => Interlocked.Increment(ref _nextId);

    [Event(1, Level = EventLevel.Verbose)]
    public void PhaseBegin(string category, string name, int phaseId)
    {
        if (IsEnabled()) WriteEvent(1, category, name, phaseId);
    }

    [Event(2, Level = EventLevel.Verbose)]
    public void PhaseEnd(string category, int phaseId)
    {
        if (IsEnabled()) WriteEvent(2, category, phaseId);
    }
}

//ClientTrace phase scopes for a client runtime trace
public static class ClientTrace
{
    //Provider the EventPipe provider entry a capture needs to collect ClientTraceEventSource
    public static EventPipeProvider Provider { get; } =
        new(ClientTraceEventSource.Log.Name, EventLevel.Verbose, 0, null);

    //BeginScope marks the start of a phase, returns a scope that is a no-op unless a capture is listening
    public static Scope BeginScope(string category, string name)
        => ClientTraceEventSource.Log.IsEnabled() ? new Scope(category, name) : default;

    public readonly struct Scope : IDisposable
    {
        private readonly string? _category;
        private readonly int _phaseId;

        internal Scope(string category, string name)
        {
            _category = category;
            _phaseId = ClientTraceEventSource.Log.NextId();
            ClientTraceEventSource.Log.PhaseBegin(category, name, _phaseId);
        }

        public void Dispose()
        {
            if (_category is not null) ClientTraceEventSource.Log.PhaseEnd(_category, _phaseId);
        }
    }
}
