using NacreCraft.Client.Sounds;

namespace NacreCraft.Client.Blaze3d.Audio;

//SoundBuffer one PCM payload and its OpenAL buffer, maps to vanilla com.mojang.blaze3d.audio.SoundBuffer
public sealed class SoundBuffer
{
    private byte[]? _data;
    private readonly AudioFormat _format;
    private bool _hasAlBuffer;
    private uint _alBuffer;
    private readonly int _size;

    public SoundBuffer(byte[] data, AudioFormat format)
    {
        _data = data;
        _format = format;
        _size = data.Length;
    }

    //GetAlBuffer uploads the data on first use, null when OpenAL refuses it
    public uint? GetAlBuffer()
    {
        if (_hasAlBuffer) return _alBuffer;
        var al = OpenAlUtil.Al;
        if (_data is null || al is null) return null;

        var buffers = al.GenBuffers(1);
        if (OpenAlUtil.CheckALError("Creating buffer")) return null;
        al.BufferData(buffers[0], OpenAlUtil.AudioFormatToOpenAl(_format), _data, _format.SampleRate);
        if (OpenAlUtil.CheckALError("Assigning buffer data")) return null;

        _alBuffer = buffers[0];
        _hasAlBuffer = true;
        _data = null;
        return _alBuffer;
    }

    public void DiscardAlBuffer()
    {
        if (!_hasAlBuffer) return;
        OpenAlUtil.Al?.DeleteBuffers(new[] { _alBuffer });
        if (OpenAlUtil.CheckALError("Deleting stream buffers")) return;
        _hasAlBuffer = false;
    }

    //ReleaseAlBuffer hands the buffer to the caller, which now owns its lifetime
    public uint? ReleaseAlBuffer()
    {
        var result = GetAlBuffer();
        _hasAlBuffer = false;
        return result;
    }

    public AudioFormat Format => _format;

    public int Size => _size;

    public bool IsValid() => _data is not null || _hasAlBuffer;
}
