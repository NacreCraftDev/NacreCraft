using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Level.LevelGen.Dimension;

//BiomeSourceCodecs biome source dispatch, maps to vanilla BiomeSources dispatch
//The codec for each type lives in the BIOME_SOURCE registry; this only reads the type field and forwards to it
public static class BiomeSourceCodecs
{
    //Decode derive the biome source from the type field
    public static DataResult<BiomeSource> Decode<U>(DynamicOps<U> ops, U input)
        => ops.GetMap(input).FlatMap(map => DecodeMap(ops, map));

    private static DataResult<BiomeSource> DecodeMap<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var typeText = ReadString(ops, input, "type");
        if (typeText is null)
            return DataResult<BiomeSource>.Error(() => "biome source is missing the type field");
        var typeId = Identifier.TryParse(typeText);
        if (typeId is null)
            return DataResult<BiomeSource>.Error(() => $"invalid biome source type id {typeText}");

        var registry = (ops as RegistryOps<U>)?.GetRegistry(Registries.BIOME_SOURCE) ?? BuiltInRegistries.BIOME_SOURCE;
        var codec = registry.GetValue(ResourceKey<MapCodec<BiomeSource>>.Create(Registries.BIOME_SOURCE, typeId.Value));
        return codec is null
            ? DataResult<BiomeSource>.Error(() => $"unsupported biome source type {typeId}")
            : codec.Decode(ops, input);
    }

    //ParameterListRegistry prefer the registry carried by RegistryOps; fall back to the built-in one for plain ops
    internal static Registry<object>? ParameterListRegistry<U>(DynamicOps<U> ops)
        => (ops as RegistryOps<U>)?.GetRegistry(Registries.MULTI_NOISE_BIOME_SOURCE_PARAMETER_LIST)
            ?? BuiltInRegistries.MULTI_NOISE_BIOME_SOURCE_PARAMETER_LIST;

    //BiomeRegistry same as above for biomes; tests can pass an isolated registry without touching the global one
    internal static Registry<Biome>? BiomeRegistry<U>(DynamicOps<U> ops)
        => (ops as RegistryOps<U>)?.GetRegistry(Registries.BIOME) ?? BuiltInRegistries.BIOME;

    //ReadString read a string field; returns null when missing or not a string
    internal static string? ReadString<U>(DynamicOps<U> ops, MapLike<U> input, string name)
    {
        var tag = input.Get(name);
        if (!tag.IsPresent) return null;
        var text = ops.GetStringValue(tag.Get());
        return text.Result().IsPresent ? text.GetOrThrow() : null;
    }
}
