using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Util;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen;

//FloatProvider float provider system, maps to vanilla net.minecraft.util.valueproviders
//FloatProviders.Codec is either a bare float or dispatch by the type field
public interface SampledFloat
{
    float Sample(RandomSource random);
}

public abstract class FloatProvider : NacreCraft.Registry.FloatProvider, SampledFloat
{
    //Sample draws a float from the random source, maps to vanilla sample
    public abstract float Sample(RandomSource random);

    //Min lower bound, maps to vanilla min
    public abstract float Min { get; }

    //Max upper bound, maps to vanilla max
    public abstract float Max { get; }
}

//ConstantFloat constant float provider, maps to vanilla ConstantFloat
public sealed class ConstantFloat : FloatProvider
{
    //Zero constant-zero singleton, maps to vanilla ZERO
    public static readonly ConstantFloat Zero = new(0f);

    public static readonly MapCodec<NacreCraft.Registry.FloatProvider> MAP_CODEC =
        RecordCodecBuilder.Of1<NacreCraft.Registry.FloatProvider, float>(
            Codecs.Float.FieldOf("value").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((ConstantFloat)f).Value),
            value => ConstantFloat.Of(value));

    public float Value { get; }

    public ConstantFloat(float value) => Value = value;

    //Of reuses the singleton for zero, maps to vanilla of
    public static ConstantFloat Of(float value) => value == 0f ? Zero : new ConstantFloat(value);

    public override float Sample(RandomSource random) => Value;

    public override float Min => Value;

    public override float Max => Value;

    public override string ToString() => Value.ToString();
}

//UniformFloat uniform distribution float provider, maps to vanilla UniformFloat
public sealed class UniformFloat : FloatProvider
{
    public static readonly MapCodec<NacreCraft.Registry.FloatProvider> MAP_CODEC =
        RecordCodecBuilder.Of2<NacreCraft.Registry.FloatProvider, float, float>(
                Codecs.Float.FieldOf("min_inclusive").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((UniformFloat)f).Min),
                Codecs.Float.FieldOf("max_exclusive").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((UniformFloat)f).Max),
                (min, max) => Of(min, max))
            .ComapFlatMap(Validate, value => value);

    private readonly float _min;
    private readonly float _max;

    public UniformFloat(float min, float max)
    {
        _min = min;
        _max = max;
    }

    //Of requires max to be strictly greater than min, maps to vanilla of
    public static UniformFloat Of(float min, float max)
        => max <= min ? throw new ArgumentException("Max must exceed min") : new UniformFloat(min, max);

    private static DataResult<NacreCraft.Registry.FloatProvider> Validate(NacreCraft.Registry.FloatProvider value)
    {
        if (value is UniformFloat uniform && uniform.Max <= uniform.Min)
            return DataResult<NacreCraft.Registry.FloatProvider>.Error(
                () => $"Max must be larger than min, min: {uniform.Min}, max: {uniform.Max}");
        return DataResult<NacreCraft.Registry.FloatProvider>.Success(value);
    }

    public override float Sample(RandomSource random) => Mth.RandomBetween(random, _min, _max);

    public override float Min => _min;

    public override float Max => _max;

    public override string ToString() => $"[{_min}-{_max}]";
}

