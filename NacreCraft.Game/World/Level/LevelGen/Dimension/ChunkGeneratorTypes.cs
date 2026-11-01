using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Level.LevelGen.Dimension;

//ChunkGeneratorTypes chunk generator codec types, maps to vanilla ChunkGenerators registration
//The registry is weakly typed MapCodec<object> to keep the cross-layer signature, so each entry is mapped on the way in
//and out; touching any static field here is what performs the registration
public static class ChunkGeneratorTypes
{
    //Noise the noise based generator, the only one the data pack uses
    public static readonly MapCodec<object> Noise =
        new MappedMapCodec<ChunkGenerator, object>(new NoiseChunkGeneratorCodec(), value => value, value => (ChunkGenerator)value);

    private static bool _registered;

    //RegisterAll registers every type before the registry freezes
    public static void RegisterAll()
    {
        if (_registered) return;
        _registered = true;
        _ = Noise;
        Registry<MapCodec<object>>.Register(BuiltInRegistries.CHUNK_GENERATOR,
            ResourceKey<MapCodec<object>>.Create(Registries.CHUNK_GENERATOR, Identifier.WithDefaultNamespace("noise")),
            Noise);
    }
}

//NoiseChunkGeneratorCodec decodes the noise generator, maps to vanilla NoiseBasedChunkGenerator.CODEC
//It resolves the biome source through its own registry and the noise settings by name from the loaded data
internal sealed class NoiseChunkGeneratorCodec : AbstractMapCodec<ChunkGenerator>
{
    public override DataResult<ChunkGenerator> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var biomeTag = input.Get("biome_source");
        if (!biomeTag.IsPresent)
            return DataResult<ChunkGenerator>.Error(() => "noise generator is missing the biome_source field");
        var biomeSource = BiomeSourceCodecs.Decode(ops, biomeTag.Get());
        if (!biomeSource.Result().IsPresent)
            return DataResult<ChunkGenerator>.Error(() => "failed to parse biome_source");

        var settingsText = LevelStemCodec.ReadString(ops, input, "settings");
        if (settingsText is null)
            return DataResult<ChunkGenerator>.Error(() => "noise generator is missing the settings field");
        var settingsId = Identifier.TryParse(settingsText);
        if (settingsId is null)
            return DataResult<ChunkGenerator>.Error(() => $"invalid noise settings id {settingsText}");
        //Noise settings are loaded by data; if missing, the pack lacks worldgen/noise_settings
        var settingsRegistry = (ops as RegistryOps<U>)?.GetRegistry(Registries.NOISE_SETTINGS)
            ?? BuiltInRegistries.NOISE_SETTINGS;
        if (settingsRegistry.GetValue(settingsId.Value) is not NoiseGeneratorSettings settings)
            return DataResult<ChunkGenerator>.Error(() => $"noise settings {settingsId} not found");
        return DataResult<ChunkGenerator>.Success(new NoiseBasedChunkGenerator(biomeSource.GetOrThrow(), settings));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, ChunkGenerator value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding chunk generators is not supported yet");
}
