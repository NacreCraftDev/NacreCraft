namespace NacreCraft.Client.Telemetry;

//TelemetryEventType one event schema and its property list, maps to vanilla net.minecraft.client.telemetry.TelemetryEventType
//The vanilla codec/export half is omitted until an authlib-equivalent telemetry session exists
public sealed class TelemetryEventType
{
    private static readonly Dictionary<string, TelemetryEventType> Registry = new(StringComparer.Ordinal);

    public static readonly TelemetryEventType GameLoadTimes = Builder("game_load_times", "GameLoadTimes")
        .Define(TelemetryProperty.LoadTimeTotalTimeMs)
        .Define(TelemetryProperty.LoadTimePreWindowMs)
        .Define(TelemetryProperty.LoadTimeBootstrapMs)
        .Define(TelemetryProperty.LoadTimeLoadingOverlayMs)
        .OptIn()
        .Register();

    public static readonly TelemetryEventType WorldLoaded = Builder("world_loaded", "WorldLoaded")
        .Define(TelemetryProperty.GameModeProperty)
        .Define(TelemetryProperty.RealmsMapContent)
        .Define(TelemetryProperty.ServerModded)
        .Define(TelemetryProperty.ServerTypeProperty)
        .Register();

    public static readonly TelemetryEventType WorldLoadTimes = Builder("world_load_times", "WorldLoadTimes")
        .Define(TelemetryProperty.WorldLoadTimeMs)
        .Define(TelemetryProperty.NewWorld)
        .OptIn()
        .Register();

    public static readonly TelemetryEventType WorldUnloaded = Builder("world_unloaded", "WorldUnloaded")
        .Define(TelemetryProperty.SecondsSinceLoad)
        .Define(TelemetryProperty.TicksSinceLoad)
        .Register();

    public static readonly TelemetryEventType PerformanceMetrics = Builder("performance_metrics", "PerformanceMetrics")
        .Define(TelemetryProperty.FrameRateSamples)
        .Define(TelemetryProperty.RenderTimeSamples)
        .Define(TelemetryProperty.UsedMemorySamples)
        .Define(TelemetryProperty.NumberOfSamples)
        .Define(TelemetryProperty.RenderDistance)
        .Define(TelemetryProperty.DedicatedMemoryKb)
        .OptIn()
        .Register();

    private readonly List<ITelemetryProperty> _properties = new();

    private TelemetryEventType(string id, string exportKey, bool isOptIn)
    {
        Id = id;
        ExportKey = exportKey;
        IsOptIn = isOptIn;
    }

    public string Id { get; }

    public string ExportKey { get; }

    public bool IsOptIn { get; }

    public IReadOnlyList<ITelemetryProperty> Properties => _properties;

    public bool Contains<T>(TelemetryProperty<T> property) => _properties.Contains(property);

    public static IReadOnlyCollection<TelemetryEventType> Values() => Registry.Values;

    private static BuilderInstance Builder(string id, string exportKey) => new(id, exportKey);

    //BuilderInstance collects the schema before registration
    private sealed class BuilderInstance
    {
        private readonly string _id;
        private readonly string _exportKey;
        private readonly List<ITelemetryProperty> _properties = new();
        private bool _isOptIn;

        public BuilderInstance(string id, string exportKey)
        {
            _id = id;
            _exportKey = exportKey;
        }

        public BuilderInstance Define<T>(TelemetryProperty<T> property)
        {
            _properties.Add(property);
            return this;
        }

        public BuilderInstance OptIn()
        {
            _isOptIn = true;
            return this;
        }

        public TelemetryEventType Register()
        {
            var type = new TelemetryEventType(_id, _exportKey, _isOptIn);
            type._properties.AddRange(_properties);
            if (!Registry.TryAdd(_id, type))
                throw new InvalidOperationException($"Duplicate TelemetryEventType with key: '{_id}'");
            return type;
        }
    }
}
