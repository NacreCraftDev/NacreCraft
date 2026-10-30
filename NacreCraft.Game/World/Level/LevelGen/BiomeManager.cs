using System.Security.Cryptography;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.LevelGen;

//BiomeManager biome manager, maps to vanilla net.minecraft.world.level.biome.BiomeManager
//Finds the nearest quart sample point for a block coordinate, preferring the generated chunk's biome palette and only falling back to full climate sampling out of bounds
//The surface stage queries a biome for every block; vanilla keeps the cost constant through this palette lookup
//Passing BiomeSource as the getter would degenerate into a 6-dimensional climate sample per block and make chunk generation time out
public sealed class BiomeManager
{
    //_chunk the chunk being generated; its biome palette is filled during the BIOMES stage
    private readonly ChunkAccess _chunk;
    //_fallback full sampling when out of bounds; arguments are the block coordinates of the quart, matching those written into the palette during BIOMES
    private readonly Func<int, int, int, Biome> _fallback;
    //_region neighbouring chunks around the generating one; their palettes answer edge lookups without a climate sample
    private readonly WorldGenRegion? _region;
    //_borderCache memoises out-of-chunk quart lookups; a climate sample depends only on the quart, so repeats are pure waste
    private readonly Dictionary<(int X, int Y, int Z), Biome> _borderCache = new();
    //_cornerFiddles caches the eight corner offsets per quart; they depend only on the corner and the seed
    //Surface asks for every block in a column, so recomputing them per block repeated the same nine LCGs sixty-four times
    private readonly Dictionary<(int X, int Y, int Z), double[]> _cornerFiddles = new();
    private readonly long _biomeZoomSeed;
    private readonly int _minQuartY;
    private readonly int _maxQuartY;
    //_palette this chunk's biome grid, indexed by quart offset within the chunk
    //Surface asks for one biome per block, so walking section then palette container then hash palette for each of them
    //was the single hottest lookup in the stage; copying the same 4 x quartY x 4 grid once turns it into two array indexes
    private readonly Biome[] _palette;
    private readonly int _paletteSizeY;

    public BiomeManager(ChunkAccess chunk, Func<int, int, int, Biome> fallback, long seed,
        WorldGenRegion? region = null)
    {
        _chunk = chunk;
        _fallback = fallback;
        _region = region;
        _biomeZoomSeed = ObfuscateSeed(seed);
        _minQuartY = chunk.MinSectionY * 4;
        _maxQuartY = (chunk.MinSectionY + chunk.SectionsCount) * 4;
        _paletteSizeY = _maxQuartY - _minQuartY;
        _palette = new Biome[4 * _paletteSizeY * 4];
        var minQuartX = chunk.Pos.X * 4;
        var minQuartZ = chunk.Pos.Z * 4;
        for (var y = 0; y < _paletteSizeY; y++)
        for (var x = 0; x < 4; x++)
        for (var z = 0; z < 4; z++)
            _palette[(y * 4 + x) * 4 + z] = chunk.GetNoiseBiome(minQuartX + x, _minQuartY + y, minQuartZ + z).Value;
    }

