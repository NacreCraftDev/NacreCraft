using NacreCraft.Config;
using NacreCraft;
using NacreCraft.Game;
using NacreCraft.Client;
using NacreCraft.Logging;
using NacreCraft.ModLoader;

namespace NacreCraft.Loader;

//NacreCraft.Loader launcher entry, maps to vanilla launcher
//Console EXE dispatches by mode, calling ServerMain.Run or ClientMain.Run
//Defaults to --client mode; the remaining args are passed through to Run and parsed by the existing LaunchOptions+GameOptions mechanism
public static class Program
{
    //Main launcher entry
    //Does exactly one thing: register the kernel assembly resolution callback. Kernel assemblies were moved out of deps.json,
    //so at runtime the callback fetches their bytes from the kernel directory and the main library's embedded resources. This must run before any kernel type is resolved,
    //and since the JIT resolves every type appearing in a method body, the real startup logic must live in another method.
    public static int Main(string[] args)
    {
        EmbeddedAssemblyLoader.Initialize();
        return Launch(args);
    }

    //Launch startup logic
    //Declare --server/--client as kernel flags so LaunchOptions swallows them automatically without polluting downstream business args
    //Scan args to detect the mode, then pass the whole args through to the matching Run
    private static int Launch(string[] args)
    {
        Log.Debug($"Main entry args={string.Join(",", args)}");
        LaunchOptions.DeclareKernelFlag("server");
        LaunchOptions.DeclareKernelFlag("client");
        LaunchOptions.DeclareKernelFlag("debug");
        //--agent is consumed unconditionally so it never leaks into the business argument broadcast, including on the
        //restart that installs the profiler; only debug mode goes on to act on its value
        LaunchOptions.DeclareKernelOption("agent");

        //Detect the --debug flag to enable global debug mode, triggering NacreCraftKernel.PrintStartupInfo and other debug behavior
        if (Array.IndexOf(args, "--debug") >= 0)
            DebugMode.IsEnabled = true;

        //Mod injection runs before kernel Initialize; if the log output is not opened early here, the whole run of debug records from the injection process is dropped
        if (DebugMode.IsEnabled)
        {
            Log.SetConsoleLevel(LogLevel.Debug);
            Log.SetFileLevel(LogLevel.Debug);
            Log.SetVerbose(true);
        }

        //The agent is the earliest injected code: it runs before mode detection and before mod bootstrap, and it is
        //deliberately placed ahead of the crash handler, so a fault in it takes the process down with it
        if (DebugMode.IsEnabled)
            AgentBootstrap.Run(args);

        var mode = DetectMode(args);

        //Mod bootstrap must run before kernel sub-libraries are resolved, so it sits between mode detection and RunServer/RunClient
        //This step statically scans the mods directory and assembles injection rules for the embedded loader; sub-libraries loaded on demand later get rewritten
        ModBootstrap.Run(mode == LaunchMode.Server ? ModEnvironment.Server : ModEnvironment.Client);

        var result = mode switch
        {
            LaunchMode.Server => RunServer(args),
            _ => RunClient(args)
        };
        Log.Debug($"Main exit result={result}");
        return result;
    }

    //DetectMode scans args and takes the first --server/--client to decide the mode
    //When no mode flag is passed, defaults to Client, aligning with vanilla client-first behavior
    private static LaunchMode DetectMode(string[] args)
    {
        //Log.Debug($"DetectMode entry args={string.Join(",", args)}");
        foreach (var arg in args)
        {
            if (arg == "--server")
            {
                //Log.Debug($"DetectMode exit result={LaunchMode.Server}");
                return LaunchMode.Server;
            }
            if (arg == "--client")
            {
                //Log.Debug($"DetectMode exit result={LaunchMode.Client}");
                return LaunchMode.Client;
            }
        }
        //Log.Debug($"DetectMode exit result={LaunchMode.Client}");
        return LaunchMode.Client;
    }

    //RunServer passes args through to ServerMain.Run
    //ServerMain subscribes to GameOptions internally, then triggers NacreCraftKernel.Initialize to parse the remaining args
    private static int RunServer(string[] args)
    {
        //Log.Debug($"RunServer entry args={string.Join(",", args)}");
        ServerMain.Run(args);
        //Log.Debug($"RunServer exit result=0");
        return 0;
    }

    //RunClient passes args through to ClientMain.Run
    //ClientMain subscribes to GameOptions internally, then triggers NacreCraftKernel.Initialize to parse the remaining args
    private static int RunClient(string[] args)
    {
        Log.Debug("Calling ClientMain.Run...");
        ClientMain.Run(args);
        Log.Debug("RunClient exit");
        return 0;
    }
}

//LaunchMode startup mode enum
//Client includes rendering and input; Server is a pure server
internal enum LaunchMode
{
    Client,
    Server
}
