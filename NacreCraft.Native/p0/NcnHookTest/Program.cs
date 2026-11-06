using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NcnHookTest.Guard;

namespace NcnHookTest;

//P0 harness for the machine-code stack guard
//
//It runs under the profiler like the product does, and every method here lives in this assembly so the module filter has
//something to accept. The throw entry sits in a second assembly the guard never touches, which stands in for the place
//NacreCraft.Util holds in the product. The run script starts the process with the profiler variables and the module
//list; nothing in here turns the guard on by itself.
internal static class Program
{
    private const string LibraryName = "nacrecraft_native";

    //Small enough that a test run reaches it well inside the main thread's stack
    private const int ReserveBytes = 256 * 1024;

    [StructLayout(LayoutKind.Sequential)]
    private struct HookStats
    {
        public ulong Attempts;
        public ulong Hooked;
        public ulong Deduplicated;
        public ulong NotReady;
        public ulong SkippedDecode;
        public ulong SkippedAlloc;
        public ulong SkippedPatch;
        public ulong TrampolineBytes;
        public ulong InstallNanos;
        public ulong MaxInstallNanos;
        public ulong SlowCalls;
        public ulong Trips;
        public ulong ThreadInits;
        public ulong DisabledThreads;
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_stack_hook_enabled();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_configure_stack_guard(IntPtr tripEntry, UIntPtr reserve);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_hook_stats(out HookStats stats);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_hook_addresses([Out] IntPtr[] addresses, int capacity);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ncn_hook_report();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_hook_debug_state([Out] IntPtr[] values, int capacity);

    //Counters the recursion tests read after the stack has been unwound
    private static int _depth;
    private static int _finallyRuns;
    private static int _finallyDepth;
    private static int _disposeRuns;
    private static bool _gcRan;
    private static int _gcDepth;

    private static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--bench")
            return Bench(int.Parse(args[1], CultureInfo.InvariantCulture));

        Console.WriteLine("NcnHookTest: P0 machine-code stack guard");

        var hookOn = ncn_stack_hook_enabled() != 0;
        if (!hookOn)
        {
            //The baseline run is the control: the same tests, no hook, so a failure here is not the guard's
            Console.WriteLine("the guard is off; running the checks as the control");
        }
        else
        {
            //Disarm first: resolving the throw entry compiles it, and a hook installed on it would send every trip back
            //through the check it just failed
            _ = ncn_configure_stack_guard(IntPtr.Zero, UIntPtr.Zero);
            var trip = ResolveTripEntry();
            _ = ncn_configure_stack_guard(trip, (UIntPtr)ReserveBytes);
            Console.WriteLine($"trip entry 0x{trip.ToInt64():X16}, reserve {ReserveBytes / 1024} KB");
        }

        var failures = new List<string>();

        //The control run cannot exercise the guard, so it only proves the plain runtime path still works with the
        //profiler attached; the recursion tests would reach the real guard page without a hook
        Check(failures, "regression over varied signatures", Regression);

        if (hookOn)
        {
            PrintDebugState();
            Check(failures, "deep recursion without a handler", DeepRecursionWithoutHandler);
            Check(failures, "deep recursion on a worker thread", WorkerThreadRecursion);
            Check(failures, "finally runs while unwinding", FinallyRuns);
            Check(failures, "using disposes while unwinding", UsingDisposes);
            Check(failures, "a second trip behaves like the first", RepeatTrip);
            Check(failures, "gc runs while the stack is short", GcWhileShort);
            Check(failures, "a promoted method stays hooked", PromotedMethod);
        }

        var stats = ReadStats();
        var averageInstall = stats.Hooked == 0 ? 0 : (double)stats.InstallNanos / stats.Hooked;
        Console.WriteLine(
            $"hook stats: {stats.Attempts} attempts, {stats.Hooked} hooked, {stats.Deduplicated} duplicated, " +
            $"{stats.NotReady} before arming, {stats.SkippedDecode} decode skipped, {stats.SkippedAlloc} alloc skipped, " +
            $"{stats.SkippedPatch} patch skipped");
        Console.WriteLine(
            $"hook stats: {stats.TrampolineBytes} trampoline bytes, {averageInstall:N0} ns per method " +
            $"(max {stats.MaxInstallNanos:N0}), {stats.SlowCalls} slow calls, {stats.Trips} trips, " +
            $"{stats.ThreadInits} thread states, {stats.DisabledThreads} threads left unguarded");

        if (hookOn && stats.Hooked == 0)
            failures.Add("no method entry was hooked, so nothing below proves anything");

        ncn_hook_report();
        WriteLogTail();

        foreach (var failure in failures)
            Console.Error.WriteLine($"FAIL: {failure}");

