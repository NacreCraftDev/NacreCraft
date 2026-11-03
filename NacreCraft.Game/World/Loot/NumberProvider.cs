using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Loot;

//NumberProvider a count source resolved once per loot roll, maps to vanilla NumberProvider
public interface NumberProvider
{
    float GetFloat(LootContext context);

    int GetInt(LootContext context) => (int)MathF.Round(GetFloat(context));

    //TypeId is the registry name the type field dispatches on
    Identifier TypeId { get; }

    MapCodec<NumberProvider> Codec();
}

//ConstantValue a fixed count, maps to vanilla ConstantValue
public sealed record ConstantValue(float Value) : NumberProvider
{
    public static readonly MapCodec<NumberProvider> MAP_CODEC = RecordCodecBuilder.Of1<NumberProvider, float>(
        Codecs.Float.FieldOf("value").ForGetter<NumberProvider, float>(p => ((ConstantValue)p).Value),
        value => new ConstantValue(value));

    public Identifier TypeId => Identifier.WithDefaultNamespace("constant");

    public float GetFloat(LootContext context) => Value;

    public MapCodec<NumberProvider> Codec() => MAP_CODEC;

    public static ConstantValue Exactly(float value) => new(value);
}

//UniformGenerator a count drawn uniformly from a range, maps to vanilla UniformGenerator
public sealed record UniformGenerator(float Min, float Max) : NumberProvider
{
    public static readonly MapCodec<NumberProvider> MAP_CODEC = RecordCodecBuilder.Of2<NumberProvider, float, float>(
        Codecs.Float.FieldOf("min").ForGetter<NumberProvider, float>(p => ((UniformGenerator)p).Min),
        Codecs.Float.FieldOf("max").ForGetter<NumberProvider, float>(p => ((UniformGenerator)p).Max),
        (min, max) => new UniformGenerator(min, max));

    public Identifier TypeId => Identifier.WithDefaultNamespace("uniform");

    public float GetFloat(LootContext context) => context.Random.NextFloat() * (Max - Min) + Min;

    public MapCodec<NumberProvider> Codec() => MAP_CODEC;
}

//EnchantmentLevelProvider a count computed from the enchantment level, maps to vanilla EnchantmentLevelProvider
public sealed record EnchantmentLevelProvider(NacreCraft.Registry.LevelBasedValue Amount) : NumberProvider
{
    public static readonly MapCodec<NumberProvider> MAP_CODEC =
        RecordCodecBuilder.Of1<NumberProvider, NacreCraft.Registry.LevelBasedValue>(
            NacreCraft.Game.World.Items.Enchanting.LevelBasedValueCodec.Instance.FieldOf("amount")
                .ForGetter<NumberProvider, NacreCraft.Registry.LevelBasedValue>(
                    p => ((EnchantmentLevelProvider)p).Amount),
            amount => new EnchantmentLevelProvider(amount));

    public Identifier TypeId => Identifier.WithDefaultNamespace("enchantment_level");

    public float GetFloat(LootContext context)
        => Amount.Calculate(context.GetParameter(LootContextParams.EnchantmentLevel));

    public MapCodec<NumberProvider> Codec() => MAP_CODEC;
}

//SumProvider adds the values of every summand, maps to vanilla Sum
public sealed record SumProvider(IReadOnlyList<NumberProvider> Summands) : NumberProvider
{
    public static readonly MapCodec<NumberProvider> MAP_CODEC =
        RecordCodecBuilder.Of1<NumberProvider, IReadOnlyList<NumberProvider>>(
            NumberProviders.CODEC.ListOf().FieldOf("summands")
                .ForGetter<NumberProvider, IReadOnlyList<NumberProvider>>(p => ((SumProvider)p).Summands),
            summands => new SumProvider(summands));

    public Identifier TypeId => Identifier.WithDefaultNamespace("sum");

    public float GetFloat(LootContext context)
    {
        var value = 0f;
        foreach (var summand in Summands) value += summand.GetFloat(context);
        return value;
    }

    public MapCodec<NumberProvider> Codec() => MAP_CODEC;
}

//BinomialDistributionGenerator draws the number of successes out of n with probability p, maps to vanilla
//BinomialDistributionGenerator
public sealed record BinomialDistributionGenerator(NumberProvider N, NumberProvider P) : NumberProvider
{
    public static readonly MapCodec<NumberProvider> MAP_CODEC =
        RecordCodecBuilder.Of2<NumberProvider, NumberProvider, NumberProvider>(
            NumberProviders.CODEC.FieldOf("n")
                .ForGetter<NumberProvider, NumberProvider>(p => ((BinomialDistributionGenerator)p).N),
            NumberProviders.CODEC.FieldOf("p")
                .ForGetter<NumberProvider, NumberProvider>(p => ((BinomialDistributionGenerator)p).P),
            (n, p) => new BinomialDistributionGenerator(n, p));

    public Identifier TypeId => Identifier.WithDefaultNamespace("binomial");

    public float GetFloat(LootContext context) => GetInt(context);

    public int GetInt(LootContext context)
    {
        var rounds = N.GetInt(context);
        var probability = P.GetFloat(context);
        var result = 0;
        for (var index = 0; index < rounds; index++)
            if (context.Random.NextFloat() < probability)
                result++;
        return result;
    }

    public MapCodec<NumberProvider> Codec() => MAP_CODEC;
}

//NumberProviders the number provider type registry plus the root codec, maps to vanilla NumberProviders
public static class NumberProviders
{
    private static readonly Dictionary<Identifier, MapCodec<NumberProvider>> Types = new();

    //TYPED_CODEC dispatches on the type field alone
    public static readonly Codec<NumberProvider> TYPED_CODEC =
        IdentifierCodec.Instance.Dispatch<NumberProvider, Identifier>("type", p => p.TypeId, Lookup);

    //CODEC also accepts a bare number, the shorthand vanilla writes for a constant count
    public static readonly Codec<NumberProvider> CODEC = new NumberProviderCodec();

    static NumberProviders()
    {
        Register("constant", ConstantValue.MAP_CODEC);
        Register("uniform", UniformGenerator.MAP_CODEC);
        Register("enchantment_level", EnchantmentLevelProvider.MAP_CODEC);
        Register("sum", SumProvider.MAP_CODEC);
        Register("binomial", BinomialDistributionGenerator.MAP_CODEC);
    }

    private static MapCodec<NumberProvider> Lookup(Identifier id)
        => Types.TryGetValue(id, out var codec)
            ? codec
            : throw new KeyNotFoundException($"Unknown number provider type: {id}");

    private static void Register(string name, MapCodec<NumberProvider> codec)
        => Types[Identifier.WithDefaultNamespace(name)] = codec;
}

//NumberProviderCodec reads a bare number as a constant and otherwise falls back to the typed form
internal sealed class NumberProviderCodec : ScalarCodec<NumberProvider>
{
    public override DataResult<NumberProvider> Parse<U>(DynamicOps<U> ops, U input)
    {
        var number = ops.GetNumberValue(input);
        if (number.Result().IsPresent)
            return DataResult<NumberProvider>.Success(ConstantValue.Exactly((float)number.GetOrThrow()));
        return NumberProviders.TYPED_CODEC.Parse(ops, input);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, NumberProvider value)
        => value is ConstantValue constant
            ? DataResult<U>.Success(ops.CreateFloat(constant.Value))
            : NumberProviders.TYPED_CODEC.EncodeStart(ops, value);
}
