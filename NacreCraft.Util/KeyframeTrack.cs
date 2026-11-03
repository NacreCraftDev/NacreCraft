using NacreCraft.Codec;

namespace NacreCraft.Util;

//KeyframeTrack an ordered list of keyframes plus the easing applied between them, maps to vanilla net.minecraft.util.KeyframeTrack
//A track must have at least one keyframe, keep them ordered by ticks and never place more than two on the same tick
public sealed class KeyframeTrack<T>
{
    public KeyframeTrack(IReadOnlyList<Keyframe<T>> keyframes, EasingType easingType)
    {
        if (keyframes.Count == 0) throw new ArgumentException("Track has no keyframes", nameof(keyframes));
        Keyframes = keyframes;
        EasingType = easingType;
    }

    public IReadOnlyList<Keyframe<T>> Keyframes { get; }

    public EasingType EasingType { get; }

    //CreateCodec reads the keyframes plus the optional ease field, maps to vanilla KeyframeTrack.mapCodec
    public static Codec<KeyframeTrack<T>> CreateCodec(Codec<T> valueCodec) => RecordCodecBuilder.Of2(
        Keyframe<T>.CreateCodec(valueCodec).ListOf().Validate(ValidateKeyframes)
            .FieldOf("keyframes").ForGetter<KeyframeTrack<T>, IReadOnlyList<Keyframe<T>>>(track => track.Keyframes),
        EasingType.Codec.OptionalFieldOf("ease", EasingType.Linear)
            .ForGetter<KeyframeTrack<T>, EasingType>(track => track.EasingType),
        (keyframes, easing) => new KeyframeTrack<T>(keyframes, easing));

    private static DataResult<IReadOnlyList<Keyframe<T>>> ValidateKeyframes(IReadOnlyList<Keyframe<T>> keyframes)
    {
        if (keyframes.Count == 0) return DataResult<IReadOnlyList<Keyframe<T>>>.Error(() => "Keyframes must not be empty");
        for (var i = 1; i < keyframes.Count; i++)
            if (keyframes[i - 1].Ticks > keyframes[i].Ticks)
                return DataResult<IReadOnlyList<Keyframe<T>>>.Error(() => "Keyframes must be ordered by ticks field");
        if (keyframes.Count > 1)
        {
            var repeatCount = 0;
            var lastTicks = keyframes[^1].Ticks;
            foreach (var keyframe in keyframes)
            {
                if (keyframe.Ticks == lastTicks)
                {
                    if (++repeatCount > 2)
                        return DataResult<IReadOnlyList<Keyframe<T>>>.Error(() => $"More than 2 keyframes on same tick: {keyframe.Ticks}");
                }
                else
                {
                    repeatCount = 0;
                }
                lastTicks = keyframe.Ticks;
            }
        }
        return DataResult<IReadOnlyList<Keyframe<T>>>.Success(keyframes);
    }

    //ValidatePeriod rejects keyframes outside [0; periodTicks], maps to vanilla KeyframeTrack.validatePeriod
    public static DataResult<KeyframeTrack<T>> ValidatePeriod(KeyframeTrack<T> track, int periodTicks)
    {
        foreach (var keyframe in track.Keyframes)
        {
            if (keyframe.Ticks >= 0 && keyframe.Ticks <= periodTicks) continue;
            return DataResult<KeyframeTrack<T>>.Error(() => $"Keyframe at tick {keyframe.Ticks} must be in range [0; {periodTicks}]");
        }
        return DataResult<KeyframeTrack<T>>.Success(track);
    }

    //BakeSampler bakes the period and the lerp into a sampler; the lerp is a plain delegate so this assembly stays below the attribute package
    public KeyframeTrackSampler<T> BakeSampler(int? periodTicks, Func<float, T, T, T> lerp) => new(this, periodTicks, lerp);

    //Builder assembles a track with validation, maps to vanilla KeyframeTrack.Builder
    public sealed class Builder
    {
        private readonly List<Keyframe<T>> _keyframes = new();
        private EasingType _easing = EasingType.Linear;

        public Builder AddKeyframe(int ticks, T value)
        {
            _keyframes.Add(new Keyframe<T>(ticks, value));
            return this;
        }

        public Builder SetEasing(EasingType easing)
        {
            _easing = easing;
            return this;
        }

        public KeyframeTrack<T> Build()
            => new(ValidateKeyframes(_keyframes).GetOrThrow(), _easing);
    }
}
