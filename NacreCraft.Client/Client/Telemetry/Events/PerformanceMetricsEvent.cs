namespace NacreCraft.Client.Telemetry.Events;

//PerformanceMetricsEvent samples FPS, frame time and memory, maps to vanilla net.minecraft.client.telemetry.events.PerformanceMetricsEvent
public sealed class PerformanceMetricsEvent : AggregatedTelemetryEvent
{
    private static readonly long DedicatedMemoryKb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1000;

    private readonly List<long> _fpsSamples = new();
    private readonly List<long> _frameTimeSamples = new();
    private readonly List<long> _usedMemorySamples = new();

    public override void Tick(TelemetryEventSender eventSender)
    {
        if (MinecraftClient.Current?.TelemetryOptInExtra == true) base.Tick(eventSender);
    }

    public override void TakeSample()
    {
        if (MinecraftClient.Current is not { } client) return;
        _fpsSamples.Add(client.GetFps());
        _usedMemorySamples.Add(GC.GetTotalMemory(false) / 1000);
        _frameTimeSamples.Add(client.GetFrameTimeNs());
    }

    public override void SendEvent(TelemetryEventSender eventSender)
    {
        var client = MinecraftClient.Current;
        eventSender.Send(TelemetryEventType.PerformanceMetrics, properties =>
        {
            properties.Put(TelemetryProperty.FrameRateSamples, _fpsSamples.ToArray());
            properties.Put(TelemetryProperty.RenderTimeSamples, _frameTimeSamples.ToArray());
            properties.Put(TelemetryProperty.UsedMemorySamples, _usedMemorySamples.ToArray());
            properties.Put(TelemetryProperty.NumberOfSamples, GetSampleCount());
            properties.Put(TelemetryProperty.RenderDistance, client?.Config.RenderDistance ?? 0);
            properties.Put(TelemetryProperty.DedicatedMemoryKb, (int)DedicatedMemoryKb);
        });
        _fpsSamples.Clear();
        _frameTimeSamples.Clear();
        _usedMemorySamples.Clear();
    }
}
