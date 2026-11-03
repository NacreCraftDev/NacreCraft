using NacreCraft.Codec;
using NacreCraft.Registry.Environment;
using NacreCraft.Util;

namespace NacreCraft.Registry;

//AttributeTrack one environment attribute track of a timeline, maps to vanilla net.minecraft.world.timeline.AttributeTrack
//Holds the modifier and the keyframed argument; a client samples it against the world clock to derive the attribute value
//NC's modifiers erase their argument to object, so the argument track is a track of objects
public sealed class AttributeTrack<Value>
{
    public AttributeTrack(AttributeModifier<Value, object> modifier, KeyframeTrack<object> argumentTrack)
    {
        Modifier = modifier;
        ArgumentTrack = argumentTrack;
    }

    public AttributeModifier<Value, object> Modifier { get; }

    public KeyframeTrack<object> ArgumentTrack { get; }

    //BakeSampler turns the track into a sampler bound to the clock and period, maps to vanilla bakeSampler
    public AttributeTrackSampler<Value> BakeSampler(EnvironmentAttribute<Value> attribute, Holder<WorldClock> clock,
        int? periodTicks, ClockManager clockManager)
        => new(clock, periodTicks, Modifier, ArgumentTrack, Modifier.ArgumentKeyframeLerp(attribute), clockManager);

    //ValidatePeriod rejects keyframes outside the timeline period, maps to vanilla AttributeTrack.validatePeriod
    public static DataResult<AttributeTrack<Value>> ValidatePeriod(AttributeTrack<Value> track, int periodTicks)
        => KeyframeTrack<object>.ValidatePeriod(track.ArgumentTrack, periodTicks)
            .Map(_ => track);
}
