using System.Diagnostics;
using System.Reflection;

namespace NacreCraft.ModLoader;

//ModManager loading flow: prepare every solved mod, then run the loader-owned phases in dependency order
//The scan phase is gone: ModDependencies.Solve already produced the mod set, the order and the dependency graph
public sealed partial class ModManager
{
    //LoadAllModsAsync: prepares every solved mod, then invokes preLaunch for all and main for all
    //No assembly is loaded during the scan; the mod set arrives solved and environment-filtered
    internal async Task<LoadResult> LoadAllModsAsync(SolvedMods solved, ModEnvironment environment)
    {
        if (string.IsNullOrEmpty(_modsFolderPath))
            throw new InvalidOperationException("Init was not called to specify the mods directory");

        var result = new LoadResult();
        result.Skipped.AddRange(solved.EnvSkipped);
        result.Skipped.AddRange(solved.SolverSkipped);

        foreach (var scanned in solved.Mods)
            _mods[scanned.Manifest.Id] = CreateModInfo(scanned);

        foreach (var (name, deps) in solved.Graph)
        {
            _dependencyGraph[name] = deps.ToList();
            _mods[name].Dependencies = deps.ToList();
        }
        _loadOrder.AddRange(solved.Mods.Select(mod => mod.Manifest.Id));

        var ordered = solved.Mods.Select(mod => _mods[mod.Manifest.Id]).ToList();

        //S5: load every mod assembly and resolve its entrypoint bindings before any mod code runs
        //After this step every mod can see every other mod's types, which is what makes cross-mod references safe
        UpdateProgress("Preparing", 0, ordered.Count, string.Empty);
        for (var index = 0; index < ordered.Count; index++)
        {
            var mod = ordered[index];
            UpdateProgress("Preparing", index, ordered.Count, mod.Name);
            if (!PrepareMod(mod, result))
            {
                mod.Status = ModStatus.Error;
                OnModLoadComplete?.Invoke(Context(mod, index, ordered.Count), ModStatus.Error);
            }
        }

        //S6: every mod's preLaunch runs before any mod's main; a mod failing one phase does not stop the others
        await InvokePhaseAsync(ModPhases.PreLaunch, ordered, result);
        await InvokePhaseAsync(ModPhases.Main, ordered, result);

        //Mods that never failed reach Running here; Scanned covers hook-only mods with no
        //loader phases, Initializing covers mods whose phases all ran
        for (var index = 0; index < ordered.Count; index++)
        {
            var mod = ordered[index];
            if (mod.Status is not (ModStatus.Scanned or ModStatus.Initializing))
                continue;
            mod.Status = ModStatus.Running;
            result.Loaded.Add(mod.Name);
            ModLoaded?.Invoke(mod.ToPublic());
            OnModLoadComplete?.Invoke(Context(mod, index, ordered.Count), ModStatus.Running);
        }

        result.Success = result.Errors.Count == 0;
        UpdateProgress("Done", ordered.Count, ordered.Count, result.Success ? "OK" : "PartialFail");
        return result;
    }

    //CreateModInfo: the internal record of one solved mod; the manifest was validated at scan time
    private static InternalModInfo CreateModInfo(ScannedMod scanned) => new()
    {
        Name = scanned.Manifest.Id,
        AssemblyPath = scanned.AssemblyPath,
        AssemblyName = scanned.AssemblyName,
        EmbeddedResources = scanned.EmbeddedResources.ToList(),
        ReferencedAssemblies = scanned.ReferencedAssemblies.ToList(),
        AnnotatedHooks = scanned.AnnotatedHooks.ToList(),
        Manifest = scanned.Manifest,
        Status = ModStatus.Scanned,
    };

