using NacreCraft.Codec;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Registry;

//BiomeGenerationSettings biome generation settings, maps to vanilla BiomeGenerationSettings
//carvers maps to ConfiguredWorldCarver.LIST_CODEC and features maps to PlacedFeature.LIST_OF_LISTS_CODEC
//The outer features array is grouped by GenerationStep.Decoration index and the inner array holds the placed features referenced by this biome under that step
public sealed class BiomeGenerationSettings
{
    public static readonly BiomeGenerationSettings Empty = new(
        new DirectHolderSet<ConfiguredWorldCarver>(Array.Empty<Holder<ConfiguredWorldCarver>>()),
        Array.Empty<HolderSet<PlacedFeature>>());

    //FeaturesCodec codec for the outer/inner arrays; an empty inner array preserves the placeholder step
    public static readonly Codec<IReadOnlyList<HolderSet<PlacedFeature>>> FeaturesCodec =
        HolderSetCodecs.PlacedFeatureSet.ListOf();

    public static readonly Codec<BiomeGenerationSettings> Codec =
        RecordCodecBuilder.Of2<BiomeGenerationSettings, HolderSet<ConfiguredWorldCarver>, IReadOnlyList<HolderSet<PlacedFeature>>>(
            HolderSetCodecs.ConfiguredCarverSet.FieldOf("carvers")
                .ForGetter<BiomeGenerationSettings, HolderSet<ConfiguredWorldCarver>>(s => s.Carvers),
            FeaturesCodec.FieldOf("features").ForGetter<BiomeGenerationSettings, IReadOnlyList<HolderSet<PlacedFeature>>>(s => s.Features),
            (carvers, features) => new BiomeGenerationSettings(carvers, features));

    //Carvers carver set, bound to the configured carver registry
    public HolderSet<ConfiguredWorldCarver> Carvers { get; }

    //Features placed feature sets per step; the index is the GenerationStep.Decoration ordinal
    public IReadOnlyList<HolderSet<PlacedFeature>> Features { get; }

    public BiomeGenerationSettings(HolderSet<ConfiguredWorldCarver> carvers, IReadOnlyList<HolderSet<PlacedFeature>> features)
    {
        Carvers = carvers;
        Features = features;
    }

    //PlainBuilder builder over already resolved holders, maps to vanilla BiomeGenerationSettings.PlainBuilder
    public class PlainBuilder
    {
        private readonly List<Holder<ConfiguredWorldCarver>> _carvers = new();
        private readonly List<List<Holder<PlacedFeature>>> _features = new();

        public PlainBuilder AddFeature(int step, Holder<PlacedFeature> feature)
        {
            while (_features.Count <= step) _features.Add(new List<Holder<PlacedFeature>>());
            _features[step].Add(feature);
            return this;
        }

        public PlainBuilder AddCarver(Holder<ConfiguredWorldCarver> carver)
        {
            _carvers.Add(carver);
            return this;
        }

        public BiomeGenerationSettings Build()
            => new(new DirectHolderSet<ConfiguredWorldCarver>(_carvers),
                _features.Select(list => (HolderSet<PlacedFeature>)new DirectHolderSet<PlacedFeature>(list)).ToList());
    }

    //Builder the key-based builder the biome data classes use, maps to vanilla BiomeGenerationSettings.Builder
    //A key that is not registered yet resolves to an unbound placeholder holder, which the later register call binds
    public sealed class Builder : PlainBuilder
    {
        private readonly HolderLookup<PlacedFeature> _placedFeatures;
        private readonly HolderLookup<ConfiguredWorldCarver> _worldCarvers;

        public Builder(HolderLookup<PlacedFeature> placedFeatures, HolderLookup<ConfiguredWorldCarver> worldCarvers)
        {
            _placedFeatures = placedFeatures;
            _worldCarvers = worldCarvers;
        }

        public Builder AddFeature(int step, ResourceKey<PlacedFeature> feature)
            => (Builder)AddFeature(step, Resolve(_placedFeatures, feature));

        public Builder AddCarver(ResourceKey<ConfiguredWorldCarver> carver)
            => (Builder)AddCarver(Resolve(_worldCarvers, carver));

        //Resolve looks up the holder, creating a placeholder for a key that registers later
        private static Holder<T> Resolve<T>(HolderLookup<T> lookup, ResourceKey<T> key) where T : class
            => lookup is Registry<T> registry
                ? registry.GetOrCreateHolder(key.Identifier)
                : lookup.Get(key) ?? throw new InvalidOperationException($"Missing element: {key.Identifier}");
    }
}
