using System.Runtime.Loader;
using NacreCraft.Logging;
using NacreCraft.Network.Chat;
using NacreCraft.Util;

namespace NacreCraft.ModLoader;

//ModBootstrap: mod bootstrap entry point
//Must be hooked at the very start of every executable entry; kernel assemblies resolve on demand, and once they come in there is no chance to rewrite
public static class ModBootstrap
{
    //Run: statically scans the mods directory, assembles injection rules, then initializes mods in dependency order
    //When modsFolder is null it uses mods under the program root
    public static ModBootstrapResult Run(ModEnvironment environment, string? modsFolder = null)
    {
        var folder = modsFolder ?? Path.Combine(AppPaths.BaseDirectory, "mods");
        var scanned = ModScanner.ScanAll(folder)
            .Where(m => m.Manifest.Environment.Matches(environment))
            .ToList();

        //Mods with a version mismatch must be dropped here; their injection rules likewise must not enter the rule table
        scanned = ApplyDependencyVersions(scanned);

        //Embedded dependency resolution must precede assembly; assembly resolves replacement classes, and at that moment the dependencies they reference must be available
        ModLibs.Register(scanned);

        var kernelNames = CollectKernelAssemblies();
        var hooks = ModHooks.Build(scanned, kernelNames, environment);

        //ReJIT can only be switched on at process start, and that restart belongs to the native layer's own guard: by the
        //time this runs the profiler is either attached or it never will be, so nothing is started from here
        //A runtime rule that cannot be committed is reported as a warning by ApplyRuntimeInjects below

        if (hooks.TargetAssemblies.Count > 0 || hooks.RuntimeTargets.Count > 0)
        {
            //Mod assemblies also pass through the rewriter; mod-into-mod injection happens at the moment they are loaded
            ModAssemblies.Rewriter = hooks.Rewrite;
            EmbeddedAssemblyLoader.SetRewriter(hooks.Rewrite);
            //Replacements come up first so the injected one can be rewritten when its turn comes
            hooks.PreloadReplacers();
            PreloadTargets(hooks, kernelNames);
            //RuntimePatch lands only after all target assemblies are loaded; placed before mod Init for a deterministic timing
            hooks.ApplyRuntimePatches();
            //Runtime injection also requires the target to be in the process, so it runs after kernel preloading
            hooks.ApplyRuntimeInjects();
        }

        //Problems from every phase are collected as they are reported, so the window at the end and the log say the same
        //thing; a rule that never landed is exactly what the developer needs in front of them
        var problems = new List<string>();

        //Problems accumulated during assembly and preloading are reported together here
        foreach (var error in hooks.Errors)
        {
            var line = $"Mod injection rule error {error}";
            problems.Add(line);
            Log.Warning(line);
        }

        //A collision at an injection point is not an error, just the later rule silently not applying, which no one would know about if not reported
        foreach (var warning in hooks.Warnings)
        {
            var line = $"Mod injection conflict {warning}";
            problems.Add(line);
            Log.Warning(line);
        }

        if (hooks.TargetAssemblies.Count == 0 && hooks.RuntimeTargets.Count == 0)
        {
            Log.Info($"Mod scan finished: {scanned.Count} found, no injection rules");
        }
        else
        {
            Log.Info($"Mod scan finished: {scanned.Count} found, injection targets {string.Join(",", hooks.TargetAssemblies)}" +
                     $" runtime {string.Join(",", hooks.RuntimeTargets)}");
        }

        //Initialization must come after preloading; if a mod touches a kernel type in Init it triggers resolution
        //At that point the target assembly is already the rewritten version; the reverse would pull in the unrewritten one
        var manager = new ModManager();
        //The error event has no other subscriber, so a fault that only reaches it would be lost altogether; it is taken
        //here and lands in the log and the window along with everything else
        manager.OnError += (message, exception) =>
        {
            var line = $"Mod load error {message}: {exception.Message}";
            problems.Add(line);
            Log.Warning(line);
        };
        manager.Init(folder);
        var load = manager.LoadAllModsAsync(environment).GetAwaiter().GetResult();

        foreach (var error in load.Errors)
        {
            var line = $"Mod init error {error}";
            problems.Add(line);
            Log.Warning(line);
        }

        Log.Info($"Mod init finished: {load.Loaded.Count} loaded, {load.Skipped.Count} skipped");

        //R1: per-rule results, printed once rewriting has run for both kernel and mod assemblies
        //A rule that never landed gets a line each so it can be pointed at; the ones that landed are only counted, since a
        //large mod set would otherwise flood the log with one line per rule on every start
        var landed = 0;
        foreach (var outcome in hooks.Outcomes)
        {
            if (outcome.IsApplied)
            {
                landed++;
                continue;
            }

            var reason = $"[{outcome.SourceMod ?? "unknown"}] {outcome.Rule} reason={outcome.Kind} matches={outcome.Matches}";
            Log.Warning($"Mod injection not applied {reason}");
            if (outcome.Detail is { Length: > 0 } detail)
                Log.Debug($"Mod injection detail {outcome.SourceMod ?? "unknown"}: {detail}");
            //Under debug a rule that never landed joins the window alongside the load failures, which is where a mod author
            //is already looking. A normal launch only interrupts for a failed load: one stale rule would otherwise put a
            //dialog in front of every single start
            if (DebugModeRequested())
                problems.Add($"Mod injection not applied {reason}");
        }
        Log.Info($"Mod injection outcomes: {landed} of {hooks.Rules.Count} rules landed");

        //Registers with the host components; UI code that is not in the loading flow reads data from ModHost
        ModHost.Bind(manager, folder);

        //The window comes up once loading is done, right before the kernel starts, and never blocks: startup carries on
        //behind it. A failed load writes no report, so the lines are handed over directly
        if (problems.Count > 0 && WindowAllowed())
        {
            //Mod loading finishes before the kernel installs its language table, so the table the window needs is installed here
            NcLanguageFiles.Extract();
            NcLanguage.Load(Language.Default);
            NativeErrorWindow.ShowModLoadFailure(string.Join(Environment.NewLine, problems), problems.Count);
        }

        return new ModBootstrapResult { Hooks = hooks, Manager = manager, Load = load };
    }

