namespace NacreCraft.Util;

//General utility helpers, maps to vanilla net.minecraft.util.Util
//Only Memoize is inlined, pure functions; other methods (WriteAndReadTypedOrThrow etc. that depend on DFU types are placed in DataFixUtils per dependency direction)
//Other methods (getRandomMillis/getMillis, etc.) are implemented as needed in ProfilingUtil
public static class Util
{
    //MaxThreadsProperty system property that caps the executor thread count, maps to vanilla Util.MAX_THREADS_SYSTEM_PROPERTY
    private const string MaxThreadsProperty = "max.bg.threads";

    //Thread-safe memoization cache, maps to vanilla Util.memoize
    public static Func<T, R> Memoize<T, R>(Func<T, R> fn) where T : notnull
    {
        var cache = new System.Collections.Concurrent.ConcurrentDictionary<T, R>();
        return input => cache.GetOrAdd(input, fn);
    }

    //OffsetByCodepoints moves a UTF-16 index by whole codepoints, maps to vanilla Util.offsetByCodepoints
    public static int OffsetByCodepoints(string text, int position, int count)
    {
        var length = text.Length;
        if (count >= 0)
        {
            for (var i = 0; i < count; i++)
            {
                if (position >= length) break;
                position += char.IsHighSurrogate(text[position]) && position + 1 < length
                    && char.IsLowSurrogate(text[position + 1]) ? 2 : 1;
            }
        }
        else
        {
            for (var i = count; i < 0; i++)
            {
                if (position <= 0) break;
                position -= position >= 2 && char.IsLowSurrogate(text[position - 1])
                    && char.IsHighSurrogate(text[position - 2]) ? 2 : 1;
            }
        }

        return position;
    }

    //MaxAllowedExecutorThreads how many worker threads the parallel executors may use, maps to vanilla Util.maxAllowedExecutorThreads
    public static int MaxAllowedExecutorThreads()
        => Mth.Clamp(Environment.ProcessorCount - 1, 1, GetMaxThreads());

    private static int GetMaxThreads()
    {
        //Vanilla reads a JVM system property; the process environment variable is the .NET equivalent
        var raw = Environment.GetEnvironmentVariable(MaxThreadsProperty);
        if (!string.IsNullOrEmpty(raw))
        {
            if (int.TryParse(raw, out var maxThreads) && maxThreads >= 1 && maxThreads <= 255) return maxThreads;
            Logging.Log.Error($"Wrong {MaxThreadsProperty} property value '{raw}'. Should be an integer value between 1 and 255.");
        }
        return 255;
    }

    //RunNamed runs an action with the current thread renamed, maps to vanilla Util.runNamed
    //The rename only happens in development builds, matching the vanilla IS_RUNNING_IN_IDE guard
    public static void RunNamed(Action runnable, string name)
    {
        if (!NacreCraft.Config.DebugFlags.IsRunningInIde)
        {
            runnable();
            return;
        }
        var thread = System.Threading.Thread.CurrentThread;
        var oldName = thread.Name;
        thread.Name = name;
        try { runnable(); }
        finally { thread.Name = oldName; }
    }
}