//TrapezoidFloat trapezoid distribution float provider, maps to vanilla TrapezoidFloat
public sealed class TrapezoidFloat : FloatProvider
{
    public static readonly MapCodec<NacreCraft.Registry.FloatProvider> MAP_CODEC =
        RecordCodecBuilder.Of3<NacreCraft.Registry.FloatProvider, float, float, float>(
                Codecs.Float.FieldOf("min").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((TrapezoidFloat)f).Min),
                Codecs.Float.FieldOf("max").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((TrapezoidFloat)f).Max),
                Codecs.Float.FieldOf("plateau").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((TrapezoidFloat)f).Plateau),
                (min, max, plateau) => Of(min, max, plateau))
            .ComapFlatMap(Validate, value => value);

    private readonly float _min;
    private readonly float _max;
    private readonly float _plateau;

    public TrapezoidFloat(float min, float max, float plateau)
    {
        _min = min;
        _max = max;
        _plateau = plateau;
    }

    public float Plateau => _plateau;

    public static TrapezoidFloat Of(float min, float max, float plateau) => new(min, max, plateau);

    private static DataResult<NacreCraft.Registry.FloatProvider> Validate(NacreCraft.Registry.FloatProvider value)
    {
        if (value is not TrapezoidFloat trapezoid)
            return DataResult<NacreCraft.Registry.FloatProvider>.Success(value);
        if (trapezoid.Max < trapezoid.Min)
            return DataResult<NacreCraft.Registry.FloatProvider>.Error(
                () => $"Max must be larger than min: [{trapezoid.Min}, {trapezoid.Max}]");
        if (trapezoid.Plateau > trapezoid.Max - trapezoid.Min)
            return DataResult<NacreCraft.Registry.FloatProvider>.Error(
                () => $"Plateau can at most be the full span: [{trapezoid.Min}, {trapezoid.Max}]");
        return DataResult<NacreCraft.Registry.FloatProvider>.Success(value);
    }

    //Sample adds two independent draws, maps to vanilla sample; the call order must not change
    public override float Sample(RandomSource random)
    {
        var range = _max - _min;
        var plateauStart = (range - _plateau) / 2f;
        var plateauEnd = range - plateauStart;
        return _min + (random.NextFloat() * plateauEnd) + (random.NextFloat() * plateauStart);
    }

    public override float Min => _min;

    public override float Max => _max;

    public override string ToString() => $"trapezoid({_plateau}) in [{_min}-{_max}]";
}

//ClampedNormalFloat normal distribution then clamped float provider, maps to vanilla ClampedNormalFloat
public sealed class ClampedNormalFloat : FloatProvider
{
    public static readonly MapCodec<NacreCraft.Registry.FloatProvider> MAP_CODEC =
        RecordCodecBuilder.Of4<NacreCraft.Registry.FloatProvider, float, float, float, float>(
                Codecs.Float.FieldOf("mean").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((ClampedNormalFloat)f).Mean),
                Codecs.Float.FieldOf("deviation").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((ClampedNormalFloat)f).Deviation),
                Codecs.Float.FieldOf("min").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((ClampedNormalFloat)f).Min),
                Codecs.Float.FieldOf("max").ForGetter<NacreCraft.Registry.FloatProvider, float>(f => ((ClampedNormalFloat)f).Max),
                (mean, deviation, min, max) => Of(mean, deviation, min, max))
            .ComapFlatMap(Validate, value => value);

    public float Mean { get; }
    public float Deviation { get; }
    public override float Min { get; }
    public override float Max { get; }

    public ClampedNormalFloat(float mean, float deviation, float min, float max)
    {
        Mean = mean;
        Deviation = deviation;
        Min = min;
        Max = max;
    }

    public static ClampedNormalFloat Of(float mean, float deviation, float min, float max)
        => new(mean, deviation, min, max);

    private static DataResult<NacreCraft.Registry.FloatProvider> Validate(NacreCraft.Registry.FloatProvider value)
    {
        if (value is ClampedNormalFloat normal && normal.Max < normal.Min)
            return DataResult<NacreCraft.Registry.FloatProvider>.Error(
                () => $"Max must be larger than min: [{normal.Min}, {normal.Max}]");
        return DataResult<NacreCraft.Registry.FloatProvider>.Success(value);
    }

    public override float Sample(RandomSource random) => Sample(random, Mean, Deviation, Min, Max);

    //Sample clamps the normal draw into the range, maps to the vanilla static sample
    public static float Sample(RandomSource random, float mean, float deviation, float min, float max)
        => Mth.Clamp(Mth.Normal(random, mean, deviation), min, max);

    public override string ToString() => $"normal({Mean}, {Deviation}) in [{Min}-{Max}]";
}

//MultipliedFloats multiplies several sampled floats, maps to vanilla MultipliedFloats
public sealed class MultipliedFloats : SampledFloat
{
    private readonly SampledFloat[] _values;

    public MultipliedFloats(params SampledFloat[] values) => _values = values;

    public float Sample(RandomSource random)
    {
        var result = 1.0f;
        foreach (var value in _values) result *= value.Sample(random);
        return result;
    }

    public override string ToString() => "MultipliedFloats[" + string.Join(", ", _values.Select(v => v.ToString())) + "]";
}

