using System.Diagnostics;

namespace NacreCraft.Util;

//TimeSource a clock readable in a chosen time unit, maps to vanilla net.minecraft.util.TimeSource
public interface TimeSource
{
    long Get(TimeUnit unit);

    //Constant a fixed reading, maps to vanilla TimeSource.constant
    public static TimeSource Constant(long value) => new ConstantTimeSource(value);

    //NanoTime a monotonic stopwatch reading, maps to vanilla NanoTimeSource backed by Util.timeSource
    public static TimeSource NanoTime() => NanoTimeSource.Instance;

    //ConvertNanos expresses a nanosecond count in the given unit
    public static long ConvertNanos(TimeUnit unit, long nanos) => unit switch
    {
        TimeUnit.Nanoseconds => nanos,
        TimeUnit.Microseconds => nanos / 1_000L,
        TimeUnit.Milliseconds => nanos / 1_000_000L,
        TimeUnit.Seconds => nanos / 1_000_000_000L,
        TimeUnit.Minutes => nanos / (1_000_000_000L * 60),
        TimeUnit.Hours => nanos / (1_000_000_000L * 3600),
        TimeUnit.Days => nanos / (1_000_000_000L * 86400),
        _ => nanos
    };

    private sealed class ConstantTimeSource(long value) : TimeSource
    {
        public long Get(TimeUnit unit) => value;
    }

    private sealed class NanoTimeSource : TimeSource
    {
        public static readonly NanoTimeSource Instance = new();
        private static readonly double TicksToNanos = 1_000_000_000.0d / Stopwatch.Frequency;

        public long Get(TimeUnit unit)
            => ConvertNanos(unit, (long)(Stopwatch.GetTimestamp() * TicksToNanos));
    }
}
