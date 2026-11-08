using System.Text.Json;
using NacreCraft.Logging;
using Logger = NacreCraft.Logging.Log;

namespace NacreCraft.Client.Telemetry;

//TelemetryEventLog writes one JSON line per event, maps to vanilla net.minecraft.client.telemetry.TelemetryEventLog
public sealed class TelemetryEventLog : TelemetryEventLogger, IDisposable
{
    private readonly object _lock = new();
    private readonly StreamWriter _writer;
    private bool _closed;

    public TelemetryEventLog(string path)
    {
        _writer = new StreamWriter(path, append: true) { AutoFlush = true };
    }

    public void Log(TelemetryEventInstance instance)
    {
        lock (_lock)
        {
            if (_closed) return;
            try
            {
                _writer.WriteLine(instance.ToJson().ToJsonString(new JsonSerializerOptions()));
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException)
            {
                Logger.Error($"Failed to write telemetry event to log {e}");
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_closed) return;
            _closed = true;
            _writer.Dispose();
        }
    }
}
