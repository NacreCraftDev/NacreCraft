namespace NacreCraft.Util.Random;

//SingleThreadedRandomSource single-threaded 48-bit LCG, maps to vanilla net.minecraft.world.level.levelgen.SingleThreadedRandomSource
//Same seed sequence as LegacyRandomSource but the Gaussian helper is created lazily; used by worldgen threads that own their source
public sealed class SingleThreadedRandomSource : BitRandomSource
{
    private const int ModulusBits = 48;
    private const long ModulusMask = 281474976710655L;
    private const long Multiplier = 25214903917L;
    private const long Increment = 11;

    private long _seed;
    private MarsagliaPolarGaussian? _gaussianSource;

    public SingleThreadedRandomSource(long seed) => SetSeed(seed);

    public RandomSource Fork() => new SingleThreadedRandomSource(NextLong());

    public PositionalRandomFactory ForkPositional()
        => new LegacyRandomSource.LegacyPositionalRandomFactory(NextLong());

    public void SetSeed(long seed)
    {
        _seed = (seed ^ Multiplier) & ModulusMask;
        _gaussianSource?.Reset();
    }

    public int Next(int bits)
    {
        _seed = (_seed * Multiplier + Increment) & ModulusMask;
        return (int)(_seed >>> (ModulusBits - bits));
    }

    public int NextInt() => Next(32);

    public int NextInt(int bound)
    {
        if (bound <= 0)
            throw new ArgumentException("Bound must be positive");
        if ((bound & (bound - 1)) == 0)
            return (int)((bound * (long)Next(31)) >> 31);
        int sample, modulo;
        do
        {
            sample = Next(31);
            modulo = sample % bound;
        } while (sample - modulo + (bound - 1) < 0);
        return modulo;
    }

    public long NextLong() => ((long)Next(32) << 32) + Next(32);

    public bool NextBoolean() => Next(1) != 0;

    public float NextFloat() => Next(24) * BitRandomSource.FloatMultiplier;

    public double NextDouble() => (((long)Next(26) << 27) + Next(27)) * BitRandomSource.DoubleMultiplier;

    public double NextGaussian() => (_gaussianSource ??= new MarsagliaPolarGaussian(this)).NextGaussian();
}
