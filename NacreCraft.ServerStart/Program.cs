using System.Runtime.CompilerServices;
using NacreCraft;
using NacreCraft.Config;
using NacreCraft.Game;
using NacreCraft.Logging;
#if !NC_NO_MODLOADER
using NacreCraft.ModLoader;
#endif

namespace NacreCraft.ServerStart;

//Program standalone server entry point
//Only handles the process entry and mod bootstrap; the real server implementation stays in the NacreCraft.Server class library
public static class Program
{
    //Main process entry
    //First thing is registering the kernel assembly resolution callback: kernel assemblies were moved out of deps.json,
    //so at runtime the callback fetches their bytes from the kernel directory and the main library's embedded resources. This must run before any kernel type is resolved.
    //The JIT resolves every type appearing in a method body, so the two layers below are marked no-inline,
    //otherwise ServerMain gets resolved early and bypasses the rewrite, bringing up an un-injected NacreCraft.Server.
    public static int Main(string[] args)
    {
        EmbeddedAssemblyLoader.Initialize();
        args = PrepareArgs(args);
        BootMods(args);
        return Launch(args);
    }

    //PrepareArgs merges Server.Args into the command line before anything reads the arguments
    //BootMods scans for --debug and ServerMain reads nogui/noconsole/trace itself, so the merge has to finish first
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string[] PrepareArgs(string[] args)
        => LaunchArgsFile.Merge(args, Path.Combine(AppPaths.BaseDirectory, "Server.Args"));

    //BootMods mod bootstrap; this layer only touches the loader, not server types
    //--debug must open the log output here: injection runs before kernel Initialize, and one step late drops the whole run of debug records
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BootMods(string[] args)
    {
        if (Array.IndexOf(args, "--debug") >= 0)
            DebugMode.IsEnabled = true;

        if (DebugMode.IsEnabled)
        {
            Log.SetConsoleLevel(LogLevel.Debug);
            Log.SetFileLevel(LogLevel.Debug);
            Log.SetVerbose(true);
        }

#if !NC_NO_MODLOADER
        ModBootstrap.Run(ModEnvironment.Server);
#endif
    }

    //Launch server startup flow; only at this layer is resolving NacreCraft.Server allowed
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Launch(string[] args)
    {
        ServerMain.Run(args);
        return 0;
    }
}
