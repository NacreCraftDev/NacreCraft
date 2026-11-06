using Silk.NET.OpenAL;

namespace NacreCraft.Client.Blaze3d.Audio;

//Listener the OpenAL listener state, maps to vanilla com.mojang.blaze3d.audio.Listener
public sealed class Listener
{
    private ListenerTransform _transform = ListenerTransform.Initial;

    public unsafe void SetTransform(ListenerTransform transform)
    {
        _transform = transform;
        var al = OpenAlUtil.Al;
        if (al is null) return;

        var position = transform.Position;
        var forward = transform.Forward;
        var up = transform.Up;
        al.SetListenerProperty(ListenerVector3.Position, (float)position.X, (float)position.Y, (float)position.Z);

        var orientation = stackalloc float[6];
        orientation[0] = (float)forward.X;
        orientation[1] = (float)forward.Y;
        orientation[2] = (float)forward.Z;
        orientation[3] = (float)up.X;
        orientation[4] = (float)up.Y;
        orientation[5] = (float)up.Z;
        al.SetListenerProperty(ListenerFloatArray.Orientation, orientation);
    }

    public void Reset() => SetTransform(ListenerTransform.Initial);

    public ListenerTransform GetTransform() => _transform;
}
