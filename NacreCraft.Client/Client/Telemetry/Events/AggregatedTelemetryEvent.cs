namespace NacreCraft.Client.Telemetry.Events;

//AggregatedTelemetryEvent samples once a minute and sends after ten samples, maps to vanilla net.minecraft.client.telemetry.events.AggregatedTelemetryEvent
public abstract class AggregatedTelemetryEvent
{
    private const int SampleIntervalMs = 60000;
    private const int SamplesPerEvent = 10;

    private int _sampleCount;
    private bool _ticking;
    private DateTimeOffset? _lastSampleTime;

    public void Start()
    {
        _ticking = true;
        _lastSampleTime = DateTimeOffset.UtcNow;
        _sampleCount = 0;
    }

    public virtual void Tick(TelemetryEventSender eventSender)
    {
        if (ShouldTakeSample())
        {
            TakeSample();
            _sampleCount++;
            _lastSampleTime = DateTimeOffset.UtcNow;
        }
        if (ShouldSentEvent())
        {
            SendEvent(eventSender);
            _sampleCount = 0;
        }
    }

    public bool ShouldTakeSample()
        => _ticking && _lastSampleTime is { } last
           && (DateTimeOffset.UtcNow - last).TotalMilliseconds > SampleIntervalMs;

    public bool ShouldSentEvent() => _sampleCount >= SamplesPerEvent;

    public void Stop() => _ticking = false;

    protected int GetSampleCount() => _sampleCount;

    public abstract void TakeSample();

    public abstract void SendEvent(TelemetryEventSender eventSender);
}
