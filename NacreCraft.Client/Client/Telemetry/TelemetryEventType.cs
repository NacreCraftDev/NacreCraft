namespace NacreCraft.Client.Telemetry;

//TelemetryEventType one event schema and its property list, maps to vanilla net.minecraft.client.telemetry.TelemetryEventType
//The codec half is omitted; local logging and the session exporter work from the property list directly
public sealed class TelemetryEventType
{
    private static readonly Dictionary<string, TelemetryEventType> Registry = new(StringComparer.Ordinal);

    private static readonly ITelemetryProperty[] GlobalProperties =
    {
        TelemetryProperty.UserId,
        TelemetryProperty.ClientId,
        TelemetryProperty.MinecraftSessionId,
        TelemetryProperty.GameVersion,
        TelemetryProperty.OperatingSystem,
        TelemetryProperty.Platform,
        TelemetryProperty.ClientModded,
        TelemetryProperty.LauncherName,
        TelemetryProperty.EventTimestampUtc,
        TelemetryProperty.OptIn
    };

    private static readonly ITelemetryProperty[] WorldSessionProperties =
    {
        TelemetryProperty.UserId,
        TelemetryProperty.ClientId,
        TelemetryProperty.MinecraftSessionId,
        TelemetryProperty.GameVersion,
        TelemetryProperty.OperatingSystem,
        TelemetryProperty.Platform,
        TelemetryProperty.ClientModded,
        TelemetryProperty.LauncherName,
        TelemetryProperty.EventTimestampUtc,
        TelemetryProperty.OptIn,
        TelemetryProperty.WorldSessionId,
        TelemetryProperty.ServerModded,
        TelemetryProperty.ServerTypeProperty,
        TelemetryProperty.ServerSessionId
    };

    public static readonly TelemetryEventType GraphicsCapabilities = Builder("graphics_capabilities", "GraphicsCapabilities")
        .DefineAll(GlobalProperties)
        .Define(TelemetryProperty.BackendName)
        .Define(TelemetryProperty.BackendFailureMessage)
        .Define(TelemetryProperty.BackendFailureReason)
        .Define(TelemetryProperty.BackendFailureMissingCapabilities)
        .Register();

    public static readonly TelemetryEventType WorldLoaded = Builder("world_loaded", "WorldLoaded")
        .DefineAll(WorldSessionProperties)
        .Define(TelemetryProperty.GameModeProperty)
        .Define(TelemetryProperty.RealmsMapContent)
        .Register();

    public static readonly TelemetryEventType PerformanceMetrics = Builder("performance_metrics", "PerformanceMetrics")
        .DefineAll(WorldSessionProperties)
        .Define(TelemetryProperty.FrameRateSamples)
        .Define(TelemetryProperty.RenderTimeSamples)
        .Define(TelemetryProperty.UsedMemorySamples)
        .Define(TelemetryProperty.NumberOfSamples)
        .Define(TelemetryProperty.RenderDistance)
        .Define(TelemetryProperty.DedicatedMemoryKb)
        .OptIn()
        .Register();

    public static readonly TelemetryEventType WorldLoadTimes = Builder("world_load_times", "WorldLoadTimes")
        .DefineAll(WorldSessionProperties)
        .Define(TelemetryProperty.WorldLoadTimeMs)
        .Define(TelemetryProperty.NewWorld)
        .OptIn()
        .Register();

    public static readonly TelemetryEventType WorldUnloaded = Builder("world_unloaded", "WorldUnloaded")
        .DefineAll(WorldSessionProperties)
        .Define(TelemetryProperty.SecondsSinceLoad)
        .Define(TelemetryProperty.TicksSinceLoad)
        .Register();

    public static readonly TelemetryEventType AdvancementMade = Builder("advancement_made", "AdvancementMade")
        .DefineAll(WorldSessionProperties)
        .Define(TelemetryProperty.AdvancementId)
        .Define(TelemetryProperty.AdvancementGameTime)
        .OptIn()
        .Register();

    public static readonly TelemetryEventType GameLoadTimes = Builder("game_load_times", "GameLoadTimes")
        .DefineAll(GlobalProperties)
        .Define(TelemetryProperty.LoadTimeTotalTimeMs)
        .Define(TelemetryProperty.LoadTimePreWindowMs)
        .Define(TelemetryProperty.LoadTimeBootstrapMs)
        .Define(TelemetryProperty.LoadTimeLoadingOverlayMs)
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

    public bool Contains(ITelemetryProperty property) => _properties.Contains(property);

    public static IReadOnlyCollection<TelemetryEventType> Values() => Registry.Values;

    //Export creates the session event and writes every declared property
    public ITelemetryEvent Export(ITelemetrySession session, TelemetryPropertyMap input)
    {
        var output = session.CreateNewEvent(ExportKey);
        foreach (var property in _properties)
            property.Export(input, output);
        return output;
    }

    private static BuilderInstance Builder(string id, string exportKey) => new(id, exportKey);

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

        public BuilderInstance DefineAll(IEnumerable<ITelemetryProperty> properties)
        {
            _properties.AddRange(properties);
            return this;
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
