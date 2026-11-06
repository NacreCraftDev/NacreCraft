namespace NacreCraft.Client.Blaze3d.Audio;

//DeviceTracker follows the audio device list, maps to vanilla com.mojang.blaze3d.audio.DeviceTracker
public interface DeviceTracker
{
    DeviceList CurrentDevices();

    void Tick();

    void ForceRefresh();
}
