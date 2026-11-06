using Silk.NET.OpenAL;

namespace NacreCraft.Client.Blaze3d.Audio;

//DeviceList the available playback devices, maps to vanilla com.mojang.blaze3d.audio.DeviceList
public sealed record DeviceList(string? DefaultDevice, IReadOnlyList<string> AllDevices)
{
    public static readonly DeviceList EMPTY = new(null, Array.Empty<string>());

    //Query asks OpenAL for the default playback device, an empty list when the enumeration extension is absent
    //Silk.NET exposes the default name but not the LWJGL string-list helper, so AllDevices stays empty for now
    public static unsafe DeviceList Query()
    {
        var alc = OpenAlUtil.Alc;
        if (alc is null) return EMPTY;
        if (!alc.IsExtensionPresent("ALC_ENUMERATE_ALL_EXT")) return EMPTY;

        var defaultDevice = alc.GetContextProperty((Device*)0, GetContextString.DeviceSpecifier);
        return new DeviceList(defaultDevice, Array.Empty<string>());
    }
}
