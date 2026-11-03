using System.Numerics;

namespace NacreCraft.Client.Animation;

//Keyframe one animation key with its easing, maps to vanilla net.minecraft.client.animation.Keyframe
public sealed record Keyframe(float Timestamp, Vector3 PreTarget, Vector3 PostTarget, AnimationChannel.Interpolation Interpolation)
{
    public Keyframe(float timestamp, Vector3 postTarget, AnimationChannel.Interpolation interpolation)
        : this(timestamp, postTarget, postTarget, interpolation) { }
}