    //DebugModeRequested reports the --debug flag, the same spelling the launcher declares as a kernel flag
    //Read off the command line because the flag is not carried down into this layer
    private static bool DebugModeRequested()
    {
        var args = Environment.GetCommandLineArgs();
        return Array.IndexOf(args, "--debug") >= 0;
    }

    //WindowAllowed reports whether there is a desktop to draw on; the same two spellings the server accepts
    //The flag is read off the command line because mod loading finishes long before ServerMain gets to decide it
    private static bool WindowAllowed()
    {
        var args = Environment.GetCommandLineArgs();
        return Array.IndexOf(args, "--nogui") < 0 && Array.IndexOf(args, "nogui") < 0;
    }

    //ApplyDependencyVersions: drops incompatible mods by the dependency versions declared in the manifest
    //Must come before rule assembly; a dropped mod that still had rules would rewrite the kernel and other mods anyway
    private static List<ScannedMod> ApplyDependencyVersions(List<ScannedMod> scanned)
    {
        var skipped = new HashSet<string>(StringComparer.Ordinal);
        var errors = new List<string>();
        ModDependencies.Filter(scanned.Select(m => m.Manifest).ToList(), skipped, errors);
        if (skipped.Count == 0)
            return scanned;

        foreach (var error in errors)
            Log.Warning($"Mod dependency unmet {error}");
        return scanned.Where(m => !skipped.Contains(m.Manifest.Id)).ToList();
    }

    //CollectKernelAssemblies: the injectable kernel assembly names
    //Embedded sub-libraries come from the main library's embedded resources; the upper assemblies Game/Client/Server/Gpu are moved into the kernel subdirectory after build
    //Both sources must be collected, otherwise rules targeting them are silently dropped
    //The main library and the loader itself are excluded; regular mods may not rewrite the kernel body or the loading flow
    public static List<string> CollectKernelAssemblies()
    {
        var names = new List<string>(EmbeddedAssemblyLoader.ListEmbeddedAssemblies());
        var kernelDirectory = Path.Combine(AppPaths.BaseDirectory, EmbeddedAssemblyLoader.KernelDirectoryName);
        if (!Directory.Exists(kernelDirectory))
            return names;

        foreach (var path in Directory.EnumerateFiles(kernelDirectory, "NacreCraft.*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (name is "NacreCraft" or "NacreCraft.Loader" or "NacreCraft.ModLoader")
                continue;
            if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                names.Add(name);
        }
        return names;
    }

    //PreloadTargets: loads the rewritten kernel target assemblies into Default ahead of time
    //The resolution callback can rewrite too, but it fires on demand; preloading does it in one step without depending on timing
    //Mod targets do not go through here; they are rewritten by ModAssemblies.Load at the moment of loading
    //An assembly already in Default cannot be rewritten, so the only option is to warn that this injection missed its window
    private static void PreloadTargets(ModHooks hooks, List<string> kernelNames)
    {
        var kernel = new HashSet<string>(kernelNames, StringComparer.Ordinal);
        foreach (var name in hooks.TargetAssemblies)
        {
            if (!kernel.Contains(name))
                continue;

            if (AssemblyLoadContext.Default.Assemblies.Any(a => a.GetName().Name == name))
            {
                Log.Warning($"Assembly {name} was already loaded early, its injection rules are too late");
                continue;
            }

            var bytes = EmbeddedAssemblyLoader.ReadAssemblyBytes(name);
            if (bytes is null)
            {
                Log.Warning($"Assembly {name} is neither embedded nor in the kernel directory, injection rules have nowhere to apply");
                continue;
            }

            bytes = hooks.Rewrite(name, bytes);
            AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(bytes));
            Log.Info($"Rewrote and preloaded assembly {name}");
        }
    }

    //ReadKernelAssembly: gets the raw bytes of a kernel assembly
    //Embedded resources first with the kernel directory as fallback; returns null when neither has it
    public static byte[]? ReadKernelAssembly(string assemblyName)
        => EmbeddedAssemblyLoader.ReadAssemblyBytes(assemblyName);
}

//ModBootstrapResult: the bootstrap output
//Hooks: the injection assembly result, for querying target assemblies and rule errors
//Manager: the mod manager, used later to query mod status and to call ShutdownAsync on exit
//Load: the statistics of this load
public sealed class ModBootstrapResult
{
    public required ModHooks Hooks { get; init; }
    public required ModManager Manager { get; init; }
    public required LoadResult Load { get; init; }
}
