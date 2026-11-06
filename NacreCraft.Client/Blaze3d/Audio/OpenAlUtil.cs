using NacreCraft.Client.Sounds;
using NacreCraft.Logging;
using Silk.NET.OpenAL;

namespace NacreCraft.Client.Blaze3d.Audio;

//OpenAlUtil OpenAL error checks and format mapping, maps to vanilla com.mojang.blaze3d.audio.OpenAlUtil
public static unsafe class OpenAlUtil
{
    //Al/Alc the API instances Library installs on init, null before the audio device is opened
    public static AL? Al { get; set; }

    public static ALContext? Alc { get; set; }

    public static bool CheckALError(string location)
    {
        var error = Al?.GetError() ?? AudioError.NoError;
        if (error == AudioError.NoError) return false;
        Log.Error($"{location}: {AlErrorToString(error)}");
        return true;
    }

    public static bool CheckALCError(nint device, string location)
    {
        if (Alc is null) return false;
        var error = Alc.GetError((Device*)device);
        if (error == ContextError.NoError) return false;
        Log.Error($"{location} ({device}): {AlcErrorToString(error)}");
        return true;
    }

    public static BufferFormat AudioFormatToOpenAl(AudioFormat format)
    {
        if (format.Channels == 1)
        {
            if (format.SampleSizeInBits == 8) return BufferFormat.Mono8;
            if (format.SampleSizeInBits == 16) return BufferFormat.Mono16;
        }
        else if (format.Channels == 2)
        {
            if (format.SampleSizeInBits == 8) return BufferFormat.Stereo8;
            if (format.SampleSizeInBits == 16) return BufferFormat.Stereo16;
        }
        throw new ArgumentException($"Invalid audio format: {format}");
    }

    private static string AlErrorToString(AudioError error) => error switch
    {
        AudioError.InvalidName => "Invalid name parameter.",
        AudioError.InvalidEnum => "Invalid enumerated parameter value.",
        AudioError.InvalidValue => "Invalid parameter parameter value.",
        AudioError.InvalidOperation => "Invalid operation.",
        AudioError.OutOfMemory => "Unable to allocate memory.",
        _ => "An unrecognized error occurred."
    };

    private static string AlcErrorToString(ContextError error) => error switch
    {
        ContextError.InvalidDevice => "Invalid device.",
        ContextError.InvalidContext => "Invalid context.",
        ContextError.InvalidValue => "Invalid value.",
        ContextError.InvalidEnum => "Illegal enum.",
        ContextError.OutOfMemory => "Unable to allocate memory.",
        _ => "An unrecognized error occurred."
    };
}
