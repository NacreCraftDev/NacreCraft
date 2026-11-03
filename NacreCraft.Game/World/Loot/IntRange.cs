using NacreCraft.Codec;

namespace NacreCraft.Game.World.Loot;

//IntRange a bounded or unbounded integer range whose ends are number providers, maps to vanilla IntRange
public sealed class IntRange
{
    //Codec persistence codec, a bare integer is the exact range and an object carries min/max, maps to vanilla CODEC
    public static readonly Codec<IntRange> Codec = new IntRangeCodec();

    public IntRange(NumberProvider? min, NumberProvider? max)
    {
        Min = min;
        Max = max;
    }

    public NumberProvider? Min { get; }
    public NumberProvider? Max { get; }

    public static IntRange Exact(int value) => new(ConstantValue.Exactly(value), ConstantValue.Exactly(value));

    //Clamp pulls both ends then clamps the value, maps to vanilla clamp
    public int Clamp(LootContext context, int value)
    {
        if (Min is null && Max is null) return value;
        if (Min is null) return Math.Min(Max!.GetInt(context), value);
        if (Max is null) return Math.Max(Min.GetInt(context), value);
        return Math.Clamp(value, Min.GetInt(context), Max.GetInt(context));
    }
}

//IntRangeCodec reads a bare integer as the exact range and otherwise falls back to the min/max object, maps to
//vanilla IntRange.CODEC
internal sealed class IntRangeCodec : ScalarCodec<IntRange>
{
    private static readonly Codec<IntRange> RecordCodec = RecordCodecBuilder.Of2(
        NumberProviders.CODEC.OptionalFieldOf("min").ForGetter((IntRange range) => ToOptional(range.Min)),
        NumberProviders.CODEC.OptionalFieldOf("max").ForGetter((IntRange range) => ToOptional(range.Max)),
        (min, max) => new IntRange(min.IsPresent ? min.Get() : null, max.IsPresent ? max.Get() : null));

    public override DataResult<IntRange> Parse<U>(DynamicOps<U> ops, U input)
    {
        var number = ops.GetNumberValue(input);
        if (number.Result().IsPresent)
            return DataResult<IntRange>.Success(IntRange.Exact((int)number.GetOrThrow()));
        return RecordCodec.Parse(ops, input);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, IntRange value)
        => DataResult<U>.Error(() => "Integer ranges are not encodable");

    private static Optional<NumberProvider> ToOptional(NumberProvider? provider)
        => provider is null ? Optional<NumberProvider>.Empty() : Optional<NumberProvider>.Of(provider);
}
