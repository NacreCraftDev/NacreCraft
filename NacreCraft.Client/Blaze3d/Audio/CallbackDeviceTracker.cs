namespace NacreCraft.Client.Blaze3d.Audio;

//CallbackDeviceTracker the event-driven device tracker, maps to vanilla com.mojang.blaze3d.audio.CallbackDeviceTracker
//Silk.NET.OpenAL does not bind the ALC_SOFT_system_events callback, so the callback path is unavailable and Library falls back to polling
public sealed class CallbackDeviceTracker : AbstractDeviceTracker
{
    private volatile bool _updateRequested;

    public CallbackDeviceTracker(DeviceList deviceList) : base(deviceList)
    {
    }

    protected override bool IsUpdateRequested() => _updateRequested;

    protected override void DiscardUpdateRequest() => _updateRequested = false;

    public static bool IsSupported() => false;

    public static CallbackDeviceTracker CreateAndInstall(DeviceList deviceList)
        => new(deviceList);
}
