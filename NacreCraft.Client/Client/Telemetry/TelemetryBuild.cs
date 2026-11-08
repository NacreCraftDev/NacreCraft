namespace NacreCraft.Client.Telemetry;

//TelemetryBuild the compile-time telemetry gate, maps to the hardcoded disabled/vanilla behavior switch
//Default build compiles every telemetry class but the gate is a const false; -p:EnableTelemetry=true defines NC_TELEMETRY
internal static class TelemetryBuild
{
#if NC_TELEMETRY
    public const bool Enabled = true;
#else
    public const bool Enabled = false;
#endif
}
