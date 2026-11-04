using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen;

//FixedBiomeSource one biome everywhere, maps to vanilla net.minecraft.world.level.biome.FixedBiomeSource
//Vanilla holds a Holder<Biome>; this port keeps the biome value and wraps it on demand, matching the value-based BiomeSource stub
public sealed class FixedBiomeSource : BiomeSource, BiomeResolver
{
    public Biome Biome { get; }

    public FixedBiomeSource(Biome biome) => Biome = biome;

    public IReadOnlyList<Biome> PossibleBiomes => new[] { Biome };

    public Biome GetBiome(int x, int y, int z) => Biome;

    public Holder<Biome> GetBiomeHolder(int x, int y, int z) => BuiltInRegistries.BIOME.WrapAsHolder(Biome);

    public Holder<Biome> GetNoiseBiome(int quartX, int quartY, int quartZ, Climate.Sampler sampler)
        => GetBiomeHolder(quartX, quartY, quartZ);
}
