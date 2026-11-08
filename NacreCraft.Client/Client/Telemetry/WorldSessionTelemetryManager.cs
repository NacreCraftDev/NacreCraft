using NacreCraft.Client.Telemetry.Events;
using NacreCraft.Game.World.Level;

namespace NacreCraft.Client.Telemetry;

//WorldSessionTelemetryManager the per-world telemetry session, maps to vanilla net.minecraft.client.telemetry.WorldSessionTelemetryManager
public sealed class WorldSessionTelemetryManager
{
    private readonly Guid _worldSessionId = Guid.NewGuid();
    private readonly TelemetryEventSender _eventSender;
    private readonly WorldLoadEvent _worldLoadEvent;
    private readonly WorldUnloadEvent _worldUnloadEvent = new();
    private readonly PerformanceMetricsEvent _performanceMetricsEvent = new();
    private readonly WorldLoadTimesEvent _worldLoadTimesEvent;

    public WorldSessionTelemetryManager(TelemetryEventSender eventSender, bool newWorld,
        TimeSpan? worldLoadDuration, string? minigameName, Guid sessionId)
    {
        _worldLoadEvent = new WorldLoadEvent(minigameName);
        _worldLoadTimesEvent = new WorldLoadTimesEvent(newWorld, worldLoadDuration);
        _eventSender = eventSender.Decorate(properties =>
        {
            _worldLoadEvent.AddProperties(properties);
            properties.Put(TelemetryProperty.WorldSessionId, _worldSessionId);
            properties.Put(TelemetryProperty.ServerSessionId, sessionId);
        });
    }

    public void Tick() => _performanceMetricsEvent.Tick(_eventSender);

    public void OnPlayerInfoReceived(GameType type, bool hardcore)
    {
        _worldLoadEvent.SetGameMode(type, hardcore);
        _worldUnloadEvent.OnPlayerInfoReceived();
        WorldSessionStart();
    }

    public void OnServerBrandReceived(string serverBrand)
    {
        _worldLoadEvent.SetServerBrand(serverBrand);
        WorldSessionStart();
    }

    public void SetTime(long gameTime) => _worldUnloadEvent.SetTime(gameTime);

    public void WorldSessionStart()
    {
        if (!_worldLoadEvent.Send(_eventSender, false)) return;
        _worldLoadTimesEvent.Send(_eventSender);
        _performanceMetricsEvent.Start();
    }

    public void OnDisconnect()
    {
        _worldLoadEvent.Send(_eventSender, true);
        _performanceMetricsEvent.Stop();
        if (_worldLoadEvent.WasSent()) _worldUnloadEvent.Send(_eventSender);
    }

    public void OnAdvancementDone(long gameTime, NacreCraft.Registry.Identifier advancementId, bool sendsTelemetryEvent)
    {
        if (!sendsTelemetryEvent || advancementId.Namespace != "minecraft") return;
        _eventSender.Send(TelemetryEventType.AdvancementMade, properties =>
        {
            properties.Put(TelemetryProperty.AdvancementId, advancementId.ToString());
            properties.Put(TelemetryProperty.AdvancementGameTime, gameTime);
        });
    }
}
