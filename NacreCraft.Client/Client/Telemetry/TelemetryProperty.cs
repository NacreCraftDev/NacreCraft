namespace NacreCraft.Client.Telemetry;

//ITelemetryProperty non-generic view of a telemetry field, used by event type property lists
public interface ITelemetryProperty
{
    string Id { get; }

    string ExportKey { get; }
}

//TelemetryProperty one telemetry field, maps to vanilla net.minecraft.client.telemetry.TelemetryProperty
public sealed class TelemetryProperty<T>(string id, string exportKey) : ITelemetryProperty
{
    public string Id => id;

    public string ExportKey => exportKey;

    public override string ToString() => $"TelemetryProperty[{id}]";
}

//TelemetryProperty the built-in field instances and their value enums, maps to the static part of vanilla TelemetryProperty
public static class TelemetryProperty
{
    public static readonly TelemetryProperty<bool> ServerModded = new("server_modded", "serverModded");
    //C# cannot give a field and a nested type the same name, so the field carries the Property suffix
    public static readonly TelemetryProperty<GameMode> GameModeProperty = new("game_mode", "playerGameMode");
    public static readonly TelemetryProperty<ServerType> ServerTypeProperty = new("server_type", "serverType");
    public static readonly TelemetryProperty<string> RealmsMapContent = new("realms_map_content", "realmsMapContent");
    public static readonly TelemetryProperty<int> SecondsSinceLoad = new("seconds_since_load", "secondsSinceLoad");
    public static readonly TelemetryProperty<int> TicksSinceLoad = new("ticks_since_load", "ticksSinceLoad");
    public static readonly TelemetryProperty<int> WorldLoadTimeMs = new("world_load_time_ms", "worldLoadTimeMs");
    public static readonly TelemetryProperty<bool> NewWorld = new("new_world", "newWorld");
    public static readonly TelemetryProperty<int> NumberOfSamples = new("number_of_samples", "numSamples");
    public static readonly TelemetryProperty<int> RenderDistance = new("render_distance", "renderDistance");
    public static readonly TelemetryProperty<int> DedicatedMemoryKb = new("dedicated_memory_kb", "dedicatedMemoryKb");
    public static readonly TelemetryProperty<long[]> FrameRateSamples = new("frame_rate_samples", "serializedFpsSamples");
    public static readonly TelemetryProperty<long[]> RenderTimeSamples = new("render_time_samples", "serializedRenderTimeSamples");
    public static readonly TelemetryProperty<long[]> UsedMemorySamples = new("used_memory_samples", "serializedUsedMemoryKbSamples");

    public static readonly TelemetryProperty<Events.GameLoadTimesEvent.Measurement> LoadTimeTotalTimeMs =
        new("load_time_total_time_ms", "loadTimeTotalTimeMs");
    public static readonly TelemetryProperty<Events.GameLoadTimesEvent.Measurement> LoadTimePreWindowMs =
        new("load_time_pre_window_ms", "loadTimePreWindowMs");
    public static readonly TelemetryProperty<Events.GameLoadTimesEvent.Measurement> LoadTimeBootstrapMs =
        new("load_time_bootstrap_ms", "loadTimeBootstrapMs");
    public static readonly TelemetryProperty<Events.GameLoadTimesEvent.Measurement> LoadTimeLoadingOverlayMs =
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
