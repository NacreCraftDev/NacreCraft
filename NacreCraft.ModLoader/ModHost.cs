namespace NacreCraft.ModLoader;

//ModHost: a read-only access point for the loader's host components
//Maps to vanilla FabricLoader.getInstance(); UI code that does not take part in the loading flow reads mod data from here
//Bootstrap is one-shot and never changes after binding, so no locking is needed here
public static class ModHost
{
    //RuntimeModId: the id of the easter-egg entry; it is not in the mods directory so this id identifies it
    private const string RuntimeModId = "dotnet.runtime";
    //RuntimeIconResource: the embedded resource name of the easter-egg icon
    private const string RuntimeIconResource = "logo_net.png";

    //RuntimeMod: the .NET runtime showing up through the mod page
    //It is not a mod and takes no part in loading, so its status is marked as loaded and injection rules are left empty
    private static readonly ModInfo RuntimeMod = new()
    {
        Name = RuntimeModId,
        DisplayName = ".NET Runtime",
        Version = System.Environment.Version.ToString(3),
        Description = ".NET is a cross-platform runtime for cloud, mobile, desktop, and IoT apps.",
        License = "MIT",
        Contact = new ModContact { Homepage = "https://github.com/dotnet/runtime" },
        Environment = ModEnvironment.Both,
        Status = ModStatus.Running,
        //The initialization step is meaningless for the runtime, a negative value keeps the whole row from showing in the UI
        LoadMilliseconds = -1,
    };

    private static readonly Lazy<byte[]?> RuntimeIconLazy = new(ReadRuntimeIcon);

    //Manager: the bound mod manager, null before bootstrap
    public static ModManager? Manager { get; private set; }

    //ModsFolder: the mods directory, an empty string when unbound
    public static string ModsFolder { get; private set; } = string.Empty;

    //Environment: the side this process bootstrapped as
    public static ModEnvironment Environment { get; private set; } = ModEnvironment.Both;

    //IsDevelopmentEnvironment: whether the launch carried --debug, the same spelling the launcher declares
    public static bool IsDevelopmentEnvironment { get; private set; }

    //Bind: registers right after the dependency solve, before any mod entrypoint runs,
    //so NcLoader answers during preLaunch and main instead of only after the whole load
    public static void Bind(ModManager manager, string modsFolder, ModEnvironment environment, bool development)
    {
        Manager = manager;
        ModsFolder = modsFolder;
        Environment = environment;
        IsDevelopmentEnvironment = development;
    }

    //Containers: the mod-facing read-only container views; the fake runtime entry is host UI sugar and stays out of here
    public static IReadOnlyList<NacreCraft.ModLoader.Api.IModContainer> Containers
        => Manager?.GetContainers() ?? Array.Empty<NacreCraft.ModLoader.Api.IModContainer>();

    //FindContainer: one mod-facing container view, null when the id is not tracked
    public static NacreCraft.ModLoader.Api.IModContainer? FindContainer(string name) => Manager?.GetContainer(name);

    //LoadedMods: mods that finished loading
    public static IReadOnlyList<ModInfo> LoadedMods => Merge(Manager?.GetLoadedMods());

    //AllMods: all scanned mods, including failed and skipped ones
    public static IReadOnlyList<ModInfo> AllMods => Merge(Manager?.GetAllMods());

    //Find: looks up one mod by its identifier, returns null if not found
    public static ModInfo? Find(string name)
        => name == RuntimeModId ? RuntimeMod : Manager?.GetModInfo(name);

    //ReadIcon: gets the mod icon bytes, returns null when there is no icon or when unbound
    public static byte[]? ReadIcon(string name)
        => name == RuntimeModId ? RuntimeIconLazy.Value : Manager?.ReadIcon(name);

    //Merge: appends the easter-egg entry after the real mods
    //It does not live in the mods directory or take part in loading, and is placed last so it does not look like a member of the mod list
    private static IReadOnlyList<ModInfo> Merge(IReadOnlyList<ModInfo>? mods)
    {
        var merged = new List<ModInfo>((mods?.Count ?? 0) + 1);
        if (mods is not null)
            merged.AddRange(mods);
        merged.Add(RuntimeMod);
        return merged;
    }

    //ReadRuntimeIcon: reads the easter-egg icon from the loader's own embedded resources
    private static byte[]? ReadRuntimeIcon()
    {
        try
        {
            using var stream = typeof(ModHost).Assembly.GetManifestResourceStream(RuntimeIconResource);
            if (stream is null)
                return null;

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
