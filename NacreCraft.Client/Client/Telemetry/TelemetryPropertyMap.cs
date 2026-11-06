namespace NacreCraft.Client.Telemetry;

//TelemetryPropertyMap the property values of one event, maps to vanilla net.minecraft.client.telemetry.TelemetryPropertyMap
public sealed class TelemetryPropertyMap
{
    private readonly Dictionary<ITelemetryProperty, object> _values;

    private TelemetryPropertyMap(Dictionary<ITelemetryProperty, object> values) => _values = values;

    public T? Get<T>(TelemetryProperty<T> property)
        => _values.TryGetValue(property, out var value) ? (T)value : default;

    //Builder collects the properties of one event before it is sent
    public sealed class Builder
    {
        private readonly Dictionary<ITelemetryProperty, object> _values = new();

        public void Put<T>(TelemetryProperty<T> property, T value) => _values[property] = value!;

        public TelemetryPropertyMap Build() => new(new Dictionary<ITelemetryProperty, object>(_values));
    }
}
