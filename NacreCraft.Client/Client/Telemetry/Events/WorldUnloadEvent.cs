namespace NacreCraft.Client.Telemetry.Events;

//WorldUnloadEvent tracks world session time and sends it on unload, maps to vanilla net.minecraft.client.telemetry.events.WorldUnloadEvent
public sealed class WorldUnloadEvent
{
    private DateTimeOffset? _worldLoadedTime;
    private long _totalTicks;
    private long _lastGameTime;

    public void OnPlayerInfoReceived()
    {
        _lastGameTime = -1;
        _worldLoadedTime ??= DateTimeOffset.UtcNow;
    }

    public void SetTime(long gameTime)
    {
        if (_lastGameTime != -1)
            _totalTicks += Math.Max(0, gameTime - _lastGameTime);
        _lastGameTime = gameTime;
    }

    public void Send(TelemetryEventSender eventSender)
    {
        if (_worldLoadedTime is not { } loadedTime) return;
        eventSender.Send(TelemetryEventType.WorldUnloaded, properties =>
        {
            properties.Put(TelemetryProperty.SecondsSinceLoad,
                (int)(DateTimeOffset.UtcNow - loadedTime).TotalSeconds);
            properties.Put(TelemetryProperty.TicksSinceLoad, (int)_totalTicks);
        });
    }
}