//FloatProviders float provider codec entry point, maps to vanilla FloatProviders
public static class FloatProviders
{
    //Codec either a bare float or an object with type, maps to vanilla CODEC
    public static readonly Codec<FloatProvider> Codec = new FloatProviderCodec();

    //ValidatedCodec restricts the provider range, maps to vanilla FloatProviders.codec(min)
    public static Codec<FloatProvider> ValidatedCodec(float minValue)
        => new RangeValidatedFloatProviderCodec(Codec, minValue, float.MaxValue);

    //ValidatedCodec restricts the provider range, maps to vanilla FloatProviders.codec(min, max)
    public static Codec<FloatProvider> ValidatedCodec(float minValue, float maxValue)
        => new RangeValidatedFloatProviderCodec(Codec, minValue, maxValue);

    //Bootstrap registers the provider descriptors in vanilla order, maps to vanilla FloatProviders.bootstrap
    public static void Bootstrap()
    {
        Register("constant", ConstantFloat.MAP_CODEC);
        Register("uniform", UniformFloat.MAP_CODEC);
        Register("clamped_normal", ClampedNormalFloat.MAP_CODEC);
        Register("trapezoid", TrapezoidFloat.MAP_CODEC);
    }

    private static void Register(string id, MapCodec<NacreCraft.Registry.FloatProvider> codec)
        => Registry<MapCodec<NacreCraft.Registry.FloatProvider>>.Register(
            BuiltInRegistries.FLOAT_PROVIDER_TYPE, Identifier.WithDefaultNamespace(id), codec);
}

//RangeValidatedFloatProviderCodec rejects providers whose range leaves the accepted bounds, maps to the vanilla codec(min[, max])
internal sealed class RangeValidatedFloatProviderCodec : ScalarCodec<FloatProvider>
{
    private readonly Codec<FloatProvider> _source;
    private readonly float _min;
    private readonly float _max;

    public RangeValidatedFloatProviderCodec(Codec<FloatProvider> source, float min, float max)
    {
        _source = source;
        _min = min;
        _max = max;
    }

    public override DataResult<FloatProvider> Parse<U>(DynamicOps<U> ops, U input)
        => _source.Parse(ops, input).FlatMap(value =>
        {
            if (value.Min < _min)
                return DataResult<FloatProvider>.Error(() => $"Value provider too low: {_min} [{value.Min}-{value.Max}]");
            if (value.Max > _max)
                return DataResult<FloatProvider>.Error(() => $"Value provider too high: {_max} [{value.Min}-{value.Max}]");
            return DataResult<FloatProvider>.Success(value);
        });

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, FloatProvider value)
        => _source.EncodeStart(ops, value);
}

//FloatProviderCodec float provider codec
//A bare number decodes to ConstantFloat; an object dispatches by the type field
internal sealed class FloatProviderCodec : ScalarCodec<FloatProvider>
{
    public override DataResult<FloatProvider> Parse<U>(DynamicOps<U> ops, U input)
    {
        var number = ops.GetNumberValue(input);
        if (number.Result().IsPresent)
            return DataResult<FloatProvider>.Success(ConstantFloat.Of((float)number.GetOrThrow()));
        return ops.GetMap(input).FlatMap(map => DecodeProvider(ops, map));
    }

