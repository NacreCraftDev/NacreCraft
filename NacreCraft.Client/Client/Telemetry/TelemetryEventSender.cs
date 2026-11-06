namespace NacreCraft.Client.Telemetry;

//TelemetryEventSender the sink for telemetry events, maps to vanilla net.minecraft.client.telemetry.TelemetryEventSender
public interface TelemetryEventSender
{
    void Send(TelemetryEventType type, Action<TelemetryPropertyMap.Builder> properties);
}
