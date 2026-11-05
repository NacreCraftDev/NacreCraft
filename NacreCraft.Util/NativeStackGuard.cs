using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NacreCraft.Logging;

namespace NacreCraft.Util;

//NativeStackGuard connects the managed crash path to the native layer
//
//The stack overflow callback and the injected early check live here. The restart that gets the layer into the process
//sits in NacreCraft.Boot instead: it has to run before any kernel assembly is loaded, while this type is the injected
//entry point the native layer looks up by name and therefore cannot move out of the kernel.
public static class NativeStackGuard
{
    //LibraryName file the native layer builds into, without extension
    private const string LibraryName = "nacrecraft_native";

    //Quiet, per thread, whether this thread has already reported and is waiting for room to come back
    //Per thread rather than shared: one thread's descent must not silence another's report
    [ThreadStatic]
    private static bool _quiet;

    //EnsureStack is the entry the injected call points at
    //
    //The test is the runtime's own stack probe, which it answers from a limit it has already cached rather than by
    //asking the system. The common case is a field read and one call inside the runtime; going through the native layer
    //meant a managed to native transition, a look at the thread's stack limits and a dynamic TLS access on every method
    //call, and the level generation path calls methods often enough for that to become the work
    //
    //It is written to be inlined into every injected call site. A call left as a call costs a frame push and a return
    //on every method entry of the whole ecosystem, which showed up as a large, call-count proportional slowdown; kept
    //small and inline the entry costs a handful of instructions. The throw lives in its own method so the body stays
    //under the inline budget, and the inline hint is only a hint, so the shape matters more than the attribute
    //
    //The quiet flag is what the probe alone cannot express. On its own it keeps answering that the stack is short while
    //the exception is unwound and handled, and the process dies on its way to reporting the problem. A thread that has
    //tripped stays quiet until the probe says there is room again, which leaves room for the unwind, the catch block
    //and anything that logs along the way
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureStack()
    {
        if (_quiet)
        {
            //Room again means the exception was handled and this thread may report the next one
            if (RuntimeHelpers.TryEnsureSufficientExecutionStack()) _quiet = false;
            return;
        }

        if (RuntimeHelpers.TryEnsureSufficientExecutionStack()) return;

        Trip();
    }

    //Trip reports the overflow once per descent; never inlined, it is the cold half of EnsureStack
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Trip()
    {
        _quiet = true;
        throw new InsufficientExecutionStackException();
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StackOverflowCallback(IntPtr faultingAddress);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_register_stack_overflow_callback(IntPtr callback, byte[] reportDirectory, UIntPtr length);

    //The delegate is kept alive for the process lifetime: the native side holds its address and would otherwise end up
    //calling a collected stub
    private static StackOverflowCallback? _callback;
    private static bool _installed;

    /// <summary>
    /// Register takes the stack overflow callback from the native layer
    /// Returns whether the layer was present and accepted it; a missing layer only costs the richer report
    /// </summary>
    public static bool Register()
    {
        if (_installed) return true;

        try
        {
            _callback = OnStackOverflow;
            var pointer = Marshal.GetFunctionPointerForDelegate(_callback);
            var directory = Encoding.UTF8.GetBytes(CrashHandler.CrashReportsDir);
            var result = ncn_register_stack_overflow_callback(pointer, directory, (UIntPtr)directory.Length);
            if (result != 0)
            {
                Log.Warning($"The native layer refused the stack overflow callback, code {result}");
                _callback = null;
                return false;
            }

            _installed = true;
            Log.Info("The native layer is in place and will report a stack overflow");
            return true;
        }
        catch (DllNotFoundException)
        {
            //Not loaded, which is the normal case when profiling was never enabled for this run
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            Log.Warning("The native layer is present but has no stack overflow entry point");
            return false;
        }
    }

    //Installed whether the native layer took the callback
    public static bool Installed => _installed;

    //OnStackOverflow runs on the thread the native layer starts, so a stack is available here
    //The thread that overflowed is still stuck where it failed, so its stack cannot be described; what this can carry
    //is the address it failed at and the fact that a stack overflow was the cause
    private static void OnStackOverflow(IntPtr faultingAddress)
    {
        try
        {
            var address = $"{faultingAddress.ToInt64():X16}";
            var report = new CrashReport("A stack overflow ended the process",
                new StackOverflowException($"The stack was exhausted at address 0x{address}"));
            CrashHandler.Save(report, new[]
            {
                "A stack overflow cannot be caught by the runtime, so this report comes from the native layer",
                $"The thread that overflowed is stopped at 0x{address} and its stack cannot be read",
            }, showWindow: false);
        }
        catch (Exception exception)
        {
            //The process is already on its way out; all this branch can do is say that reporting failed
            Log.Critical($"Writing the stack overflow report failed: {exception.GetType().Name}: {exception.Message}");
        }
    }
}
