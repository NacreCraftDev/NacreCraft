namespace NacreCraft.Client.Blaze3d.Audio;

//PollingDeviceTracker refreshes the device list once a second, maps to vanilla com.mojang.blaze3d.audio.PollingDeviceTracker
public sealed class PollingDeviceTracker : AbstractDeviceTracker
{
    private const long DefaultDeviceCheckIntervalMs = 1000L;

    private long _lastDeviceCheckTime;

    public PollingDeviceTracker(DeviceList deviceList) : base(deviceList)
    {
    }

    protected override bool IsUpdateRequested()
        => Environment.TickCount64 - _lastDeviceCheckTime >= DefaultDeviceCheckIntervalMs;

    protected override void DiscardUpdateRequest()
        => _lastDeviceCheckTime = Environment.TickCount64;
}
