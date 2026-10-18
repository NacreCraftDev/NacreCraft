using System.Reflection;
using NacreCraft.Logging;

namespace NacreCraft;

//AgentBootstrap loads an agent assembly at the earliest stage of startup, standing in for the JVM's -javaagent
//It belongs to the main library rather than to one launcher, because every entry point loads the main library first:
//the launcher, the standalone server and the standalone client all get an agent without wiring it up separately
//Mod bootstrap and the kernel have not run yet at this point, so an agent always sees the kernel untouched
internal static class AgentBootstrap
{
    //EntryVariable holds the entry method name; a bare name like libmain is the default
    private const string EntryVariable = "NACRECRAFT_AGENT_ENTRY";
    private const string DefaultEntry = "libmain";

    //Run scans the command line for the first --agent and loads the assembly it names
    //Only one agent is honoured, mirroring -javaagent: a later occurrence is ignored rather than chained
    //Only debug runs one; the argument itself is claimed by LaunchOptions either way, so it never reaches the broadcast
    public static void Run(string[] args)
    {
        if (Array.IndexOf(args, "--debug") < 0) return;

        for (int i = 0; i < args.Length; i++)
        {
            var token = args[i];
            string? path = null;

            if (token.StartsWith("--agent=", StringComparison.Ordinal))
                path = token["--agent=".Length..];
            else if (token == "--agent" && i + 1 < args.Length)
                path = args[++i];

            if (string.IsNullOrWhiteSpace(path)) continue;

            Load(Path.IsPathRooted(path) ? path : Path.Combine(AppPaths.BaseDirectory, path));
            return;
        }
    }

    //Load resolves the assembly into the default context and invokes its entry
    //No isolated context: an agent exists to call back into the kernel, and isolation would force a separate
    //load/unload path for every host type it touches
    private static void Load(string path)
    {
        if (!File.Exists(path))
        {
            Log.Info($"agent assembly not found: {path}");
            return;
        }

        var entry = Environment.GetEnvironmentVariable(EntryVariable);
        if (string.IsNullOrWhiteSpace(entry)) entry = DefaultEntry;

        var assembly = Assembly.LoadFrom(path);
        var method = FindEntry(assembly, entry);
        if (method is null)
        {
            Log.Info($"agent entry '{entry}' not found in {Path.GetFileName(path)}");
            return;
        }

        Log.Info($"agent loading {Path.GetFileName(path)} entry={entry}");
        //Blocking on purpose: nothing else may run until the agent returns
        method.Invoke(null, null);
        Log.Info($"agent finished {Path.GetFileName(path)}");
    }

    //FindEntry searches every type for a parameterless static method of the given name
    //The contract names a bare function rather than a qualified one, so the whole assembly has to be walked
    private static MethodInfo? FindEntry(Assembly assembly, string name)
    {
        foreach (var type in assembly.GetTypes())
        {
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method is not null && method.GetParameters().Length == 0) return method;
        }
        return null;
    }
}
