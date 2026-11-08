using NacreCraft.Client.Telemetry.Events;

namespace NacreCraft.Client.Telemetry;

//ITelemetryProperty non-generic view of a telemetry field, used by event type property lists
public interface ITelemetryProperty
{
    string Id { get; }

    string ExportKey { get; }

    void Export(TelemetryPropertyMap input, ITelemetryEvent output);
}

//TelemetryProperty one telemetry field, maps to vanilla net.minecraft.client.telemetry.TelemetryProperty
public sealed class TelemetryProperty<T>(string id, string exportKey) : ITelemetryProperty
{
    public string Id => id;

    public string ExportKey => exportKey;

    //Export writes the value into the session event, a missing value becomes an explicit null property
    public void Export(TelemetryPropertyMap input, ITelemetryEvent output)
    {
        if (!input.TryGet(this, out var value))
        {
            output.AddNullProperty(exportKey);
            return;
        }

        switch (value)
        {
            case null:
                output.AddNullProperty(exportKey);
                break;
            case string text:
                output.AddProperty(exportKey, text);
                break;
            case bool flag:
                output.AddProperty(exportKey, flag);
                break;
            case int number:
                output.AddProperty(exportKey, number);
                break;
            case long number:
                output.AddProperty(exportKey, number);
                break;
            case Guid uuid:
                output.AddProperty(exportKey, uuid.ToString());
                break;
            case DateTimeOffset instant:
                output.AddProperty(exportKey, instant.ToString("O"));
                break;
            case TelemetryProperty.GameMode mode:
                output.AddProperty(exportKey, mode switch
                {
                    TelemetryProperty.GameMode.Survival => "survival",
                    TelemetryProperty.GameMode.Creative => "creative",
                    TelemetryProperty.GameMode.Adventure => "adventure",
                    TelemetryProperty.GameMode.Spectator => "spectator",
                    _ => "hardcore"
                });
                break;
            case TelemetryProperty.ServerType type:
                output.AddProperty(exportKey, type switch
                {
                    TelemetryProperty.ServerType.Realm => "realm",
                    TelemetryProperty.ServerType.Local => "local",
                    _ => "server"
                });
                break;
            case long[] samples:
                output.AddProperty(exportKey, string.Join(";", samples));
                break;
            case GameLoadTimesEvent.Measurement measurement:
                output.AddProperty(exportKey, measurement.Millis);
                break;
            default:
                output.AddProperty(exportKey, value.ToString() ?? string.Empty);
                break;
        }
    }

    public override string ToString() => $"TelemetryProperty[{id}]";
}

//TelemetryProperty the built-in field instances and their value enums, maps to the static part of vanilla TelemetryProperty
public static class TelemetryProperty
{
    public static readonly TelemetryProperty<string> UserId = new("user_id", "userId");
    public static readonly TelemetryProperty<string> ClientId = new("client_id", "clientId");
    public static readonly TelemetryProperty<Guid> MinecraftSessionId = new("minecraft_session_id", "deviceSessionId");
    public static readonly TelemetryProperty<string> GameVersion = new("game_version", "buildDisplayName");
    public static readonly TelemetryProperty<string> OperatingSystem = new("operating_system", "buildPlatform");
    public static readonly TelemetryProperty<string> Platform = new("platform", "platform");
    public static readonly TelemetryProperty<bool> ClientModded = new("client_modded", "clientModded");
    public static readonly TelemetryProperty<string> LauncherName = new("launcher_name", "launcherName");
    public static readonly TelemetryProperty<string> BackendName = new("backend_name", "backendName");
    public static readonly TelemetryProperty<string> BackendFailureMessage = new("backend_failure_message", "backendFailureMessage");
    public static readonly TelemetryProperty<string> BackendFailureReason = new("backend_failure_reason", "backendFailureReason");
    public static readonly TelemetryProperty<string> BackendFailureMissingCapabilities = new("backend_failure_missing_capabilities", "backendFailureMissingCapabilities");
    public static readonly TelemetryProperty<Guid> WorldSessionId = new("world_session_id", "worldSessionId");
    public static readonly TelemetryProperty<Guid> ServerSessionId = new("server_session_id", "GlobalMultiplayerCorrelationId");
    public static readonly TelemetryProperty<bool> ServerModded = new("server_modded", "serverModded");
    public static readonly TelemetryProperty<ServerType> ServerTypeProperty = new("server_type", "serverType");
    public static readonly TelemetryProperty<bool> OptIn = new("opt_in", "isOptional");
    public static readonly TelemetryProperty<DateTimeOffset> EventTimestampUtc = new("event_timestamp_utc", "eventTimestampUtc");
    public static readonly TelemetryProperty<GameMode> GameModeProperty = new("game_mode", "playerGameMode");
    public static readonly TelemetryProperty<string> RealmsMapContent = new("realms_map_content", "realmsMapContent");
    public static readonly TelemetryProperty<int> SecondsSinceLoad = new("seconds_since_load", "secondsSinceLoad");
    public static readonly TelemetryProperty<int> TicksSinceLoad = new("ticks_since_load", "ticksSinceLoad");
    public static readonly TelemetryProperty<long[]> FrameRateSamples = new("frame_rate_samples", "serializedFpsSamples");
    public static readonly TelemetryProperty<long[]> RenderTimeSamples = new("render_time_samples", "serializedRenderTimeSamples");
    public static readonly TelemetryProperty<long[]> UsedMemorySamples = new("used_memory_samples", "serializedUsedMemoryKbSamples");
    public static readonly TelemetryProperty<int> NumberOfSamples = new("number_of_samples", "numSamples");
    public static readonly TelemetryProperty<int> RenderDistance = new("render_distance", "renderDistance");
    public static readonly TelemetryProperty<int> DedicatedMemoryKb = new("dedicated_memory_kb", "dedicatedMemoryKb");
    public static readonly TelemetryProperty<int> WorldLoadTimeMs = new("world_load_time_ms", "worldLoadTimeMs");
    public static readonly TelemetryProperty<bool> NewWorld = new("new_world", "newWorld");
    public static readonly TelemetryProperty<string> AdvancementId = new("advancement_id", "advancementId");
    public static readonly TelemetryProperty<long> AdvancementGameTime = new("advancement_game_time", "advancementGameTime");

    public static readonly TelemetryProperty<GameLoadTimesEvent.Measurement> LoadTimeTotalTimeMs =
        new("load_time_total_time_ms", "loadTimeTotalTimeMs");
    public static readonly TelemetryProperty<GameLoadTimesEvent.Measurement> LoadTimePreWindowMs =
        new("load_time_pre_window_ms", "loadTimePreWindowMs");
    public static readonly TelemetryProperty<GameLoadTimesEvent.Measurement> LoadTimeBootstrapMs =
        new("load_time_bootstrap_ms", "loadTimeBootstrapMs");
    public static readonly TelemetryProperty<GameLoadTimesEvent.Measurement> LoadTimeLoadingOverlayMs =
        new("load_time_loading_overlay_ms", "loadTimeLoadingOverlayMs");

    public enum GameMode
    {
        Survival,
        Creative,
        Adventure,
        Spectator,
        Hardcore
    }

    public enum ServerType
    {
        Realm,
        Local,
        Other
    }
}
