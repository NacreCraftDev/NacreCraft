using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Level.LevelGen.Dimension;

//BiomeSources biome source type registration, maps to vanilla net.minecraft.world.level.biome.BiomeSources
//Each type owns a MapCodec and registers into BIOME_SOURCE, so dispatch looks the codec up in the registry instead of
//branching on a string; touching any static field here is what performs the registration, like every other type table
public static class BiomeSources
{
    //Fixed one biome everywhere
    public static readonly MapCodec<BiomeSource> Fixed = new FixedBiomeSourceCodec();

    //MultiNoise multi-noise source; only the preset form is used by the data pack
    public static readonly MapCodec<BiomeSource> MultiNoise = new MultiNoiseBiomeSourceCodec();

    //Checkerboard a checkerboard of the allowed biomes
    public static readonly MapCodec<BiomeSource> Checkerboard = new CheckerboardColumnBiomeSourceCodec();

    //TheEnd the end source, whose five biomes come from the biome registry
    public static readonly MapCodec<BiomeSource> TheEnd = new TheEndBiomeSourceCodec();

    private static bool _registered;

    //Bootstrap registers every source type before the registry freezes, maps to vanilla BiomeSources.bootstrap
    public static void Bootstrap()
    {
        if (_registered) return;
        _registered = true;
        _ = Fixed;
        _ = MultiNoise;
        _ = Checkerboard;
        _ = TheEnd;
        Register("fixed", Fixed);
        Register("multi_noise", MultiNoise);
        Register("checkerboard", Checkerboard);
        Register("the_end", TheEnd);
    }

    private static void Register(string name, MapCodec<BiomeSource> codec)
        => Registry<MapCodec<BiomeSource>>.Register(BuiltInRegistries.BIOME_SOURCE,
            ResourceKey<MapCodec<BiomeSource>>.Create(Registries.BIOME_SOURCE, Identifier.WithDefaultNamespace(name)),
            codec);
}

//FixedBiomeSourceCodec decodes the fixed source, maps to vanilla FixedBiomeSource.CODEC
internal sealed class FixedBiomeSourceCodec : AbstractMapCodec<BiomeSource>
{
    public override DataResult<BiomeSource> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var tag = input.Get("biome");
        if (!tag.IsPresent)
            return DataResult<BiomeSource>.Error(() => "fixed biome source is missing biome");
        var biome = Biome.Codec.Parse(ops, tag.Get());
        return biome.Result().IsPresent
            ? DataResult<BiomeSource>.Success(new FixedBiomeSource(biome.GetOrThrow()))
            : DataResult<BiomeSource>.Error(() => "invalid biome in fixed biome source");
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, BiomeSource value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding biome sources is not supported yet");
}

//CheckerboardColumnBiomeSourceCodec decodes the checkerboard source, maps to vanilla CheckerboardColumnBiomeSource.CODEC
//scale is limited to 0..62 like the vanilla intRange; out-of-range values are rejected rather than clamped
internal sealed class CheckerboardColumnBiomeSourceCodec : AbstractMapCodec<BiomeSource>
{
    private const int DefaultScale = 2;
    private const int MaxScale = 62;

    public override DataResult<BiomeSource> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var biomesTag = input.Get("biomes");
        if (!biomesTag.IsPresent)
            return DataResult<BiomeSource>.Error(() => "checkerboard biome source is missing biomes");
        var biomes = HolderSetCodecs.BiomeSet.Parse(ops, biomesTag.Get());
        if (!biomes.Result().IsPresent)
            return DataResult<BiomeSource>.Error(() => "invalid biomes in checkerboard biome source");
        var size = DefaultScale;
        var sizeTag = input.Get("scale");
        if (sizeTag.IsPresent)
        {
            var parsed = ops.GetNumberValue(sizeTag.Get());
            if (!parsed.Result().IsPresent)
                return DataResult<BiomeSource>.Error(() => "checkerboard scale is not a number");
            size = (int)parsed.GetOrThrow();
        }
        if (size < 0 || size > MaxScale)
            return DataResult<BiomeSource>.Error(() => $"checkerboard scale must be within 0..{MaxScale}, got {size}");
        return DataResult<BiomeSource>.Success(new CheckerboardColumnBiomeSource(biomes.GetOrThrow(), size));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, BiomeSource value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding biome sources is not supported yet");
}

//MultiNoiseBiomeSourceCodec decodes the preset form of multi_noise, maps to vanilla MultiNoiseBiomeSource.CODEC
//The inline biomes list form is unused by the data pack; add it when needed
internal sealed class MultiNoiseBiomeSourceCodec : AbstractMapCodec<BiomeSource>
{
    public override DataResult<BiomeSource> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var presetText = BiomeSourceCodecs.ReadString(ops, input, "preset");
        if (presetText is null)
            return DataResult<BiomeSource>.Error(() => "multi_noise only supports the preset form for now");
        var presetId = Identifier.TryParse(presetText);
        if (presetId is null)
            return DataResult<BiomeSource>.Error(() => $"invalid parameter list preset id {presetText}");
        //The parameter list must already be loaded by data; if missing, the pack lacks worldgen/multi_noise_biome_source_parameter_list
        var registry = BiomeSourceCodecs.ParameterListRegistry(ops);
        if (registry is null)
            return DataResult<BiomeSource>.Error(() => "parameter list registry is missing");
        //A pure read of the key index; decoding is concurrent and must not create holders
        var holder = registry.GetHolder(presetId.Value);
        return holder is null
            ? DataResult<BiomeSource>.Error(() => $"parameter list preset {presetId} not found")
            : DataResult<BiomeSource>.Success(new MultiNoiseBiomeSource(holder));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, BiomeSource value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding biome sources is not supported yet");
}

//TheEndBiomeSourceCodec decodes the end source, maps to vanilla TheEndBiomeSource.CODEC
//It takes no arguments; its five fixed biomes are resolved from the biome registry
internal sealed class TheEndBiomeSourceCodec : AbstractMapCodec<BiomeSource>
{
    public override DataResult<BiomeSource> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var source = TheEndBiomeSource.FromRegistry(BiomeSourceCodecs.BiomeRegistry(ops));
        return source is null
            ? DataResult<BiomeSource>.Error(() => "end biome source is missing required biomes (the_end/end_highlands/end_midlands/small_end_islands/end_barrens)")
            : DataResult<BiomeSource>.Success(source);
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, BiomeSource value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding biome sources is not supported yet");
}
