using System.Diagnostics;
using System.Runtime.InteropServices;
using NacreCraft.Logging;

namespace NacreCraft.Boot;

//NativeRelaunch restarts the process once so the native layer is loaded from its first instruction
//The runtime only reads the profiler variables while it starts, so a process that is already running cannot be
//attached afterwards and a restart is the only way in
public static class NativeRelaunch
{
    //LibraryName file the native layer builds into, without extension
    private const string LibraryName = "nacrecraft_native";

    //The native library's file name on each platform; a cdylib has no prefix on Windows
    private static readonly string[] LibraryNames =
    {
        "nacrecraft_native.dll",
        "libnacrecraft_native.so",
        "libnacrecraft_native.dylib",
    };

    //ProfilerGuid must match the CLSID the native library reports
    private const string ProfilerGuid = "{2B5F8D34-6C1A-4E27-9B3D-52E8714AC618}";

    //AttachedFlag marks a process that has already been restarted once for the native layer
    //Without it a run where the layer fails to load would restart endlessly
    private const string AttachedFlag = "NC_NATIVE_ATTACHED";

    //Console bits needed to hand an interrupt to the child and keep escape sequences working
    private const uint CtrlCEvent = 0;
    private const uint StdOutputHandle = 0xFFFFFFF5;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(uint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr handle, uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GenerateConsoleCtrlEvent(uint ctrlEvent, uint processGroupId);

    /// <summary>
    /// RelaunchIfNeeded restarts this process once so the native layer is loaded from its first instruction
    /// Returns without doing anything when the layer is already in, when the platform cannot carry it, or when another
    /// profiler has claimed the process; in those cases this process simply goes on without the early stack check
    /// When it does restart, this never returns: the parent waits for the child and exits with the child's code
    /// </summary>
    public static void RelaunchIfNeeded()
    {
        //--no-restart keeps the process in place; the early stack check stays off for this run
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--no-restart") >= 0) return;

        //Restarted once already: if the layer is still missing, restarting again would loop forever
        if (Environment.GetEnvironmentVariable(AttachedFlag) == "1") return;

        //Another profiler already holds this process. It is not ours to displace, so the check stays off and the server
        //carries on; this is the case when the hook layer has been attached for runtime injection
        var existing = Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH");
        if (!string.IsNullOrEmpty(existing))
        {
            Log.Warning($"Another profiler already holds this process ({existing}), the early stack check stays off");
            return;
        }

        var library = FindNativeLibrary();
        if (library is null)
        {
            Log.Warning("The native layer was not found next to the program, the early stack check stays off");
            return;
        }

        var executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable))
        {
            Log.Warning("The running executable could not be located, restarting with the native layer is not possible");
            return;
        }

        var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = false };

        //Entry zero is the entry assembly, which is the executable only when the host was started directly. A launch
        //through the dotnet host leaves the assembly path sitting in entry zero instead, so it has to be handed back
        //as the first argument; dropping it restarts the bare host, which prints its usage and exits
        var arguments = Environment.GetCommandLineArgs();
        if (IsDotnetHost(executable)) startInfo.ArgumentList.Add(arguments[0]);
        for (var i = 1; i < arguments.Length; i++) startInfo.ArgumentList.Add(arguments[i]);

        startInfo.Environment["CORECLR_ENABLE_PROFILING"] = "1";
        startInfo.Environment["CORECLR_PROFILER"] = ProfilerGuid;
        startInfo.Environment["CORECLR_PROFILER_PATH"] = library;
        startInfo.Environment[AttachedFlag] = "1";

        //The stack check entry is named here rather than compiled into the native library, so moving it later only
        //changes these three lines. The native side falls back to the same defaults when they are absent
        startInfo.Environment["NCN_STACK_CHECK_ASSEMBLY"] = "NacreCraft.Util";
        startInfo.Environment["NCN_STACK_CHECK_TYPE"] = "NacreCraft.Util.NativeStackGuard";
        startInfo.Environment["NCN_STACK_CHECK_METHOD"] = "EnsureStack";

        //The rewrite list is left alone: the layer decides on its own when the variable is absent, and that default
        //covers every assembly except the framework ones, mods included

        EnableVirtualTerminal();

        Log.Info("Restarting with the native layer so stack depth is checked before the stack runs out");
        using var child = Process.Start(startInfo);
        if (child is null)
        {
            Log.Warning("The restarted process did not start");
            return;
        }

        ForwardInterrupt(child);

        child.WaitForExit();
        Environment.Exit(child.ExitCode);
    }

    //ForwardInterrupt hands the interrupt key to the child, since the console belongs to this process
    //Cancelling the default keeps this process alive and raises the event on the child's console instead
    private static void ForwardInterrupt(Process child)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            if (child.HasExited) return;
            //On Unix the interrupt already reaches the whole foreground process group, so only Windows needs the hop
            if (!OperatingSystem.IsWindows()) return;
            try
            {
                AttachConsole((uint)child.Id);
                GenerateConsoleCtrlEvent(CtrlCEvent, 0);
                FreeConsole();
            }
            catch (Exception exception)
            {
                Log.Warning($"Forwarding the interrupt to the restarted process failed: {exception.GetType().Name}");
            }
        };
    }

    //EnableVirtualTerminal turns on escape sequence handling for the console the child shares
    //The setting belongs to the console rather than the process, so enabling it here also covers the child's output
    private static void EnableVirtualTerminal()
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = GetStdHandle(StdOutputHandle);
        if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;
        if (!GetConsoleMode(handle, out var mode)) return;
        _ = SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
    }

    //IsDotnetHost reports whether this process was started as the shared host rather than as the program itself
    private static bool IsDotnetHost(string executable) =>
        string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase);

    //FindNativeLibrary looks beside the program first, then under the per-platform release layout
    //The host resolves runtimes/<rid>/native on its own for ordinary native calls, but the profiler path is handed to
    //the runtime by name, so the entry for this platform has to be picked out here
    private static string? FindNativeLibrary()
    {
        var explicitPath = Environment.GetEnvironmentVariable("NC_NATIVE_PATH");
        if (!string.IsNullOrEmpty(explicitPath) && File.Exists(explicitPath))
            return explicitPath;

        var directories = new List<string> { AppContext.BaseDirectory };
        var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
        if (!string.IsNullOrEmpty(runtimeIdentifier))
        {
            directories.Add(Path.Combine(AppContext.BaseDirectory, "runtimes", runtimeIdentifier, "native"));
        }

        foreach (var directory in directories)
        {
            foreach (var name in LibraryNames)
            {
                var candidate = Path.Combine(directory, name);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
}