    //ObfuscateSeed scrambles the world seed, maps to vanilla obfuscateSeed's sha256(seed).asLong()
    //Vanilla HashCode.asLong reads the first eight bytes little-endian and writes the input little-endian too; match that or every fiddled distance differs
    public static long ObfuscateSeed(long seed)
    {
        Span<byte> input = stackalloc byte[8];
        for (var i = 0; i < 8; i++) input[i] = (byte)(seed >> (i * 8));
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);
        var result = 0L;
        for (var i = 0; i < 8; i++) result |= (long)hash[i] << (i * 8);
        return result;
    }

    //GetBiome returns the biome of the nearest sample point for a block coordinate, maps to vanilla getBiome
    //Shifts the coordinate two blocks negative then buckets by four, computing a fiddled distance for each of the eight corners and taking the minimum
    //The two-block shift puts the sample at the block centre rather than the edge, and fiddled distance avoids large regular grids
    public Biome GetBiome(int blockX, int blockY, int blockZ)
    {
        //Temporary: splits the corner selection from the quart lookup, the surface stage reports both
        var entry = System.Diagnostics.Stopwatch.GetTimestamp();
        var absoluteX = blockX - 2;
        var absoluteY = blockY - 2;
        var absoluteZ = blockZ - 2;
        var parentX = absoluteX >> 2;
        var parentY = absoluteY >> 2;
        var parentZ = absoluteZ >> 2;
        var fractX = (absoluteX & 3) / 4.0;
        var fractY = (absoluteY & 3) / 4.0;
        var fractZ = (absoluteZ & 3) / 4.0;
        var fiddles = CornerFiddles(parentX, parentY, parentZ);
        var nearestCorner = 0;
        var nearestDistance = double.PositiveInfinity;
        for (var i = 0; i < 8; i++)
        {
            var xEven = (i & 4) == 0;
            var yEven = (i & 2) == 0;
            var zEven = (i & 1) == 0;
            var distanceX = (xEven ? fractX : fractX - 1.0) + fiddles[i * 3];
            var distanceY = (yEven ? fractY : fractY - 1.0) + fiddles[i * 3 + 1];
            var distanceZ = (zEven ? fractZ : fractZ - 1.0) + fiddles[i * 3 + 2];
            var distance = distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ;
            if (distance >= nearestDistance) continue;
            nearestCorner = i;
            nearestDistance = distance;
        }
        var quartX = (nearestCorner & 4) == 0 ? parentX : parentX + 1;
        var quartY = (nearestCorner & 2) == 0 ? parentY : parentY + 1;
        var quartZ = (nearestCorner & 1) == 0 ? parentZ : parentZ + 1;
        var split = System.Diagnostics.Stopwatch.GetTimestamp();
        var biome = GetNoiseBiome(quartX, quartY, quartZ);
        CornerTicks += split - entry;
        LookupTicks += System.Diagnostics.Stopwatch.GetTimestamp() - split;
        return biome;
    }

    //Temporary: corner selection ticks and quart lookup ticks, cleared by the surface report
    public static long CornerTicks;
    public static long LookupTicks;

    //GetNoiseBiome returns the biome for a quart coordinate, using the palette inside this chunk and falling back to full sampling, maps to vanilla LevelReader.getNoiseBiome
    private Biome GetNoiseBiome(int quartX, int quartY, int quartZ)
    {
        if (quartX >> 2 == _chunk.Pos.X && quartZ >> 2 == _chunk.Pos.Z
            && quartY >= _minQuartY && quartY < _maxQuartY)
        {
            PaletteHits++;
            return _palette[((quartY - _minQuartY) * 4 + (quartX & 3)) * 4 + (quartZ & 3)];
        }
        //Neighbouring chunks past BIOMES already hold a filled biome palette; reading it avoids a full climate sample per edge block
        if (_region?.GetChunk(quartX >> 2, quartZ >> 2) is { } neighbour
            && neighbour.ChunkStatus is { } status && status.IsOrAfter(ChunkStatus.BIOMES))
        {
            RegionHits++;
            return neighbour.GetNoiseBiome(quartX, quartY, quartZ).Value;
        }
        var key = (quartX, quartY, quartZ);
        if (_borderCache.TryGetValue(key, out var memoised))
        {
            BorderHits++;
            return memoised;
        }
        FallbackSamples++;
        var sampled = _fallback(quartX * 4, quartY * 4, quartZ * 4);
        _borderCache[key] = sampled;
        return sampled;
    }

    //Temporary: which branch of the quart lookup answered, cleared by the surface report
    public static long PaletteHits;
    public static long RegionHits;
    public static long BorderHits;
    public static long FallbackSamples;

    //CornerFiddles returns the eight per-corner offsets for a quart, maps to the offset part of vanilla getFiddledDistance
    //The result has three doubles per corner in x, y, z order
    private double[] CornerFiddles(int parentX, int parentY, int parentZ)
    {
        var key = (parentX, parentY, parentZ);
        if (_cornerFiddles.TryGetValue(key, out var cached)) return cached;
        var values = new double[24];
        for (var i = 0; i < 8; i++)
        {
            var cornerX = (i & 4) == 0 ? parentX : parentX + 1;
            var cornerY = (i & 2) == 0 ? parentY : parentY + 1;
            var cornerZ = (i & 1) == 0 ? parentZ : parentZ + 1;
            var value = LinearCongruentialNext(_biomeZoomSeed, cornerX);
            value = LinearCongruentialNext(value, cornerY);
            value = LinearCongruentialNext(value, cornerZ);
            value = LinearCongruentialNext(value, cornerX);
            value = LinearCongruentialNext(value, cornerY);
            value = LinearCongruentialNext(value, cornerZ);
            values[i * 3] = Fiddle(value);
            var second = LinearCongruentialNext(value, _biomeZoomSeed);
            values[i * 3 + 1] = Fiddle(second);
            values[i * 3 + 2] = Fiddle(LinearCongruentialNext(second, _biomeZoomSeed));
        }
        _cornerFiddles[key] = values;
        return values;
    }

    //LinearCongruentialNext one linear congruential step, maps to vanilla LinearCongruentialGenerator.next
    //long overflow is modulo 2^64, matching Java vanilla, so unchecked is required
    private static long LinearCongruentialNext(long value, long addend)
        => unchecked(value * (value * 6364136223846793005L + 1442695040888963407L) + addend);

    //Fiddle takes the offset, maps to vanilla getFiddle; the result stays within ±0.45 blocks
    private static double Fiddle(long value)
    {
        var uniform = FloorMod(value >> 24, 1024) / 1024.0;
        return (uniform - 0.5) * 0.9;
    }

    //FloorMod always-non-negative modulo, maps to vanilla Math.floorMod
    private static long FloorMod(long value, long modulus)
    {
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
