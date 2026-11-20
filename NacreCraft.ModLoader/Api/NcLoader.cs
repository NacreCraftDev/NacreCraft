namespace NacreCraft.ModLoader.Api;

//NcLoader: the loader's stable, read-only API surface for mods
//Maps to FabricLoader.getInstance(); the namespace is a compatibility contract: members are append-only,
//signatures do not change, and removals go through a deprecation window
//State is live from the moment the bootstrap binds, which is before any mod entrypoint runs
public static class NcLoader
{
    //Environment: the side this process bootstrapped as
    public static ModEnvironment Environment => ModHost.Environment;

    //ModsFolder: the mods directory the loader scanned
    public static string ModsFolder => ModHost.ModsFolder;

    //LoaderVersion: the version the virtual nc-loader dependency answers with
    public static string LoaderVersion => LoaderVersionProvider.Read();

    //IsDevelopmentEnvironment: whether the launch carried --debug
    public static bool IsDevelopmentEnvironment => ModHost.IsDevelopmentEnvironment;

    //IsModLoaded: whether a mod finished its loader-owned phases and reached Running
    public static bool IsModLoaded(string id) => ModHost.Manager?.IsLoaded(id) ?? false;

    //GetAllMods: every mod in the set, in load order, failed and skipped ones included
    //The fake dotnet.runtime entry of the mods page is host UI sugar and never appears here
    public static IReadOnlyList<IModContainer> GetAllMods() => ModHost.Containers;

    //GetModContainer: one mod by id, null when not tracked
    public static IModContainer? GetModContainer(string id) => ModHost.FindContainer(id);

    //GetLoadOrder: the dependency order the solver produced
    public static IReadOnlyList<string> GetLoadOrder() => ModHost.Manager?.GetLoadOrder() ?? Array.Empty<string>();

    //InvokeEntrypoints: runs the client or server phase bindings of every running mod that declares them,
    //in load order; ModApi calls this from the kernel anchors. The loader owns preLaunch and main and
    //refuses to run them again from here
    public static void InvokeEntrypoints<T>(string phase) where T : class
    {
        if (phase is not (ModPhases.Client or ModPhases.Server))
            throw new ArgumentException($"phase {phase} is not externally invokable; the loader owns preLaunch and main", nameof(phase));

        ModHost.Manager?.InvokeEntrypoints<T>(phase);
    }
}
