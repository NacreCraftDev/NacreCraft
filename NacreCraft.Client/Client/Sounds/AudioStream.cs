namespace NacreCraft.Client.Sounds;

//AudioStream a decoded streaming sound source, maps to vanilla net.minecraft.client.sounds.AudioStream
public interface AudioStream : IDisposable
{
    AudioFormat Format { get; }

    //Read takes up to size bytes of PCM data, null at end of stream
    byte[]? Read(int size);
}
