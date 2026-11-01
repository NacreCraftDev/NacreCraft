using NacreCraft.Codec;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Items.Enchanting;

//LevelBasedValues enchantment level driven numbers, maps to vanilla LevelBasedValue
//A plain number in the json is the constant form; every other form carries a type field and is dispatched through
//ENCHANTMENT_LEVEL_BASED_VALUE_TYPE

//Constant a fixed value, vanilla encodes it as a bare number instead of a typed object
public sealed record Constant(float Value) : NacreCraft.Registry.LevelBasedValue
{
    public float Calculate(int level) => Value;
}

//Linear base plus a step for every level above the first
public sealed record Linear(float Base, float PerLevelAboveFirst) : NacreCraft.Registry.LevelBasedValue
{
    public static readonly MapCodec<Linear> Codec =
        RecordCodecBuilder.Of2<Linear, float, float>(
            Codecs.Float.FieldOf("base").ForGetter<Linear, float>(value => value.Base),
            Codecs.Float.FieldOf("per_level_above_first").ForGetter<Linear, float>(value => value.PerLevelAboveFirst),
            (baseValue, perLevel) => new Linear(baseValue, perLevel));

    public float Calculate(int level) => Base + PerLevelAboveFirst * (level - 1);
}

//Clamped keeps another value inside min and max
public sealed record Clamped(NacreCraft.Registry.LevelBasedValue Value, float Min, float Max)
    : NacreCraft.Registry.LevelBasedValue
{
    public static readonly MapCodec<Clamped> Codec =
        RecordCodecBuilder.Of3<Clamped, NacreCraft.Registry.LevelBasedValue, float, float>(
            LevelBasedValueCodec.Instance.FieldOf("value")
                .ForGetter<Clamped, NacreCraft.Registry.LevelBasedValue>(value => value.Value),
            Codecs.Float.FieldOf("min").ForGetter<Clamped, float>(value => value.Min),
            Codecs.Float.FieldOf("max").ForGetter<Clamped, float>(value => value.Max),
            (inner, min, max) => new Clamped(inner, min, max));

    public float Calculate(int level) => Math.Clamp(Value.Calculate(level), Min, Max);
}

//Fraction divides one value by another
public sealed record Fraction(NacreCraft.Registry.LevelBasedValue Numerator,
    NacreCraft.Registry.LevelBasedValue Denominator) : NacreCraft.Registry.LevelBasedValue
{
    public static readonly MapCodec<Fraction> Codec =
        RecordCodecBuilder.Of2<Fraction, NacreCraft.Registry.LevelBasedValue, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("numerator")
                .ForGetter<Fraction, NacreCraft.Registry.LevelBasedValue>(value => value.Numerator),
            LevelBasedValueCodec.Instance.FieldOf("denominator")
                .ForGetter<Fraction, NacreCraft.Registry.LevelBasedValue>(value => value.Denominator),
            (numerator, denominator) => new Fraction(numerator, denominator));

    public float Calculate(int level) => Numerator.Calculate(level) / Denominator.Calculate(level);
}

//LevelsSquared added plus the squared level
public sealed record LevelsSquared(float Added) : NacreCraft.Registry.LevelBasedValue
{
    public static readonly MapCodec<LevelsSquared> Codec =
        RecordCodecBuilder.Of1<LevelsSquared, float>(
            Codecs.Float.FieldOf("added").ForGetter<LevelsSquared, float>(value => value.Added),
            added => new LevelsSquared(added));

    public float Calculate(int level) => Added + level * level;
}

//Exponent raises one value to the power of another
public sealed record Exponent(NacreCraft.Registry.LevelBasedValue Base,
    NacreCraft.Registry.LevelBasedValue Power) : NacreCraft.Registry.LevelBasedValue
{
    public static readonly MapCodec<Exponent> Codec =
        RecordCodecBuilder.Of2<Exponent, NacreCraft.Registry.LevelBasedValue, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("base")
                .ForGetter<Exponent, NacreCraft.Registry.LevelBasedValue>(value => value.Base),
            LevelBasedValueCodec.Instance.FieldOf("power")
                .ForGetter<Exponent, NacreCraft.Registry.LevelBasedValue>(value => value.Power),
            (baseValue, power) => new Exponent(baseValue, power));

    public float Calculate(int level) => MathF.Pow(Base.Calculate(level), Power.Calculate(level));
}

