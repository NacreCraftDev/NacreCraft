namespace NacreCraft.Client.Telemetry;

//TelemetryEventSender the sink for telemetry events, maps to vanilla net.minecraft.client.telemetry.TelemetryEventSender
public interface TelemetryEventSender
{
    void Send(TelemetryEventType type, Action<TelemetryPropertyMap.Builder> properties);

    //Decorate appends extra properties to every event, used by the world session manager
    TelemetryEventSender Decorate(Action<TelemetryPropertyMap.Builder> decorator);

    static TelemetryEventSender Disabled { get; } = new DisabledSender();

    private sealed class DisabledSender : TelemetryEventSender
    {
        public void Send(TelemetryEventType type, Action<TelemetryPropertyMap.Builder> properties)
        {
        }

        public TelemetryEventSender Decorate(Action<TelemetryPropertyMap.Builder> decorator) => this;
    }
}
