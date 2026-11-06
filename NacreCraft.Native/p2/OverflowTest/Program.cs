using System.Runtime.CompilerServices;
using NacreCraft.Util;

namespace OverflowTest;

//OverflowTest drives a recursion to its end so the guard modes can be compared on the same work
//
//The recursion has no try block of its own and the call's result is used, so nothing turns it into a tail call. What
//happens when the stack runs out is the whole point: with a guard the exception is caught here, without one the runtime
//ends the process on the guard page.
internal static class Program
{
    private static int _depth;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Recurse(int depth)
    {
        _depth = depth;
        return Recurse(depth + 1) + 1;
    }

    private static int Main()
    {
        var mode = Environment.GetEnvironmentVariable("NCN_OVERFLOW_MODE") ?? "unknown";
        var registered = NativeStackGuard.Register();
        Console.WriteLine($"mode={mode} registered={registered}");

        try
        {
            var result = Recurse(0);
            Console.WriteLine($"returned={result} depth={_depth}");
            return 2;
        }
        catch (InsufficientExecutionStackException)
        {
            Console.WriteLine($"caught=InsufficientExecutionStackException depth={_depth}");
            return 0;
        }
    }
}