//Lookup picks the value for the level, falling back when the level is outside the list
public sealed record Lookup(IReadOnlyList<float> Values, NacreCraft.Registry.LevelBasedValue Fallback)
    : NacreCraft.Registry.LevelBasedValue
{
    public static readonly MapCodec<Lookup> Codec =
        RecordCodecBuilder.Of2<Lookup, IReadOnlyList<float>, NacreCraft.Registry.LevelBasedValue>(
            Codecs.Float.ListOf().FieldOf("values")
                .ForGetter<Lookup, IReadOnlyList<float>>(value => value.Values),
            LevelBasedValueCodec.Instance.FieldOf("fallback")
                .ForGetter<Lookup, NacreCraft.Registry.LevelBasedValue>(value => value.Fallback),
            (values, fallback) => new Lookup(values, fallback));

    public float Calculate(int level)
        => level >= 1 && level <= Values.Count ? Values[level - 1] : Fallback.Calculate(level);
}

//LevelBasedValueCodec dispatch codec, maps to vanilla LevelBasedValue.CODEC
//A bare number decodes as the constant form, anything else is looked up by its type field
public sealed class LevelBasedValueCodec : AbstractMapCodec<NacreCraft.Registry.LevelBasedValue>
{
    public static readonly LevelBasedValueCodec Instance = new();

    public override DataResult<NacreCraft.Registry.LevelBasedValue> Parse<U>(DynamicOps<U> ops, U input)
    {
        var number = ops.GetNumberValue(input);
        if (number.Result().IsPresent)
            return DataResult<NacreCraft.Registry.LevelBasedValue>.Success(new Constant((float)number.GetOrThrow()));
        return base.Parse(ops, input);
    }

    public override DataResult<NacreCraft.Registry.LevelBasedValue> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var typeTag = input.Get("type");
        if (!typeTag.IsPresent)
            return DataResult<NacreCraft.Registry.LevelBasedValue>.Error(
                () => "level based value is missing the type field");
        var typeText = ops.GetStringValue(typeTag.Get());
        if (!typeText.Result().IsPresent)
            return DataResult<NacreCraft.Registry.LevelBasedValue>.Error(
                () => "level based value type is not a string");
        var id = Identifier.TryParse(typeText.GetOrThrow());
        if (id is null)
            return DataResult<NacreCraft.Registry.LevelBasedValue>.Error(
                () => $"invalid level based value type {typeText.GetOrThrow()}");
        var codec = BuiltInRegistries.ENCHANTMENT_LEVEL_BASED_VALUE_TYPE.GetValue(id.Value);
        if (codec is null)
            return DataResult<NacreCraft.Registry.LevelBasedValue>.Error(
                () => $"unknown level based value type {typeText.GetOrThrow()}");
        return codec.Decode(ops, input);
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, NacreCraft.Registry.LevelBasedValue value,
        RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding level based values is not supported yet");
}

//LevelBasedValues registers every form, maps to vanilla LevelBasedValue.bootstrap
public static class LevelBasedValues
{
    private static bool _registered;

    public static void Bootstrap()
    {
        if (_registered) return;
        _registered = true;
        Register("clamped", Box(Clamped.Codec));
        Register("fraction", Box(Fraction.Codec));
        Register("levels_squared", Box(LevelsSquared.Codec));
        Register("linear", Box(Linear.Codec));
        Register("exponent", Box(Exponent.Codec));
        Register("lookup", Box(Lookup.Codec));
    }

    //Box widens a concrete map codec to the interface the registry stores, MapCodec is invariant
    private static MapCodec<NacreCraft.Registry.LevelBasedValue> Box<T>(MapCodec<T> codec)
        where T : class, NacreCraft.Registry.LevelBasedValue
        => codec.Xmap<T, NacreCraft.Registry.LevelBasedValue>(value => value, value => (T)value);

    private static void Register(string id, MapCodec<NacreCraft.Registry.LevelBasedValue> codec)
        => Registry<MapCodec<NacreCraft.Registry.LevelBasedValue>>.Register(
            BuiltInRegistries.ENCHANTMENT_LEVEL_BASED_VALUE_TYPE,
            ResourceKey<MapCodec<NacreCraft.Registry.LevelBasedValue>>.Create(
                Registries.ENCHANTMENT_LEVEL_BASED_VALUE_TYPE, Identifier.WithDefaultNamespace(id)),
            codec);
}
