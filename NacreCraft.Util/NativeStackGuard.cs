using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NacreCraft.Logging;

namespace NacreCraft.Util;

//NativeStackGuard connects the managed crash path to the native layer
//
//The stack overflow callback and the throw entry live here. The restart that gets the layer into the process sits in
//NacreCraft.Boot instead: it has to run before any kernel assembly is loaded, while this type is the entry point the
//native layer jumps to on a trip and therefore cannot move out of the kernel.
public static class NativeStackGuard
{
    //LibraryName file the native layer builds into, without extension
    private const string LibraryName = "nacrecraft_native";

    //Trip is the entry the machine-code guard jumps to when the stack runs short
    //The trampoline reaches it with the caller's return address on the stack, so the throw unwinds as if the method
    //whose entry was replaced had thrown it; the native side owns the quiet period around the trip
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Trip()
    {
        throw new InsufficientExecutionStackException();
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StackOverflowCallback(IntPtr faultingAddress);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_register_stack_overflow_callback(IntPtr callback, byte[] reportDirectory, UIntPtr length);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_stack_hook_enabled();

    //tripEntry is zero while the guard is disarmed; reserve is read from NCN_STACK_RESERVE when zero
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ncn_configure_stack_guard(IntPtr tripEntry, UIntPtr reserve);

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

        //The throw entry is resolved before anything is armed: finding it compiles the method, and a hook installed
        //on that entry would send every trip back through the check it just failed
        var tripEntry = ResolveTripEntry();

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
            ArmMachineGuard(tripEntry);
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

    //ResolveTripEntry arms the machine-code guard with a real native entry for Trip
    //The lookup is done while the guard is disarmed, and a native layer without the new entry points leaves it alone
    private static IntPtr ResolveTripEntry()
    {
        try
        {
            _ = ncn_configure_stack_guard(IntPtr.Zero, UIntPtr.Zero);
        }
        catch (DllNotFoundException)
        {
            return IntPtr.Zero;
        }
        catch (EntryPointNotFoundException)
        {
            return IntPtr.Zero;
        }

        try
        {
            var method = typeof(NativeStackGuard).GetMethod(
                nameof(Trip), BindingFlags.NonPublic | BindingFlags.Static);
            if (method is null)
            {
                Log.Warning("The stack guard throw entry could not be found, the machine-code guard stays off");
                return IntPtr.Zero;
            }

            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            return method.MethodHandle.GetFunctionPointer();
        }
        catch (Exception exception)
        {
            Log.Warning($"The stack guard throw entry could not be resolved: {exception.GetType().Name}");
            return IntPtr.Zero;
        }
    }

    //ArmMachineGuard hands the throw entry over when the machine-code guard was asked for
    //A zero reserve lets the native side read NCN_STACK_RESERVE and fall back to its default
    private static void ArmMachineGuard(IntPtr tripEntry)
    {
        if (tripEntry == IntPtr.Zero)
            return;

        try
        {
            if (ncn_stack_hook_enabled() == 0)
                return;

            var result = ncn_configure_stack_guard(tripEntry, UIntPtr.Zero);
            if (result != 0)
            {
                Log.Warning($"The native layer refused the machine-code guard, code {result}");
                return;
            }

            var address = tripEntry.ToInt64().ToString("X16");
            Log.Info($"The machine-code stack guard is armed at 0x{address}");
        }
        catch (EntryPointNotFoundException)
        {
            Log.Warning("The native layer has no machine-code guard entry points, the early check stays off");
        }
        catch (Exception exception)
        {
            Log.Warning($"Arming the machine-code guard failed: {exception.GetType().Name}: {exception.Message}");
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
