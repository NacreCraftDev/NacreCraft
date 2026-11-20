using System.Reflection;

namespace NacreCraft.ModLoader;

//InternalModInfo: the full mod information held internally during the loading flow
internal sealed class InternalModInfo
{
    public string Name { get; set; } = string.Empty;
    public Type? EntryType { get; set; }
    public object? Instance { get; set; }
    public string AssemblyPath { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public ModStatus Status { get; set; }

    //AssemblyName: the mod's assembly name, used to map AssemblyRef back to the mod
    public string AssemblyName { get; set; } = string.Empty;

    //ReferencedAssemblies: all assembly names this assembly references
    public List<string> ReferencedAssemblies { get; set; } = new();

    //Manifest: the parse result of the embedded ncmod.json
    public ModManifest? Manifest { get; set; }

    //AnnotatedHooks: injection rules scanned from Inject annotations, same shape as manifest rules
    public List<ModHookRule> AnnotatedHooks { get; set; } = new();

    //EmbeddedResources: all embedded resource names in the assembly, the icon fallback looks here
    public List<string> EmbeddedResources { get; set; } = new();

    //Entrypoints: the resolved bindings of this mod, one per manifest entrypoint type
    //Instances are created when their phase first runs, so a mod that never reaches a phase never pays for it
    public List<EntrypointBinding> Entrypoints { get; } = new();

    //Prepared: the assembly is loaded and the bindings resolved; no mod code has run yet
    public bool Prepared { get; set; }

    //LoadControlDone: the host's load control ran once for this mod
    public bool LoadControlDone { get; set; }

    //LoadMilliseconds: initialization time in milliseconds, accumulated over the loader-owned phases,
    //used by the UI to show slow-loading mods, null if no phase ran
    public double? LoadMilliseconds { get; set; }

    public ModInfo ToPublic() => new()
    {
        Name = Name,
        DisplayName = Manifest?.EffectiveName ?? Name,
        Version = Manifest?.Version ?? string.Empty,
        Description = Manifest?.Description ?? string.Empty,
        Authors = Manifest?.Authors ?? (IReadOnlyList<string>)Array.Empty<string>(),
        Contributors = Manifest?.Contributors ?? (IReadOnlyList<string>)Array.Empty<string>(),
        License = Manifest?.License ?? string.Empty,
        Contact = Manifest?.Contact ?? new ModContact(),
        Icon = Manifest?.Icon ?? string.Empty,
        Environment = Manifest?.Environment ?? ModEnvironment.Both,
        Hooks = MergedHooks(),
        LoadMilliseconds = LoadMilliseconds,
        EntryType = EntryType,
        Instance = Instance,
        AssemblyPath = AssemblyPath,
        Status = Status,
        Dependencies = Dependencies,
    };

    //MergedHooks: merges annotations and manifest into one list for the UI
    //Annotations come first, matching assembly-time priority; only the first rule per injection point is kept
    private IReadOnlyList<ModHookRule> MergedHooks()
    {
        var manifestHooks = Manifest?.Hooks ?? (IReadOnlyList<ModHookRule>)Array.Empty<ModHookRule>();
        var declared = new HashSet<string>(StringComparer.Ordinal);
        var merged = new List<ModHookRule>();
        foreach (var rule in AnnotatedHooks.Concat(manifestHooks))
        {
            if (declared.Add($"{rule.Target}|{rule.Method}|{rule.HookTypeName}"))
                merged.Add(rule);
        }
        return merged;
    }
}

//EntrypointBinding: one resolved entrypoint of one mod
//The instance and the phase method are created on first use; Exit is an optional duck method on main-phase types
internal sealed class EntrypointBinding
{
    public required string Phase { get; init; }
    public required Type Type { get; init; }
    public object? Instance { get; set; }
    public MethodInfo? Method { get; set; }
    public MethodInfo? ExitMethod { get; set; }
}
