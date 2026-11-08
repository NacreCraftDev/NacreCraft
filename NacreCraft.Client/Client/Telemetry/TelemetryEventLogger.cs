namespace NacreCraft.Client.Telemetry;

//TelemetryEventLogger the local-log sink, maps to vanilla net.minecraft.client.telemetry.TelemetryEventLogger
public interface TelemetryEventLogger
{
    void Log(TelemetryEventInstance instance);
}
