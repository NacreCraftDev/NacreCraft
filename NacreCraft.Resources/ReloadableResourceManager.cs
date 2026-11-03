using NacreCraft.Registry;

namespace NacreCraft.Resources;

//ReloadableResourceManager a resource manager that can swap its pack set and rerun its listeners
//maps to vanilla net.minecraft.server.packs.resources.ReloadableResourceManager
//Vanilla schedules the reload on executors; NC keeps the synchronous stepped ReloadInstance, see PreparableReloadListener
public sealed class ReloadableResourceManager : IDisposable
{
    private readonly PackType _type;
    private readonly ResourceManager _resources;
    private readonly List<PreparableReloadListener> _listeners = new();

    public ReloadableResourceManager(PackType type)
    {
        _type = type;
        _resources = new ResourceManager();
    }

    public void RegisterReloadListener(PreparableReloadListener listener) => _listeners.Add(listener);

    //CreateReload swaps in the given packs and starts the listener chain, maps to vanilla createReload
    //The caller pumps the returned instance; vanilla instead hands it to the loading overlay
    public ReloadInstance CreateReload(IReadOnlyList<Pack> resourcePacks)
    {
        _resources.SetPacks(resourcePacks);
        return SimpleReloadInstance.Create(_resources, _listeners);
    }

    public Resource? GetResource(Identifier location) => _resources.GetResource(_type, location);

    public IReadOnlyList<Resource> GetResourceStack(Identifier location) => _resources.GetResourceStack(_type, location);

    //ListResources lists every namespace under a directory, maps to vanilla ResourceManager.listResources
    public SortedDictionary<Identifier, Resource> ListResources(string directory, Predicate<Identifier> filter)
    {
        var result = new SortedDictionary<Identifier, Resource>();
        foreach (var namespaceName in GetNamespaces())
            foreach (var resource in _resources.ListResources(_type, namespaceName, directory))
                if (filter(resource.Location)) result[resource.Location] = resource;
        return result;
    }

    public ISet<string> GetNamespaces() => _resources.GetNamespaces(_type);

    public IReadOnlyList<Pack> Packs => _resources.Packs;

    //The packs belong to the pack repository and outlive any one manager, so this instance has nothing to release
    public void Dispose() { }
}
