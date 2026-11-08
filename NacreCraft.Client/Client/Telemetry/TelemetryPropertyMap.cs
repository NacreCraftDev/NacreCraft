namespace NacreCraft.Client.Telemetry;

//TelemetryPropertyMap the property values of one event, maps to vanilla net.minecraft.client.telemetry.TelemetryPropertyMap
public sealed class TelemetryPropertyMap
{
    private readonly Dictionary<ITelemetryProperty, object> _entries;

    private TelemetryPropertyMap(Dictionary<ITelemetryProperty, object> entries) => _entries = entries;

    public IReadOnlyCollection<ITelemetryProperty> PropertySet() => _entries.Keys;

    //GetRaw reads a property without knowing its closed generic type, used by the local log
    internal object? GetRaw(ITelemetryProperty property)
        => _entries.TryGetValue(property, out var value) ? value : null;

    public T? Get<T>(TelemetryProperty<T> property)
        => _entries.TryGetValue(property, out var value) ? (T)value : default;

    public bool TryGet<T>(TelemetryProperty<T> property, out T value)
    {
        if (_entries.TryGetValue(property, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }

    //Builder collects the properties of one event before it is sent
    public sealed class Builder
    {
        private readonly Dictionary<ITelemetryProperty, object> _entries = new();

        public Builder Put<T>(TelemetryProperty<T> property, T value)
        {
            _entries[property] = value!;
            return this;
        }

        public Builder PutIfNotNull<T>(TelemetryProperty<T> property, T? value)
        {
            if (value is not null) _entries[property] = value;
            return this;
        }

        public Builder PutAll(TelemetryPropertyMap properties)
        {
            foreach (var (key, value) in properties._entries) _entries[key] = value;
            return this;
        }

        public TelemetryPropertyMap Build() => new(new Dictionary<ITelemetryProperty, object>(_entries));
    }
}
