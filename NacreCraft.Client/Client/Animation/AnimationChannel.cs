using System.Numerics;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Util;

namespace NacreCraft.Client.Animation;

//AnimationChannel animates one target of one model part, maps to vanilla net.minecraft.client.animation.AnimationChannel
public sealed class AnimationChannel
{
    public AnimationChannel(TargetApplier target, params Keyframe[] keyframes)
    {
        Target = target;
        Keyframes = keyframes;
    }

    public TargetApplier Target { get; }

    public Keyframe[] Keyframes { get; }

    //TargetApplier applies an animated offset to a model part
    public delegate void TargetApplier(ModelPart part, Vector3 offset);

    //Interpolation blends two keyframes, maps to vanilla AnimationChannel.Interpolation
    public delegate Vector3 Interpolation(Vector3 vector, float alpha, Keyframe[] keyframes, int prev, int next, float targetScale);

    public static class Targets
    {
        public static readonly TargetApplier Position = (part, offset) => part.OffsetPos(offset);

        public static readonly TargetApplier Rotation = (part, offset) => part.OffsetRotation(offset);

        public static readonly TargetApplier Scale = (part, offset) => part.OffsetScale(offset);
    }

    public static class Interpolations
    {
        public static readonly Interpolation Linear = (vector, alpha, keyframes, prev, next, targetScale) =>
        {
            var point0 = keyframes[prev].PostTarget;
            var point1 = keyframes[next].PreTarget;
            return Vector3.Lerp(point0, point1, alpha) * targetScale;
        };

        public static readonly Interpolation CatmullRom = (vector, alpha, keyframes, prev, next, targetScale) =>
        {
            var point0 = keyframes[Math.Max(0, prev - 1)].PostTarget;
            var point1 = keyframes[prev].PostTarget;
            var point2 = keyframes[next].PostTarget;
            var point3 = keyframes[Math.Min(keyframes.Length - 1, next + 1)].PostTarget;
            return new Vector3(
                Mth.CatmullRom(alpha, point0.X, point1.X, point2.X, point3.X) * targetScale,
                Mth.CatmullRom(alpha, point0.Y, point1.Y, point2.Y, point3.Y) * targetScale,
                Mth.CatmullRom(alpha, point0.Z, point1.Z, point2.Z, point3.Z) * targetScale);
        };
    }
}
