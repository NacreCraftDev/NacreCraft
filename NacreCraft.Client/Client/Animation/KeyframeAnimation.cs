using System.Numerics;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Util;

namespace NacreCraft.Client.Animation;

//KeyframeAnimation a baked animation bound to concrete model parts, maps to vanilla net.minecraft.client.animation.KeyframeAnimation
public sealed class KeyframeAnimation
{
    private readonly AnimationDefinition _definition;
    private readonly List<Entry> _entries;

    private KeyframeAnimation(AnimationDefinition definition, List<Entry> entries)
    {
        _definition = definition;
        _entries = entries;
    }

    internal static KeyframeAnimation Bake(ModelPart root, AnimationDefinition definition)
    {
        var entries = new List<Entry>();
        var partLookup = root.CreatePartLookup();
        foreach (var (partName, channels) in definition.BoneAnimations)
        {
            var part = partLookup(partName)
                ?? throw new ArgumentException($"Cannot animate {partName}, which does not exist in model");
            foreach (var channel in channels)
                entries.Add(new Entry(part, channel.Target, channel.Keyframes));
        }
        return new KeyframeAnimation(definition, entries);
    }

    public void ApplyStatic() => Apply(0L, 1.0f);

    public void ApplyWalk(float animationPos, float animationSpeed, float speedFactor, float scaleFactor)
    {
        var time = (long)(animationPos * 50.0f * speedFactor);
        var scale = Math.Min(animationSpeed * scaleFactor, 1.0f);
        Apply(time, scale);
    }

    public void Apply(AnimationState animationState, float currentTime) => Apply(animationState, currentTime, 1.0f);

    public void Apply(AnimationState animationState, float currentTime, float speedFactor)
        => animationState.IfStarted(state => Apply((long)(state.GetTimeInMillis(currentTime) * speedFactor), 1.0f));

    public void Apply(long millisSinceStart, float targetScale)
    {
        var secondsSinceStart = GetElapsedSeconds(millisSinceStart);
        var scratchVector = Vector3.Zero;
        foreach (var entry in _entries)
            entry.Apply(secondsSinceStart, targetScale, ref scratchVector);
    }

    private float GetElapsedSeconds(long millisSinceStart)
    {
        var secondsSinceStart = millisSinceStart / 1000.0f;
        return _definition.Looping ? secondsSinceStart % _definition.LengthInSeconds : secondsSinceStart;
    }

    private sealed record Entry(ModelPart Part, AnimationChannel.TargetApplier Target, Keyframe[] Keyframes)
    {
        public void Apply(float secondsSinceStart, float targetScale, ref Vector3 scratchVector)
        {
            var prev = Math.Max(0,
                Mth.BinarySearch(0, Keyframes.Length, i => secondsSinceStart <= Keyframes[i].Timestamp) - 1);
            var next = Math.Min(Keyframes.Length - 1, prev + 1);
            var previousFrame = Keyframes[prev];
            var nextFrame = Keyframes[next];
            var keyframeTimeDelta = secondsSinceStart - previousFrame.Timestamp;
            var lerpAlpha = next != prev
                ? Mth.Clamp(keyframeTimeDelta / (nextFrame.Timestamp - previousFrame.Timestamp), 0.0f, 1.0f)
                : 0.0f;
            scratchVector = nextFrame.Interpolation(scratchVector, lerpAlpha, Keyframes, prev, next, targetScale);
            Target(Part, scratchVector);
        }
    }
}
