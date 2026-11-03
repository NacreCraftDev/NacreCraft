using System.Numerics;
using NacreCraft.Client.Model.Geom;

namespace NacreCraft.Client.Animation;

//AnimationDefinition one baked animation description, maps to vanilla net.minecraft.client.animation.AnimationDefinition
public sealed record AnimationDefinition(
    float LengthInSeconds,
    bool Looping,
    Dictionary<string, List<AnimationChannel>> BoneAnimations)
{
    public KeyframeAnimation Bake(ModelPart root) => KeyframeAnimation.Bake(root, this);

    public sealed class Builder
    {
        private readonly float _length;
        private readonly Dictionary<string, List<AnimationChannel>> _animationByBone = new();
        private bool _looping;

        private Builder(float length) => _length = length;

        public static Builder WithLength(float lengthInSeconds) => new(lengthInSeconds);

        public Builder Looping()
        {
            _looping = true;
            return this;
        }

        public Builder AddAnimation(string boneName, AnimationChannel animation)
        {
            if (!_animationByBone.TryGetValue(boneName, out var channels))
                _animationByBone[boneName] = channels = new List<AnimationChannel>();
            channels.Add(animation);
            return this;
        }

        public AnimationDefinition Build() => new(_length, _looping, _animationByBone);
    }
}
