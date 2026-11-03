namespace NacreCraft.Util;

//KeyframeTrackSampler samples a baked keyframe track, maps to vanilla net.minecraft.util.KeyframeTrackSampler
//The track is expanded into segments once, so a sample only has to find the segment and lerp inside it
public sealed class KeyframeTrackSampler<T>
{
    private readonly int? _periodTicks;
    private readonly Func<float, T, T, T> _lerp;
    private readonly IReadOnlyList<Segment> _segments;

    internal KeyframeTrackSampler(KeyframeTrack<T> track, int? periodTicks, Func<float, T, T, T> lerp)
    {
        _periodTicks = periodTicks;
        _lerp = lerp;
        _segments = BakeSegments(track, periodTicks);
    }

    private static IReadOnlyList<Segment> BakeSegments(KeyframeTrack<T> track, int? periodTicks)
    {
        var keyframes = track.Keyframes;
        if (keyframes.Count == 1)
        {
            var value = keyframes[0].Value;
            return new[] { new Segment(EasingType.ConstantType, value, 0, value, 0) };
        }
        var segments = new List<Segment>();
        if (periodTicks is { } period)
        {
            //A periodic track wraps the last keyframe back onto the first one across the period boundary
            var first = keyframes[0];
            var last = keyframes[^1];
            segments.Add(new Segment(track.EasingType, last.Value, last.Ticks - period, first.Value, first.Ticks));
            AddSegmentsFromKeyframes(track, keyframes, segments);
            segments.Add(new Segment(track.EasingType, last.Value, last.Ticks, first.Value, first.Ticks + period));
        }
        else
        {
            AddSegmentsFromKeyframes(track, keyframes, segments);
        }
        return segments;
    }

    private static void AddSegmentsFromKeyframes(KeyframeTrack<T> track, IReadOnlyList<Keyframe<T>> keyframes,
        List<Segment> output)
    {
        for (var i = 0; i < keyframes.Count - 1; i++)
        {
            var keyframe = keyframes[i];
            var next = keyframes[i + 1];
            output.Add(new Segment(track.EasingType, keyframe.Value, keyframe.Ticks, next.Value, next.Ticks));
        }
    }

    public T Sample(long ticks)
    {
        var sampleTicks = LoopTicks(ticks);
        var segment = GetSegmentAt(sampleTicks);
        if (sampleTicks <= segment.FromTicks) return segment.FromValue;
        if (sampleTicks >= segment.ToTicks) return segment.ToValue;
        var alpha = (sampleTicks - segment.FromTicks) / (float)(segment.ToTicks - segment.FromTicks);
        return _lerp(segment.Easing.Apply(alpha), segment.FromValue, segment.ToValue);
    }

    private Segment GetSegmentAt(long currentTicks)
    {
        foreach (var segment in _segments)
            if (currentTicks < segment.ToTicks)
                return segment;
        return _segments[^1];
    }

    private long LoopTicks(long ticks)
        => _periodTicks is { } period ? ((ticks % period) + period) % period : ticks;

    private readonly record struct Segment(EasingType Easing, T FromValue, int FromTicks, T ToValue, int ToTicks);
}