    //PrepareMod: loads the assembly and resolves the entrypoint bindings for every phase
    //No mod code runs here; instances are created when their phase is invoked
    private static bool PrepareMod(InternalModInfo mod, LoadResult result)
    {
        if (mod.Prepared)
            return true;

        try
        {
            var assembly = ModAssemblies.Load(mod.AssemblyName, mod.AssemblyPath);

            //The manifest is always set at creation; the null check is a formality the compiler needs
            var manifest = mod.Manifest ?? new ModManifest();
            foreach (var (phase, typeNames) in manifest.Entrypoints)
            {
                var contract = ModPhases.ContractOf(phase);
                if (contract is null)
                {
                    //The scanner only hands over known phases; this line exists so a future phase slip fails loudly here
                    result.Errors.Add($"mod {mod.Name} entrypoint phase {phase} is unknown");
                    return false;
                }

                foreach (var typeName in typeNames)
                {
                    var type = assembly.GetType(typeName, throwOnError: false);
                    if (type is null)
                    {
                        result.Errors.Add($"mod {mod.Name} entrypoint type {typeName} not found");
                        return false;
                    }

                    if (!contract.IsAssignableFrom(type))
                    {
                        result.Errors.Add($"mod {mod.Name} entrypoint {typeName} does not implement {contract.Name}");
                        return false;
                    }

                    if (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) is null)
                    {
                        result.Errors.Add($"mod {mod.Name} entrypoint {typeName} lacks a public parameterless constructor");
                        return false;
                    }

                    mod.Entrypoints.Add(new EntrypointBinding { Phase = phase, Type = type });
                }
            }

            //Exit stays a duck method on main-phase entrypoint types, void or Task, no parameters
            foreach (var binding in mod.Entrypoints)
            {
                if (binding.Phase != ModPhases.Main)
                    continue;
                binding.ExitMethod = binding.Type.GetMethod("Exit", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            }

            //The UI shows the first main entrypoint as the mod's representative type
            mod.EntryType = mod.Entrypoints.FirstOrDefault(binding => binding.Phase == ModPhases.Main)?.Type;
            mod.Prepared = true;
            return true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"mod {mod.Name} load failed {ex.Message}");
            return false;
        }
    }

    //InvokePhaseAsync: runs one phase over the ordered mods; the load control runs once per mod,
    //before the mod's first binding of the first phase that reaches it
    private async Task InvokePhaseAsync(string phase, List<InternalModInfo> ordered, LoadResult result)
    {
        for (var index = 0; index < ordered.Count; index++)
        {
            var mod = ordered[index];
            //Initializing means a previous phase already ran; the mod still gets its remaining phases
            if (mod.Status is not (ModStatus.Scanned or ModStatus.Initializing))
                continue;

            var bindings = mod.Entrypoints.Where(binding => binding.Phase == phase).ToList();
            if (bindings.Count == 0)
                continue;

            UpdateProgress("Initializing", index, ordered.Count, mod.Name);
            mod.Status = ModStatus.Initializing;

            var context = Context(mod, index, ordered.Count);
            if (!mod.LoadControlDone)
            {
                mod.LoadControlDone = true;
                var action = ModLoadAction.Continue;
                if (OnBeforeModLoad is not null)
                {
                    try
                    {
                        action = await OnBeforeModLoad(context);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"pre-load callback of mod {mod.Name} threw", ex);
                        action = ModLoadAction.Skip;
                    }
                }

                if (action == ModLoadAction.Skip)
                {
                    mod.Status = ModStatus.Skipped;
                    result.Skipped.Add(mod.Name);
                    OnModLoadComplete?.Invoke(context, ModStatus.Skipped);
                    continue;
                }

                if (action == ModLoadAction.Interrupt)
                {
                    if (!await HandleInterruptAsync(mod, context, result))
                        continue;
                    //The takeover chose the default flow, so the phase bindings run after all
                    mod.Status = ModStatus.Initializing;
                }
            }

            try
            {
                foreach (var binding in bindings)
                    InvokeBinding(mod, binding);
            }
            catch (Exception ex)
            {
                mod.Status = ModStatus.Error;
                result.Errors.Add($"mod {mod.Name} {phase} failed {ex.Message}");
                OnError?.Invoke($"mod {mod.Name} {phase} failed", ex);
                ModFailed?.Invoke(mod.ToPublic(), ex);
                OnModLoadComplete?.Invoke(context, ModStatus.Error);
            }
        }
    }

    //HandleInterruptAsync: the host takes over loading; the result decides whether to mark failed, mark loaded,
    //stop midway, or fall back to the default flow; true means the caller continues with its bindings
    private async Task<bool> HandleInterruptAsync(InternalModInfo mod, ModLoadContext context, LoadResult result)
    {
        InterruptResult? interruptResult = null;
        if (OnModInterrupted is not null)
        {
            try
            {
                interruptResult = await OnModInterrupted(context);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"intercept callback of mod {mod.Name} threw", ex);
                interruptResult = new InterruptResult { Error = ex.Message };
            }
        }

        if (interruptResult is not null && interruptResult.Error.Length > 0)
        {
            mod.Status = ModStatus.Error;
            result.Errors.Add($"mod {mod.Name} takeover failed {interruptResult.Error}");
            ModFailed?.Invoke(mod.ToPublic(), new InvalidOperationException(interruptResult.Error));
            OnModLoadComplete?.Invoke(context, ModStatus.Error);
            return false;
        }

        if (interruptResult is not null && interruptResult.MarkAsLoaded)
        {
            mod.Status = ModStatus.Running;
            result.Loaded.Add(mod.Name);
            ModLoaded?.Invoke(mod.ToPublic());
            OnModLoadComplete?.Invoke(context, ModStatus.Running);
            return false;
        }

        if (interruptResult is not null && !interruptResult.ContinueOriginalLogic)
        {
            mod.Status = ModStatus.Interrupted;
            result.Interrupted.Add(mod.Name);
            OnModLoadComplete?.Invoke(context, ModStatus.Interrupted);
            return false;
        }

        return true;
    }

    //InvokeBinding: creates the instance on first use, injects host services, calls the phase method
    private void InvokeBinding(InternalModInfo mod, EntrypointBinding binding)
    {
        EnsureStarted(mod, binding);

        var startedAt = Stopwatch.GetTimestamp();
        binding.Method!.Invoke(binding.Instance, null);
        mod.LoadMilliseconds = (mod.LoadMilliseconds ?? 0) + Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
    }

    //EnsureStarted: the entrypoint instance and its resolved phase method, created once per binding
    private void EnsureStarted(InternalModInfo mod, EntrypointBinding binding)
    {
        if (binding.Instance is not null)
            return;

        binding.Instance = Activator.CreateInstance(binding.Type);
        InjectServices(binding.Instance);
        //The UI shows the first main instance as the mod's representative object
        if (binding.Phase == ModPhases.Main && mod.Instance is null)
            mod.Instance = binding.Instance;

        binding.Method = ModPhases.ResolveMethod(binding.Type, binding.Phase);
        if (binding.Method is null)
            throw new MissingMethodException(binding.Type.FullName, ModPhases.MethodNameOf(binding.Phase));
    }

    //InvokeEntrypoints: one externally driven phase (client or server) over the running mods in load order
    //A failure here reports as a runtime error; the mod stays loaded
    internal void InvokeEntrypoints<T>(string phase) where T : class
    {
        foreach (var name in _loadOrder)
        {
            if (!_mods.TryGetValue(name, out var mod) || mod.Status != ModStatus.Running)
                continue;

            foreach (var binding in mod.Entrypoints)
            {
                if (binding.Phase != phase || !typeof(T).IsAssignableFrom(binding.Type))
                    continue;

                try
                {
                    EnsureStarted(mod, binding);
                    binding.Method!.Invoke(binding.Instance, null);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"mod {mod.Name} {phase} entrypoint {binding.Type.Name} failed", ex);
                }
            }
        }
    }

    //Context: the load-control callback context of one mod at one point of the flow
    private ModLoadContext Context(InternalModInfo mod, int completed, int total) => new()
    {
        Name = mod.Name,
        AssemblyPath = mod.AssemblyPath,
        Dependencies = mod.Dependencies,
        Completed = completed,
        Total = total,
    };
}