        Console.WriteLine(failures.Count == 0 ? "all checks passed" : $"{failures.Count} check(s) failed");
        return failures.Count == 0 ? 0 : 1;
    }

    private static void Check(List<string> failures, string name, Action test)
    {
        var before = ReadStats();
        try
        {
            test();
            Console.WriteLine($"ok   {name}");
        }
        catch (Exception exception)
        {
            failures.Add($"{name}: {exception.GetType().Name}: {exception.Message}");
            Console.WriteLine($"FAIL {name}: {exception.GetType().Name}: {exception.Message}");
        }

        var after = ReadStats();
        Console.WriteLine(
            $"     hooks +{after.Hooked - before.Hooked}, slow +{after.SlowCalls - before.SlowCalls}, " +
            $"trips +{after.Trips - before.Trips}");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    //ResolveTripEntry hands the guard a real native entry for the throw method
    //The lookup has to happen while the guard is disarmed; PrepareMethod is what makes GetFunctionPointer answer the
    //JITted entry rather than a stub that is fixed up on the first call
    private static IntPtr ResolveTripEntry()
    {
        var method = typeof(StackTrip).GetMethod(
            nameof(StackTrip.Trip), BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(nameof(StackTrip), nameof(StackTrip.Trip));

        RuntimeHelpers.PrepareMethod(method.MethodHandle);
        return method.MethodHandle.GetFunctionPointer();
    }

    private static HookStats ReadStats()
    {
        _ = ncn_hook_stats(out var stats);
        return stats;
    }

    //PrintDebugState compares the TLS entry the trampoline reads with the one the helper sees
    private static void PrintDebugState()
    {
        var values = new IntPtr[7];
        if (ncn_hook_debug_state(values, values.Length) != 0)
            return;

        Console.WriteLine(
            $"thread state: tls slot {values[0]}, array 0x{values[1].ToInt64():X}, " +
            $"teb array slot 0x{values[2].ToInt64():X}, teb tls slots 0x{values[3].ToInt64():X}, " +
            $"api slot 0x{values[4].ToInt64():X}, limit 0x{values[5].ToInt64():X}, rsp 0x{values[6].ToInt64():X}");
    }

    private static void WriteLogTail()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "nacrecraft_native.log");
            if (!File.Exists(path))
                return;

            var lines = File.ReadAllLines(path);
            Console.WriteLine("--- native log (tail) ---");
            foreach (var line in lines.TakeLast(12))
                Console.WriteLine(line);
        }
        catch (IOException)
        {
            //The log is diagnostic only; a run that cannot read it still has its results
        }
    }

    //---- regression ----

    //Every method below contains a call, which is what the guard's scan requires before it hooks anything
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Helper(int value) => value + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long HelperLong(long value) => value + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double Scale(double value) => value * 2.5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AddThroughHelper(int left, int right) => Helper(left) + right;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double ScaleThroughHelper(double value) => Scale(value) + 1.0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SumFour(long a, long b, long c, long d) => HelperLong(a) + b + c + d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SumFive(long a, long b, long c, long d, long e) => HelperLong(a) + b + c + d + e;

    private static void Regression()
    {
        for (var i = 0; i < 2000; i++)
        {
            Expect(AddThroughHelper(i, 2) == i + 3, $"int method returned {AddThroughHelper(i, 2)} at {i}");
            Expect(ScaleThroughHelper(i) == i * 2.5 + 1.0, $"double method returned a different value at {i}");
            Expect(SumFour(i, 1, 2, 3) == i + 7, $"four-argument method returned a different value at {i}");
            Expect(SumFive(i, 1, 2, 3, 4) == i + 11, $"five-argument method returned a different value at {i}");
        }
    }

    //---- deep recursion ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Recurse(int depth)
    {
        _depth = depth;

        //A real frame keeps the recursion from being flattened and gives the guard something to run out of
        Span<long> padding = stackalloc long[4];
        padding[0] = depth;
        return Recurse(depth + 1) + (int)padding[0];
    }

    private static void DeepRecursionWithoutHandler()
    {
        _depth = 0;
        var caught = false;
        try
        {
            _ = Recurse(0);
        }
        catch (InsufficientExecutionStackException)
        {
            caught = true;
        }

        Expect(caught, "no guard exception reached the handler");
        Expect(_depth > 500, $"recursion stopped after only {_depth} frames, which points at the guard page");
    }

    //A second thread gets its own stack limits and its own TLS state, so the guard has to hold there as well
    private static void WorkerThreadRecursion()
    {
        _depth = 0;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                _ = Recurse(0);
            }
            catch (InsufficientExecutionStackException)
            {
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }, 1024 * 1024);

        thread.Start();
        thread.Join();

        Expect(failure is null, $"the worker thread failed with {failure?.GetType().Name}");
        Expect(_depth > 500, $"the worker thread stopped after {_depth} frames");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RecurseFinally(int depth)    {
        _finallyDepth = depth;
        try
        {
            RecurseFinally(depth + 1);
        }
        finally
        {
            _finallyRuns++;
        }
    }

    private static void FinallyRuns()
    {
        _finallyRuns = 0;
        _finallyDepth = 0;
        var caught = false;
        try
        {
            RecurseFinally(0);
        }
        catch (InsufficientExecutionStackException)
        {
            caught = true;
        }

        Expect(caught, "no guard exception reached the handler");
        Expect(_finallyRuns > 0, "no finally block ran while the stack unwound");
        Expect(_finallyRuns >= _finallyDepth, $"only {_finallyRuns} finally blocks ran for {_finallyDepth} frames");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RecurseUsing(int depth)
    {
        using var lease = new Lease();
        RecurseUsing(depth + 1);
    }

    private sealed class Lease : IDisposable
    {
        public void Dispose() => _disposeRuns++;
    }

    private static void UsingDisposes()
    {
        _disposeRuns = 0;
        var caught = false;
        try
        {
            RecurseUsing(0);
        }
        catch (InsufficientExecutionStackException)
        {
            caught = true;
        }

        Expect(caught, "no guard exception reached the handler");
        Expect(_disposeRuns > 0, "no using block disposed while the stack unwound");
    }

    private static bool TripOnce()    {
        try
        {
            _ = Recurse(0);
            return false;
        }
        catch (InsufficientExecutionStackException)
        {
            return true;
        }
    }

    private static void RepeatTrip()
    {
        var first = TripOnce();
        var second = TripOnce();
        Expect(first, "the first descent did not trip");
        Expect(second, "the second descent did not trip, so the quiet period never ended");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RecurseGc(int depth)
    {
        try
        {
            RecurseGc(depth + 1);
        }
        catch (InsufficientExecutionStackException)
        {
            //This frame is still deep in the descent, which is the point of collecting here
            if (!_gcRan)
            {
                _gcRan = true;
                _gcDepth = depth;
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            }
        }
    }

    private static void GcWhileShort()
    {
        _gcRan = false;
        _gcDepth = 0;

        //The handler swallows the trip at the deepest frame; rethrowing from every frame is a runtime stress that
        //overflows on its own, with or without the guard, so it is not what this check is about
        RecurseGc(0);

        Expect(_gcRan, "the catch never ran, so no collection happened near the limit");
        Expect(_gcDepth > 500, $"the collection ran at depth {_gcDepth}, which is not near the limit");
    }

    //---- tiered compilation ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BenchTarget(int value) => Helper(value) + 1;

    private static int AddressCount()
    {
        var buffer = new IntPtr[4096];
        return ncn_hook_addresses(buffer, buffer.Length);
    }

    private static void PromotedMethod()
    {
        var before = AddressCount();
        var clock = Stopwatch.StartNew();
        var sum = 0L;
        var calls = 0L;

        //The loop is only a smoke check; the address count is what says a second version was compiled
        while (clock.ElapsedMilliseconds < 300)
        {
            sum += BenchTarget((int)(calls & 0xFFFF));
            calls++;
        }

        Expect(calls > 1000, $"the loop ran only {calls} calls");
        Expect(sum > 0, "the promoted method returned nothing");

        Thread.Sleep(100);
        var after = AddressCount();
        Console.WriteLine($"hooked addresses {before} -> {after} after {calls} calls");
        Expect(after > before, "no second version was hooked, so promotion was not covered");
    }

    //---- hot path measurement ----

    private static int Bench(int iterations)
    {
        //The baseline run leaves NCN_STACK_HOOK unset and skips this; the measured run arms the guard first
        if (ncn_stack_hook_enabled() != 0)
        {
            _ = ncn_configure_stack_guard(IntPtr.Zero, UIntPtr.Zero);
            var trip = ResolveTripEntry();
            _ = ncn_configure_stack_guard(trip, (UIntPtr)ReserveBytes);
        }

        var clock = Stopwatch.StartNew();
        var sum = 0;
        for (var i = 0; i < iterations; i++)
            sum += BenchTarget(i & 0xFFFF);
        clock.Stop();

        Console.WriteLine(
            $"bench: {iterations} calls in {clock.Elapsed.TotalMilliseconds:F1} ms, {clock.Elapsed.TotalNanoseconds / iterations:F2} ns per call, sum {sum}");
        return 0;
    }
}
