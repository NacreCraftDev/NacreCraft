using NacreCraft.Registry.Environment;
using NacreCraft.Util;

namespace NacreCraft.Registry;

//AttributeTrackSampler samples one attribute track at the current clock tick, maps to vanilla net.minecraft.world.timeline.AttributeTrackSampler
//The sampled argument is cached per cacheTickId, so every consumer asking within the same tick pays the sampling once
public sealed class AttributeTrackSampler<Value> : EnvironmentAttributeLayer.TimeBased<Value>
{
    private readonly Holder<WorldClock> _clock;
    private readonly AttributeModifier<Value, object> _modifier;
    private readonly ClockManager _clockManager;
    private readonly KeyframeTrackSampler<object> _argumentSampler;
    private int _cachedTickId;
    private object? _cachedArgument;
    private bool _hasCachedArgument;

    public AttributeTrackSampler(Holder<WorldClock> clock, int? periodTicks, AttributeModifier<Value, object> modifier,
        KeyframeTrack<object> argumentTrack, LerpFunction<object> argumentLerp, ClockManager clockManager)
    {
        _clock = clock;
        _modifier = modifier;
        _clockManager = clockManager;
        //The sampler takes a plain delegate, the attribute package owns the LerpFunction abstraction
        _argumentSampler = argumentTrack.BakeSampler(periodTicks, argumentLerp.Apply);
    }

    public Value ApplyTimeBased(Value baseValue, int cacheTickId)
    {
        if (!_hasCachedArgument || cacheTickId != _cachedTickId)
        {
            _cachedTickId = cacheTickId;
            _cachedArgument = _argumentSampler.Sample(_clockManager.GetTotalTicks(_clock));
            _hasCachedArgument = true;
        }
        return _modifier.Apply(baseValue, _cachedArgument!);
    }
}
