using System.Runtime.CompilerServices;
using NacreCraft;
using NacreCraft.Game;
using NacreCraft.Client;
using NacreCraft.ModLoader;

namespace NacreCraft.ClientExe;

//Program standalone client entry point
//Only handles the process entry and mod bootstrap; the real client implementation stays in the NacreCraft.Client class library
public static class Program
{
    //Main process entry
    //First thing is registering the kernel assembly resolution callback: kernel assemblies were moved out of deps.json,
    //so at runtime the callback fetches their bytes from the kernel directory and the main library's embedded resources. This must run before any kernel type is resolved.
    //The JIT resolves every type appearing in a method body, so the two layers below are marked no-inline,
    //otherwise ClientMain gets resolved early and bypasses the rewrite, bringing up an un-injected NacreCraft.Client.
    public static int Main(string[] args)
    {
        EmbeddedAssemblyLoader.Initialize();
        BootMods();
        return Launch(args);
    }

    //BootMods mod bootstrap; this layer only touches the loader, not client types
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BootMods() => ModBootstrap.Run(ModEnvironment.Client);

    //Launch client startup flow; only at this layer is resolving NacreCraft.Client allowed
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Launch(string[] args)
    {
        ClientMain.Run(args);
        return 0;
    }
}
