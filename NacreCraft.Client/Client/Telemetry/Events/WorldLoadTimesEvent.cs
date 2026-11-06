using NacreCraft.Client.Telemetry;

namespace NacreCraft.Client.Telemetry.Events;

//WorldLoadTimesEvent sends how long loading the world took, maps to vanilla net.minecraft.client.telemetry.events.WorldLoadTimesEvent
public sealed class WorldLoadTimesEvent
{
    private readonly bool _newWorld;
    private readonly TimeSpan? _worldLoadDuration;

    public WorldLoadTimesEvent(bool newWorld, TimeSpan? worldLoadDuration)
    {
        _newWorld = newWorld;
        _worldLoadDuration = worldLoadDuration;
    }

    public void Send(TelemetryEventSender eventSender)
    {
        if (_worldLoadDuration is not { } duration) return;
        eventSender.Send(TelemetryEventType.WorldLoadTimes, properties =>
        {
            properties.Put(TelemetryProperty.WorldLoadTimeMs, (int)duration.TotalMilliseconds);
            properties.Put(TelemetryProperty.NewWorld, _newWorld);
        });
    }
}
