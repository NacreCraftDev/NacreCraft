using NacreCraft.Client.Sounds;
using NacreCraft.Logging;
using NacreCraft.Primitives;
using Silk.NET.OpenAL;

namespace NacreCraft.Client.Blaze3d.Audio;

//Channel one OpenAL source, maps to vanilla com.mojang.blaze3d.audio.Channel
public sealed class Channel
{
    private const int QueuedBufferCount = 4;
    public const int BufferDurationSeconds = 1;

    private readonly uint _source;
    private bool _initialized = true;
    private int _streamingBufferSize = 16384;
    private AudioStream? _stream;

    private Channel(uint source) => _source = source;

    public static Channel? Create()
    {
        var al = OpenAlUtil.Al;
        if (al is null) return null;
        var sources = al.GenSources(1);
        if (OpenAlUtil.CheckALError("Allocate new source")) return null;
        return new Channel(sources[0]);
    }

    public void Destroy()
    {
        if (!_initialized) return;
        _initialized = false;

        var al = OpenAlUtil.Al;
        if (al is null) return;
        al.SourceStop(_source);
        OpenAlUtil.CheckALError("Stop");

        if (_stream is not null)
        {
            try
            {
                _stream.Dispose();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to close audio stream {e}");
            }
            RemoveProcessedBuffers();
            _stream = null;
        }

        al.DeleteSources(new[] { _source });
        OpenAlUtil.CheckALError("Cleanup");
    }

    public void Play() => OpenAlUtil.Al?.SourcePlay(_source);

    private SourceState GetState()
    {
        if (!_initialized) return SourceState.Stopped;
        if (OpenAlUtil.Al is null) return SourceState.Stopped;
        OpenAlUtil.Al.GetSourceProperty(_source, GetSourceInteger.SourceState, out int state);
        return (SourceState)state;
    }

    public void Pause()
    {
        if (GetState() == SourceState.Playing) OpenAlUtil.Al?.SourcePause(_source);
    }

    public void Unpause()
    {
        if (GetState() == SourceState.Paused) OpenAlUtil.Al?.SourcePlay(_source);
    }

    public void Stop()
    {
        if (!_initialized) return;
        OpenAlUtil.Al?.SourceStop(_source);
        OpenAlUtil.CheckALError("Stop");
    }

    public bool Playing() => GetState() == SourceState.Playing;

    public bool Stopped() => GetState() == SourceState.Stopped;

    public void SetSelfPosition(Vec3 newPosition)
        => OpenAlUtil.Al?.SetSourceProperty(_source, SourceVector3.Position,
            (float)newPosition.X, (float)newPosition.Y, (float)newPosition.Z);

    public void SetPitch(float pitch) => OpenAlUtil.Al?.SetSourceProperty(_source, SourceFloat.Pitch, pitch);

    public void SetLooping(bool looping)
        => OpenAlUtil.Al?.SetSourceProperty(_source, SourceBoolean.Looping, looping);

    public void SetVolume(float volume) => OpenAlUtil.Al?.SetSourceProperty(_source, SourceFloat.Gain, volume);

    public void DisableAttenuation()
        => OpenAlUtil.Al?.SetSourceProperty(_source, (SourceInteger)53248, 0);

    public void LinearAttenuation(float maxDistance)
    {
        var al = OpenAlUtil.Al;
        if (al is null) return;
        al.SetSourceProperty(_source, (SourceInteger)53248, 53251);
        al.SetSourceProperty(_source, SourceFloat.MaxDistance, maxDistance);
        al.SetSourceProperty(_source, SourceFloat.RolloffFactor, 1.0f);
        al.SetSourceProperty(_source, SourceFloat.ReferenceDistance, 0.0f);
    }

    public void SetRelative(bool relative)
        => OpenAlUtil.Al?.SetSourceProperty(_source, SourceBoolean.SourceRelative, relative);

    public void AttachStaticBuffer(SoundBuffer buffer)
    {
        if (buffer.GetAlBuffer() is { } bufferId)
            OpenAlUtil.Al?.SetSourceProperty(_source, SourceInteger.Buffer, bufferId);
    }

    public void AttachBufferStream(AudioStream stream)
    {
        _stream = stream;
        _streamingBufferSize = CalculateBufferSize(stream.Format, 1);
        PumpBuffers(QueuedBufferCount);
    }

    private static int CalculateBufferSize(AudioFormat format, int seconds)
        => seconds * format.SampleSizeInBits / 8 * format.Channels * format.SampleRate;

    private void PumpBuffers(int size)
    {
        if (_stream is null) return;
        try
        {
            for (var i = 0; i < size; i++)
            {
                var data = _stream.Read(_streamingBufferSize);
                if (data is null) continue;
                var buffer = new SoundBuffer(data, _stream.Format);
                if (buffer.ReleaseAlBuffer() is { } bufferId)
                    OpenAlUtil.Al?.SourceQueueBuffers(_source, new[] { bufferId });
            }
        }
        catch (Exception e) when (e is IOException or InvalidOperationException)
        {
            Log.Error($"Failed to read from audio stream {e}");
        }
    }

    public void UpdateStream()
    {
        if (_stream is null) return;
        var processedBuffers = RemoveProcessedBuffers();
        PumpBuffers(processedBuffers);
    }

    private int RemoveProcessedBuffers()
    {
        var al = OpenAlUtil.Al;
        if (al is null) return 0;
        al.GetSourceProperty(_source, GetSourceInteger.BuffersProcessed, out int processed);
        if (processed <= 0) return processed;

        var ids = new uint[processed];
        al.SourceUnqueueBuffers(_source, ids);
        OpenAlUtil.CheckALError("Unqueue buffers");
        al.DeleteBuffers(ids);
        OpenAlUtil.CheckALError("Remove processed buffers");
        return processed;
    }
}
