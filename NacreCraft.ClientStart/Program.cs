using System.Runtime.CompilerServices;
using NacreCraft;
using NacreCraft.Boot;
using NacreCraft.Game;
using NacreCraft.Client;
using NacreCraft.Network.Chat;
using NacreCraft.Util;
#if !NC_NO_MODLOADER
using NacreCraft.ModLoader;
#endif

namespace NacreCraft.ClientStart;

//Program standalone client entry point
//Only handles the process entry and mod bootstrap; the real client implementation stays in the NacreCraft.Client class library
public static class Program
{
    //Main process entry
    //First thing is registering the kernel assembly resolution callback: kernel assemblies were moved out of deps.json,
    //so at runtime the callback fetches their bytes from the kernel directory. This must run before any kernel type is resolved.
    //The JIT resolves every type appearing in a method body, so the two layers below are marked no-inline,
    //otherwise ClientMain gets resolved early and bypasses the rewrite, bringing up an un-injected NacreCraft.Client.
    public static int Main(string[] args)
    {
        EarlyCrashGuard.Install();
        EmbeddedAssemblyLoader.Initialize();
        args = PrepareArgs(args);
        var problems = BootMods();
        return Launch(args, problems);
    }

    //PrepareArgs merges Client.Args into the command line before anything reads the arguments
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string[] PrepareArgs(string[] args)
        => LaunchArgsFile.Merge(args, Path.Combine(AppPaths.BaseDirectory, "Client.Args"));

    //BootMods mod bootstrap; this layer only touches the loader, not client types
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IReadOnlyList<string>? BootMods()
    {
#if !NC_NO_MODLOADER
        return ModBootstrap.Run(ModEnvironment.Client).Problems;
#else
        return null;
#endif
    }

    //Launch client startup flow; only at this layer is resolving NacreCraft.Client allowed
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

        ClientMain.Run(args);
        return 0;
    }

    //WindowAllowed reports whether there is a desktop to draw on; the same two spellings ClientMain accepts
    private static bool WindowAllowed()
    {
        var args = Environment.GetCommandLineArgs();
        return Array.IndexOf(args, "--nogui") < 0 && Array.IndexOf(args, "nogui") < 0;
    }
}
