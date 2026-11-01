using NacreCraft.Codec;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Items.Enchanting;

//EnchantmentValueEffects value effects, maps to vanilla net.minecraft.world.item.enchantment.effects
//A value effect takes the current number, the enchantment level and a random source and returns the new number

//AddValue adds a level based amount
public sealed record AddValue(NacreCraft.Registry.LevelBasedValue Value) : NacreCraft.Registry.EnchantmentValueEffect
{
    public static readonly MapCodec<AddValue> Codec =
        RecordCodecBuilder.Of1<AddValue, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("value")
                .ForGetter<AddValue, NacreCraft.Registry.LevelBasedValue>(value => value.Value),
            value => new AddValue(value));

    public float Process(int level, RandomSource random, float inputValue)
        => inputValue + Value.Calculate(level);
}

//MultiplyValue scales by a level based factor
public sealed record MultiplyValue(NacreCraft.Registry.LevelBasedValue Factor)
    : NacreCraft.Registry.EnchantmentValueEffect
{
    public static readonly MapCodec<MultiplyValue> Codec =
        RecordCodecBuilder.Of1<MultiplyValue, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("factor")
                .ForGetter<MultiplyValue, NacreCraft.Registry.LevelBasedValue>(value => value.Factor),
            factor => new MultiplyValue(factor));

    public float Process(int level, RandomSource random, float inputValue)
        => inputValue * Factor.Calculate(level);
}

//SetValue replaces the number outright
public sealed record SetValue(NacreCraft.Registry.LevelBasedValue Value) : NacreCraft.Registry.EnchantmentValueEffect
{
    public static readonly MapCodec<SetValue> Codec =
        RecordCodecBuilder.Of1<SetValue, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("value")
                .ForGetter<SetValue, NacreCraft.Registry.LevelBasedValue>(value => value.Value),
            value => new SetValue(value));

    public float Process(int level, RandomSource random, float inputValue) => Value.Calculate(level);
}

//ScaleExponentially multiplies by base raised to exponent, both level based
public sealed record ScaleExponentially(NacreCraft.Registry.LevelBasedValue Base,
    NacreCraft.Registry.LevelBasedValue Exponent) : NacreCraft.Registry.EnchantmentValueEffect
{
    public static readonly MapCodec<ScaleExponentially> Codec =
        RecordCodecBuilder.Of2<ScaleExponentially, NacreCraft.Registry.LevelBasedValue,
            NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("base")
                .ForGetter<ScaleExponentially, NacreCraft.Registry.LevelBasedValue>(value => value.Base),
            LevelBasedValueCodec.Instance.FieldOf("exponent")
                .ForGetter<ScaleExponentially, NacreCraft.Registry.LevelBasedValue>(value => value.Exponent),
            (baseValue, exponent) => new ScaleExponentially(baseValue, exponent));

    public float Process(int level, RandomSource random, float inputValue)
        => inputValue * MathF.Pow(Base.Calculate(level), Exponent.Calculate(level));
}

//RemoveBinomial drops a binomially distributed count from the number
//Large inputs use a normal approximation, small ones roll once per unit, matching vanilla
public sealed record RemoveBinomial(NacreCraft.Registry.LevelBasedValue Chance)
    : NacreCraft.Registry.EnchantmentValueEffect
{
    public static readonly MapCodec<RemoveBinomial> Codec =
        RecordCodecBuilder.Of1<RemoveBinomial, NacreCraft.Registry.LevelBasedValue>(
            LevelBasedValueCodec.Instance.FieldOf("chance")
                .ForGetter<RemoveBinomial, NacreCraft.Registry.LevelBasedValue>(value => value.Chance),
            chance => new RemoveBinomial(chance));

    public float Process(int level, RandomSource random, float inputValue)
    {
        var chance = Chance.Calculate(level);
        var drop = 0;
        if (!(inputValue <= 128f || inputValue * chance < 20f || inputValue * (1f - chance) < 20f))
        {
            var mean = Math.Floor(inputValue * chance);
            var sigma = Math.Sqrt(inputValue * chance * (1f - chance));
            drop = (int)Math.Round(mean + random.NextGaussian() * sigma);
            drop = (int)Math.Clamp((long)drop, 0, (int)inputValue);
        }
        else
        {
            for (var index = 0; index < inputValue; index++)
                if (random.NextFloat() < chance) drop++;
        }
        return inputValue - drop;
    }
}

