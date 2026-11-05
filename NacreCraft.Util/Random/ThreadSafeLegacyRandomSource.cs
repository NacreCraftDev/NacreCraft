namespace NacreCraft.Util.Random;

//ThreadSafeLegacyRandomSource thread-safe legacy random source, maps to vanilla net.minecraft.world.level.levelgen.ThreadSafeLegacyRandomSource
//Same 48-bit LCG advanced with a compare-and-swap loop; vanilla marks it deprecated in favour of the splittable sources
public sealed class ThreadSafeLegacyRandomSource : BitRandomSource
{
    private const int ModulusBits = 48;
    private const long ModulusMask = 281474976710655L;
    private const long Multiplier = 25214903917L;
    private const long Increment = 11;

    private long _seed;
    private readonly MarsagliaPolarGaussian _gaussianSource;

    public ThreadSafeLegacyRandomSource(long seed)
    {
        _gaussianSource = new MarsagliaPolarGaussian(this);
        SetSeed(seed);
    }

    public RandomSource Fork() => new ThreadSafeLegacyRandomSource(NextLong());

    public PositionalRandomFactory ForkPositional()
        => new LegacyRandomSource.LegacyPositionalRandomFactory(NextLong());

    public void SetSeed(long seed) => Interlocked.Exchange(ref _seed, (seed ^ Multiplier) & ModulusMask);

    public int Next(int bits)
    {
        long oldSeed, nextSeed;
        do
        {
            oldSeed = Interlocked.Read(ref _seed);
            nextSeed = (oldSeed * Multiplier + Increment) & ModulusMask;
        } while (Interlocked.CompareExchange(ref _seed, nextSeed, oldSeed) != oldSeed);
        return (int)((ulong)nextSeed >>> (ModulusBits - bits));
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

    public double NextGaussian() => _gaussianSource.NextGaussian();
}
