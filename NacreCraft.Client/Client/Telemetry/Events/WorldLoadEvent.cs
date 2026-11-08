using NacreCraft.Game.World.Level;

namespace NacreCraft.Client.Telemetry.Events;

//WorldLoadEvent collects and sends the world-loaded event, maps to vanilla net.minecraft.client.telemetry.events.WorldLoadEvent
public sealed class WorldLoadEvent
{
    private bool _eventSent;
    private TelemetryProperty.GameMode? _gameMode;
    private string? _serverBrand;
    private readonly string? _minigameName;

    public WorldLoadEvent(string? minigameName) => _minigameName = minigameName;

    public void AddProperties(TelemetryPropertyMap.Builder properties)
    {
        if (_serverBrand is not null)
            properties.Put(TelemetryProperty.ServerModded, _serverBrand != "vanilla");
        properties.Put(TelemetryProperty.ServerTypeProperty, GetServerType());
    }

    private static TelemetryProperty.ServerType GetServerType()
        => MinecraftClient.Current?.ServerResources is not null
            ? TelemetryProperty.ServerType.Local
            : TelemetryProperty.ServerType.Other;

    public bool Send(TelemetryEventSender eventSender, bool lastChance)
    {
        if (_eventSent || _gameMode is null || (_serverBrand is null && !lastChance)) return false;
        _eventSent = true;
        eventSender.Send(TelemetryEventType.WorldLoaded, properties =>
        {
            properties.Put(TelemetryProperty.GameModeProperty, _gameMode.Value);
            if (_minigameName is not null)
                properties.Put(TelemetryProperty.RealmsMapContent, _minigameName);
        });
        return true;
    }

    public bool WasSent() => _eventSent;

    public void SetGameMode(GameType type, bool hardcore)
    {
        _gameMode = type == GameType.Survival && hardcore
            ? TelemetryProperty.GameMode.Hardcore
            : type == GameType.Survival
                ? TelemetryProperty.GameMode.Survival
                : type == GameType.Creative
                    ? TelemetryProperty.GameMode.Creative
                    : type == GameType.Adventure
                        ? TelemetryProperty.GameMode.Adventure
                        : TelemetryProperty.GameMode.Spectator;
    }

    public void SetServerBrand(string serverBrand) => _serverBrand = serverBrand;
}
