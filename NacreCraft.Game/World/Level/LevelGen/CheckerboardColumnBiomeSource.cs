using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen;

//CheckerboardColumnBiomeSource a checkerboard of the allowed biomes, maps to vanilla net.minecraft.world.level.biome.CheckerboardColumnBiomeSource
//Column index x>>bitShift plus z>>bitShift picks the biome, so each cell is a square of 2^(scale+2) blocks
public sealed class CheckerboardColumnBiomeSource : BiomeSource, BiomeResolver
{
    public HolderSet<Biome> AllowedBiomes { get; }

    //Size the scale field, clamped to 0..62 by the codec like vanilla intRange
    public int Size { get; }

    private readonly int _bitShift;

    public CheckerboardColumnBiomeSource(HolderSet<Biome> allowedBiomes, int size)
    {
        AllowedBiomes = allowedBiomes;
        Size = size;
        _bitShift = size + 2;
    }

    public IReadOnlyList<Biome> PossibleBiomes
        => AllowedBiomes.Select(holder => holder.Value).ToList();

    public Biome GetBiome(int x, int y, int z) => GetBiomeHolder(x, y, z).Value;

    public Holder<Biome> GetBiomeHolder(int quartX, int quartY, int quartZ)
        => AllowedBiomes.Get(FloorMod((quartX >> _bitShift) + (quartZ >> _bitShift), AllowedBiomes.Size));

    public Holder<Biome> GetNoiseBiome(int quartX, int quartY, int quartZ, Climate.Sampler sampler)
        => GetBiomeHolder(quartX, quartY, quartZ);

    private static int FloorMod(int value, int modulus)
    {
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
