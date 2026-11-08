using System.Text.Json;
using System.Text.Json.Nodes;

namespace NacreCraft.Client.Telemetry;

//TelemetryEventInstance one event ready to log or export, maps to vanilla net.minecraft.client.telemetry.TelemetryEventInstance
public sealed class TelemetryEventInstance
{
    public TelemetryEventInstance(TelemetryEventType type, TelemetryPropertyMap properties)
    {
        foreach (var property in properties.PropertySet())
        {
            if (!type.Contains(property))
                throw new ArgumentException($"Property '{property.Id}' not expected for event: '{type.Id}'");
        }
        Type = type;
        Properties = properties;
    }

    public TelemetryEventType Type { get; }

    public TelemetryPropertyMap Properties { get; }

    public ITelemetryEvent Export(ITelemetrySession session) => Type.Export(session, Properties);

    //ToJson the local-log shape: event id plus property id to value
    public JsonObject ToJson()
    {
        var properties = new JsonObject();
        foreach (var property in Properties.PropertySet())
        {
            properties[property.Id] = ToJsonValue(Properties.GetRaw(property));
        }

        return new JsonObject
        {
            ["type"] = Type.Id,
            ["properties"] = properties
        };
    }

    //ToJsonValue mirrors the vanilla codec shape: enums and timestamps are strings, samples stay arrays
    private static JsonNode? ToJsonValue(object? value) => value switch
    {
        null => null,
        Guid uuid => JsonValue.Create(uuid.ToString()),
        DateTimeOffset instant => JsonValue.Create(instant.ToString("O")),
        TelemetryProperty.GameMode mode => JsonValue.Create(mode switch
        {
            TelemetryProperty.GameMode.Survival => "survival",
            TelemetryProperty.GameMode.Creative => "creative",
            TelemetryProperty.GameMode.Adventure => "adventure",
            TelemetryProperty.GameMode.Spectator => "spectator",
            _ => "hardcore"
        }),
        TelemetryProperty.ServerType type => JsonValue.Create(type switch
        {
            TelemetryProperty.ServerType.Realm => "realm",
            TelemetryProperty.ServerType.Local => "local",
            _ => "server"
        }),
        Events.GameLoadTimesEvent.Measurement measurement => JsonValue.Create(measurement.Millis),
        long[] samples => new JsonArray(samples.Select(sample => (JsonNode?)JsonValue.Create(sample)).ToArray()),
        _ => JsonSerializer.SerializeToNode(value)
    };
}