    private static DataResult<FloatProvider> DecodeProvider<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var type = ReadType(ops, input);
        if (!type.IsPresent)
            return DataResult<FloatProvider>.Error(() => "missing type field for FloatProvider");
        switch (PathOf(type.Get()))
        {
            case "constant":
            {
                var value = ReadFloat(ops, input, "value");
                return value is null
                    ? DataResult<FloatProvider>.Error(() => "constant float requires numeric value")
                    : DataResult<FloatProvider>.Success(ConstantFloat.Of(value.Value));
            }
            case "uniform":
            {
                var min = ReadFloat(ops, input, "min_inclusive");
                var max = ReadFloat(ops, input, "max_exclusive");
                if (min is null || max is null)
                    return DataResult<FloatProvider>.Error(() => "uniform float requires min_inclusive and max_exclusive");
                if (max <= min)
                    return DataResult<FloatProvider>.Error(() => $"Max must be larger than min, min: {min}, max: {max}");
                return DataResult<FloatProvider>.Success(new UniformFloat(min.Value, max.Value));
            }
            case "trapezoid":
            {
                var min = ReadFloat(ops, input, "min");
                var max = ReadFloat(ops, input, "max");
                var plateau = ReadFloat(ops, input, "plateau");
                if (min is null || max is null || plateau is null)
                    return DataResult<FloatProvider>.Error(() => "trapezoid float requires min/max/plateau");
                if (max < min)
                    return DataResult<FloatProvider>.Error(() => $"Max must be larger than min: [{min}, {max}]");
                if (plateau > max - min)
                    return DataResult<FloatProvider>.Error(() => $"Plateau can at most be the full span: [{min}, {max}]");
                return DataResult<FloatProvider>.Success(new TrapezoidFloat(min.Value, max.Value, plateau.Value));
            }
            case "clamped_normal":
            {
                var mean = ReadFloat(ops, input, "mean");
                var deviation = ReadFloat(ops, input, "deviation");
                var min = ReadFloat(ops, input, "min");
                var max = ReadFloat(ops, input, "max");
                if (mean is null || deviation is null || min is null || max is null)
                    return DataResult<FloatProvider>.Error(() => "clamped_normal float requires mean/deviation/min/max");
                if (max < min)
                    return DataResult<FloatProvider>.Error(() => $"Max must be larger than min: [{min}, {max}]");
                return DataResult<FloatProvider>.Success(new ClampedNormalFloat(mean.Value, deviation.Value, min.Value, max.Value));
            }
            default:
                return DataResult<FloatProvider>.Error(() => $"unknown float provider type: {type.Get()}");
        }
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, FloatProvider value)
    {
        //Constants encode as a bare float, keeping the same direction as vanilla either
        if (value is ConstantFloat constant)
            return DataResult<U>.Success(ops.CreateFloat(constant.Value));
        var builder = ops.MapBuilder();
        switch (value)
        {
            case UniformFloat uniform:
                builder.Add("type", ops.CreateString(Identifier.WithDefaultNamespace("uniform").ToString()));
                builder.Add("min_inclusive", ops.CreateFloat(uniform.Min));
                builder.Add("max_exclusive", ops.CreateFloat(uniform.Max));
                break;
            case TrapezoidFloat trapezoid:
                builder.Add("type", ops.CreateString(Identifier.WithDefaultNamespace("trapezoid").ToString()));
                builder.Add("min", ops.CreateFloat(trapezoid.Min));
                builder.Add("max", ops.CreateFloat(trapezoid.Max));
                builder.Add("plateau", ops.CreateFloat(trapezoid.Plateau));
                break;
            case ClampedNormalFloat normal:
                builder.Add("type", ops.CreateString(Identifier.WithDefaultNamespace("clamped_normal").ToString()));
                builder.Add("mean", ops.CreateFloat(normal.Mean));
                builder.Add("deviation", ops.CreateFloat(normal.Deviation));
                builder.Add("min", ops.CreateFloat(normal.Min));
                builder.Add("max", ops.CreateFloat(normal.Max));
                break;
            default:
                return DataResult<U>.Error(() => $"unsupported FloatProvider: {value?.GetType().Name}");
        }
        return builder.Build(ops.Empty());
    }

    //ReadType reads the type string field; missing or non-string returns empty
    private static Optional<string> ReadType<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var tag = input.Get("type");
        if (!tag.IsPresent) return Optional<string>.Empty();
        var text = ops.GetStringValue(tag.Get());
        return text.Result().IsPresent ? Optional<string>.Of(text.GetOrThrow()) : Optional<string>.Empty();
    }

    //ReadFloat reads a float field; missing or non-number returns null
    private static float? ReadFloat<U>(DynamicOps<U> ops, MapLike<U> input, string name)
    {
        var tag = input.Get(name);
        if (!tag.IsPresent) return null;
        var value = ops.GetNumberValue(tag.Get());
        return value.Result().IsPresent ? (float)value.GetOrThrow() : null;
    }

    //PathOf takes only the path segment of a namespaced type
    private static string PathOf(string type)
    {
        var id = Identifier.TryParse(type);
        return id is not null ? id.Value.Path : type;
    }
}
