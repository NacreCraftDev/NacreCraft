using NacreCraft.Codec;

namespace NacreCraft.Util;

//Keyframe one point of a keyframe track, maps to vanilla net.minecraft.util.Keyframe
//Ticks is the position inside the track, value is what the track holds from that tick on
public sealed record Keyframe<T>(int Ticks, T Value)
{
    public static Codec<Keyframe<T>> CreateCodec(Codec<T> valueCodec) => RecordCodecBuilder.Of2(
        ExtraCodecs.NonNegativeInt.FieldOf("ticks").ForGetter<Keyframe<T>, int>(keyframe => keyframe.Ticks),
        valueCodec.FieldOf("value").ForGetter<Keyframe<T>, T>(keyframe => keyframe.Value),
        (ticks, value) => new Keyframe<T>(ticks, value));
}