//ValueEffects applies a list of effects in order, maps to vanilla AllOf.ValueEffects
public sealed record ValueEffects(IReadOnlyList<NacreCraft.Registry.EnchantmentValueEffect> Effects)
    : NacreCraft.Registry.EnchantmentValueEffect
{
    public static readonly MapCodec<ValueEffects> Codec =
        RecordCodecBuilder.Of1<ValueEffects, IReadOnlyList<NacreCraft.Registry.EnchantmentValueEffect>>(
            EnchantmentValueEffectCodec.Instance.ListOf().FieldOf("effects")
                .ForGetter<ValueEffects, IReadOnlyList<NacreCraft.Registry.EnchantmentValueEffect>>(
                    value => value.Effects),
            effects => new ValueEffects(effects));

    public float Process(int level, RandomSource random, float value)
    {
        foreach (var effect in Effects) value = effect.Process(level, random, value);
        return value;
    }
}

//EnchantmentValueEffectCodec dispatch codec, maps to vanilla EnchantmentValueEffect.CODEC
//Value effects are always typed objects, there is no bare number form
public sealed class EnchantmentValueEffectCodec : AbstractMapCodec<NacreCraft.Registry.EnchantmentValueEffect>
{
    public static readonly EnchantmentValueEffectCodec Instance = new();

    public override DataResult<NacreCraft.Registry.EnchantmentValueEffect> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var typeTag = input.Get("type");
        if (!typeTag.IsPresent)
            return DataResult<NacreCraft.Registry.EnchantmentValueEffect>.Error(
                () => "value effect is missing the type field");
        var typeText = ops.GetStringValue(typeTag.Get());
        if (!typeText.Result().IsPresent)
            return DataResult<NacreCraft.Registry.EnchantmentValueEffect>.Error(
                () => "value effect type is not a string");
        var id = Identifier.TryParse(typeText.GetOrThrow());
        if (id is null)
            return DataResult<NacreCraft.Registry.EnchantmentValueEffect>.Error(
                () => $"invalid value effect type {typeText.GetOrThrow()}");
        var codec = BuiltInRegistries.ENCHANTMENT_VALUE_EFFECT_TYPE.GetValue(id.Value);
        if (codec is null)
            return DataResult<NacreCraft.Registry.EnchantmentValueEffect>.Error(
                () => $"unknown value effect type {typeText.GetOrThrow()}");
        return codec.Decode(ops, input);
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, NacreCraft.Registry.EnchantmentValueEffect value,
        RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding value effects is not supported yet");
}

//EnchantmentValueEffects registers every value effect, maps to vanilla EnchantmentValueEffect.bootstrap
public static class EnchantmentValueEffects
{
    private static bool _registered;

    public static void Bootstrap()
    {
        if (_registered) return;
        _registered = true;
        Register("add", Box(AddValue.Codec));
        Register("all_of", Box(ValueEffects.Codec));
        Register("multiply", Box(MultiplyValue.Codec));
        Register("remove_binomial", Box(RemoveBinomial.Codec));
        Register("exponential", Box(ScaleExponentially.Codec));
        Register("set", Box(SetValue.Codec));
    }

    //Box widens a concrete map codec to the interface the registry stores, MapCodec is invariant
    private static MapCodec<NacreCraft.Registry.EnchantmentValueEffect> Box<T>(MapCodec<T> codec)
        where T : class, NacreCraft.Registry.EnchantmentValueEffect
        => codec.Xmap<T, NacreCraft.Registry.EnchantmentValueEffect>(value => value, value => (T)value);

    private static void Register(string id, MapCodec<NacreCraft.Registry.EnchantmentValueEffect> codec)
        => Registry<MapCodec<NacreCraft.Registry.EnchantmentValueEffect>>.Register(
            BuiltInRegistries.ENCHANTMENT_VALUE_EFFECT_TYPE,
            ResourceKey<MapCodec<NacreCraft.Registry.EnchantmentValueEffect>>.Create(
                Registries.ENCHANTMENT_VALUE_EFFECT_TYPE, Identifier.WithDefaultNamespace(id)),
            codec);
}
