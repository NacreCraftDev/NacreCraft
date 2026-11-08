using System.Runtime.InteropServices;
using NacreCraft.Client.Telemetry.Events;
using NacreCraft.Config;

namespace NacreCraft.Client.Telemetry;

//ClientTelemetryManager the device-level telemetry session, maps to vanilla net.minecraft.client.telemetry.ClientTelemetryManager
//The default build compiles everything but TelemetryBuild.Enabled is a const false, so the sender is always disabled
public sealed class ClientTelemetryManager : IDisposable
{
    //SessionFactory installed by the account layer when a real telemetry session is available, null keeps telemetry offline
    public static Func<ITelemetrySession?>? SessionFactory { get; set; }

    private readonly Func<bool> _allowsTelemetry;
    private readonly Func<bool> _telemetryOptInExtra;
    private readonly TelemetryPropertyMap _deviceSessionProperties;
    private readonly string _logDirectory;
    private readonly Lazy<TelemetryEventSender> _outsideSessionSender;
    private TelemetryLogManager? _logManager;

    public ClientTelemetryManager(string gameDirectory, Func<bool> allowsTelemetry, Func<bool> telemetryOptInExtra)
    {
        _allowsTelemetry = allowsTelemetry;
        _telemetryOptInExtra = telemetryOptInExtra;
        _deviceSessionProperties = BuildDeviceProperties();
        _logDirectory = Path.Combine(gameDirectory, "logs", "telemetry");
        _outsideSessionSender = new Lazy<TelemetryEventSender>(CreateEventSender);
    }

    public string LogDirectory => _logDirectory;

    public TelemetryEventSender OutsideSessionSender => _outsideSessionSender.Value;

    public WorldSessionTelemetryManager CreateWorldSessionManager(bool newWorld, TimeSpan? worldLoadDuration,
        string? minigameName, Guid sessionId)
        => new(CreateEventSender(), newWorld, worldLoadDuration, minigameName, sessionId);

    private TelemetryEventSender CreateEventSender()
    {
        if (!TelemetryBuild.Enabled || !_allowsTelemetry()) return TelemetryEventSender.Disabled;

        var session = SessionFactory?.Invoke();
        if (session is null || !session.IsEnabled) return TelemetryEventSender.Disabled;

        _logManager ??= TelemetryLogManager.Open(_logDirectory);
        var logger = _logManager.OpenLogger();
        return new Sender(this, session, logger);
    }

    private TelemetryPropertyMap BuildDeviceProperties()
    {
        var builder = new TelemetryPropertyMap.Builder()
            .Put(TelemetryProperty.MinecraftSessionId, Guid.NewGuid())
            .Put(TelemetryProperty.GameVersion, SharedConstants.Version)
            .Put(TelemetryProperty.OperatingSystem, RuntimeInformation.OSDescription)
            .Put(TelemetryProperty.Platform, Environment.OSVersion.VersionString)
            .Put(TelemetryProperty.ClientModded, false);
        return builder.Build();
    }

    public void Dispose() => _logManager?.Dispose();

    private sealed class Sender(ClientTelemetryManager owner, ITelemetrySession session,
        TelemetryEventLogger? logger) : TelemetryEventSender
    {
        public void Send(TelemetryEventType type, Action<TelemetryPropertyMap.Builder> properties)
        {
            if (type.IsOptIn && !owner._telemetryOptInExtra()) return;

            var builder = new TelemetryPropertyMap.Builder()
                .PutAll(owner._deviceSessionProperties)
                .Put(TelemetryProperty.EventTimestampUtc, DateTimeOffset.UtcNow)
                .Put(TelemetryProperty.OptIn, type.IsOptIn);
            properties(builder);

            var instance = new TelemetryEventInstance(type, builder.Build());
            logger?.Log(instance);
            instance.Export(session).Send();
        }

        public TelemetryEventSender Decorate(Action<TelemetryPropertyMap.Builder> decorator)
            => new DecoratedSender(this, decorator);
    }

    private sealed class DecoratedSender(TelemetryEventSender inner, Action<TelemetryPropertyMap.Builder> decorator)
        : TelemetryEventSender
    {
        public void Send(TelemetryEventType type, Action<TelemetryPropertyMap.Builder> properties)
            => inner.Send(type, builder =>
            {
                properties(builder);
                decorator(builder);
            });

        public TelemetryEventSender Decorate(Action<TelemetryPropertyMap.Builder> more)
            => new DecoratedSender(this, more);
    }
}
