using System.Diagnostics;
using NacreCraft.Logging;

namespace NacreCraft.Client.Telemetry.Events;

//GameLoadTimesEvent measures the startup stages and sends them as one event, maps to vanilla net.minecraft.client.telemetry.events.GameLoadTimesEvent
public sealed class GameLoadTimesEvent
{
    public static readonly GameLoadTimesEvent Instance = new();

    private readonly Dictionary<TelemetryProperty<Measurement>, Stopwatch> _measurements = new();
    private long? _bootstrapTime;

    private GameLoadTimesEvent()
    {
    }

    public void BeginStep(TelemetryProperty<Measurement> property)
    {
        lock (_measurements) _measurements.TryAdd(property, Stopwatch.StartNew());
    }

    public void EndStep(TelemetryProperty<Measurement> property)
    {
        lock (_measurements)
        {
            if (!_measurements.TryGetValue(property, out var measurement))
            {
                Log.Warning($"Attempted to end step for {property.Id} before starting it");
                return;
            }
            if (measurement.IsRunning) measurement.Stop();
        }
    }

    public void Send(TelemetryEventSender eventSender)
    {
        eventSender.Send(TelemetryEventType.GameLoadTimes, properties =>
        {
            lock (_measurements)
            {
                foreach (var (property, measurement) in _measurements)
                {
                    if (!measurement.IsRunning)
                        properties.Put(property, new Measurement((int)measurement.ElapsedMilliseconds));
                    else
                        Log.Warning($"Measurement {property.Id} was discarded since it was still ongoing when the event {TelemetryEventType.GameLoadTimes.Id} was sent.");
                }
                if (_bootstrapTime is { } bootstrap)
                    properties.Put(TelemetryProperty.LoadTimeBootstrapMs, new Measurement((int)bootstrap));
                _measurements.Clear();
            }
        });
    }

    public void SetBootstrapTime(long duration)
    {
        lock (_measurements) _bootstrapTime = duration;
    }

    public sealed record Measurement(int Millis);
}
