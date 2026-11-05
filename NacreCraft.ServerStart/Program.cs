using System.Runtime.CompilerServices;
using NacreCraft;
using NacreCraft.Config;
using NacreCraft.Game;
using NacreCraft.Logging;
using NacreCraft.Network.Chat;
using NacreCraft.Util;
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
    //so at runtime the callback fetches their bytes from the kernel directory. This must run before any kernel type is resolved.
    //The JIT resolves every type appearing in a method body, so the two layers below are marked no-inline,
    //otherwise ServerMain gets resolved early and bypasses the rewrite, bringing up an un-injected NacreCraft.Server.
    public static int Main(string[] args)
    {
        EarlyCrashGuard.Install();
        EmbeddedAssemblyLoader.Initialize();
        args = PrepareArgs(args);
        var problems = BootMods(args);
        return Launch(args, problems);
    }

    //PrepareArgs merges Server.Args into the command line before anything reads the arguments
    //BootMods scans for --debug and ServerMain reads nogui/noconsole/trace itself, so the merge has to finish first
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string[] PrepareArgs(string[] args)
        => LaunchArgsFile.Merge(args, Path.Combine(AppPaths.BaseDirectory, "Server.Args"));

    //BootMods mod bootstrap; this layer only touches the loader, not server types
    //--debug must open the log output here: injection runs before kernel Initialize, and one step late drops the whole run of debug records
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IReadOnlyList<string>? BootMods(string[] args)
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
        return ModBootstrap.Run(ModEnvironment.Server).Problems;
#else
        return null;
#endif
    }

    //Launch server startup flow; only at this layer is resolving NacreCraft.Server allowed
    //The failure window waits until here: drawing it needs the language table, and loading a kernel assembly any earlier
    //would close the injection window for every mod that comes after
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Launch(string[] args, IReadOnlyList<string>? problems)
    {
        if (problems is { Count: > 0 } && WindowAllowed())
        {
            NcLanguageFiles.Extract();
            NcLanguage.Load(Language.Default);
            NativeErrorWindow.ShowModLoadFailure(string.Join(Environment.NewLine, problems), problems.Count);
        }

        ServerMain.Run(args);
        return 0;
    }

    //WindowAllowed reports whether there is a desktop to draw on; the same two spellings ServerMain accepts
    private static bool WindowAllowed()
    {
        var args = Environment.GetCommandLineArgs();
        return Array.IndexOf(args, "--nogui") < 0 && Array.IndexOf(args, "nogui") < 0;
    }
}
