using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen;

//MultiNoiseBiomeSource multi-noise biome source, maps to vanilla net.minecraft.world.level.biome.MultiNoiseBiomeSource
//Phase E wires in Climate.Sampler + MultiNoiseBiomeSourceParameterList for real derivation
//GetBiome samples 6-dimensional parameters by coordinate and finds the biome closest in parameter space
public sealed class MultiNoiseBiomeSource : BiomeSource
{
    //ParameterList registry reference; the parameter list registry is object keyed on the Registry side
    public Holder<object>? ParameterList { get; }
    public Climate.Sampler? Sampler { get; }

    public MultiNoiseBiomeSource(Holder<object> parameterList, Climate.Sampler sampler)
    {
        ParameterList = parameterList;
        Sampler = sampler;
    }

    //Single-parameter constructor reserved for data-driven assembly; the sampler is injected by NoiseBasedChunkGenerator from the noise router
    public MultiNoiseBiomeSource(Holder<object> parameterList)
    {
        ParameterList = parameterList;
    }

    //LegacyConstructor placeholder with no parameter list and no sampler, returns the plains biome
    //Used before Bootstrap or in test scenarios
    public MultiNoiseBiomeSource() { }

    //Data dereferences the parameter list holder; only valid once the registry element is bound
    private MultiNoiseBiomeSourceParameterList? Data
        => ParameterList?.Value as MultiNoiseBiomeSourceParameterList;

    //PossibleBiomes distinct biomes seen in the parameter table; only the plains placeholder without a table
    //Unbound entries are skipped, they point at biomes not loaded yet
    public IReadOnlyList<Biome> PossibleBiomes
    {
        get
        {
            var data = Data;
            if (data is null) return new[] { Biome.Plains };
            var seen = new HashSet<Biome>(ReferenceEqualityComparer.Instance);
            var result = new List<Biome>();
            foreach (var (_, holder) in data.Entries)
            {
                if (!holder.IsBound()) continue;
                if (seen.Add(holder.Value)) result.Add(holder.Value);
            }
            return result.Count == 0 ? new[] { Biome.Plains } : result;
        }
    }

    //GetBiome samples 6-dimensional parameters by coordinate and finds the biome closest in parameter space
    //Without a ParameterList/Sampler it returns the Biome.Plains singleton as a placeholder to avoid palette blow-up
    public Biome GetBiome(int x, int y, int z) => GetBiomeHolder(x, y, z).Value;

    //GetBiomeHolder answers with the registry holder the parameter tree already holds, maps to vanilla
    //MultiNoiseBiomeSource.getNoiseBiome
    //The biome fill writes one holder per quart; going through GetBiome and wrapping the value back cost a reverse
    //registry lookup for every one of those
    public Holder<Biome> GetBiomeHolder(int x, int y, int z)
    {
        var data = Data;
        if (data is null || Sampler is null)
            return BuiltInRegistries.BIOME.WrapAsHolder(Biome.Plains);

        var target = new Climate.ParameterPoint(
            Sampler.Temperature(x, y, z),
            Sampler.Humidity(x, y, z),
            Sampler.Continentalness(x, y, z),
            Sampler.Erosion(x, y, z),
            Sampler.Depth(x, y, z),
            Sampler.Weirdness(x, y, z),
            0L);
        return data.FindClosest(target);
    }
}
