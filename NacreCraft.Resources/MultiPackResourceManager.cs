using NacreCraft.Logging;
using NacreCraft.Registry;

namespace NacreCraft.Resources;

//MultiPackResourceManager the whole pack set for one pack type, maps to vanilla net.minecraft.server.packs.resources.MultiPackResourceManager
//Splits the packs into one FallbackResourceManager per namespace, so a lookup only walks packs that can serve that namespace
public sealed class MultiPackResourceManager : CloseableResourceManager
{
    private readonly Dictionary<string, FallbackResourceManager> _namespacedManagers = new();
    private readonly IReadOnlyList<PackResources> _packs;

    public MultiPackResourceManager(PackType type, IReadOnlyList<PackResources> packs)
    {
        _packs = packs.ToList();
        var namespaces = new List<string>();
        foreach (var pack in _packs)
            foreach (var namespaceName in pack.GetNamespaces(type))
                if (!namespaces.Contains(namespaceName)) namespaces.Add(namespaceName);

        foreach (var pack in _packs)
        {
            var filterSection = GetPackFilterSection(pack);
            var providedNamespaces = pack.GetNamespaces(type);
            Predicate<Identifier>? pathFilter = filterSection is null
                ? null
                : location => filterSection.IsPathFiltered(location.Path);
            foreach (var namespaceName in namespaces)
            {
                var packContainsNamespace = providedNamespaces.Contains(namespaceName);
                var filterMatchesNamespace = filterSection is not null && filterSection.IsNamespaceFiltered(namespaceName);
                if (!packContainsNamespace && !filterMatchesNamespace) continue;
                if (!_namespacedManagers.TryGetValue(namespaceName, out var manager))
                {
                    manager = new FallbackResourceManager(type, namespaceName);
                    _namespacedManagers[namespaceName] = manager;
                }
                //Only a pack that both provides and filters a namespace carries the path filter, matching vanilla
                if (packContainsNamespace && filterMatchesNamespace)
                {
                    manager.Push(pack, pathFilter!);
                }
                else if (packContainsNamespace)
                {
                    manager.Push(pack);
                }
                else
                {
                    manager.PushFilterOnly(pack.PackId, pathFilter!);
                }
            }
        }
    }

    private static ResourceFilterSection? GetPackFilterSection(PackResources pack)
    {
        try
        {
            return pack.GetMetadataSection(ResourceFilterSection.Type);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to get filter section from pack {pack.PackId}: {ex.Message}");
            return null;
        }
    }

    public ISet<string> GetNamespaces() => _namespacedManagers.Keys.ToHashSet();

    public Resource? GetResource(Identifier location)
        => _namespacedManagers.TryGetValue(location.Namespace, out var manager) ? manager.GetResource(location) : null;

    public IReadOnlyList<Resource> GetResourceStack(Identifier location)
        => _namespacedManagers.TryGetValue(location.Namespace, out var manager)
            ? manager.GetResourceStack(location)
            : Array.Empty<Resource>();

    public SortedDictionary<Identifier, Resource> ListResources(string directory, Predicate<Identifier> filter)
    {
        CheckTrailingDirectoryPath(directory);
        var result = new SortedDictionary<Identifier, Resource>();
        foreach (var manager in _namespacedManagers.Values)
            foreach (var (location, resource) in manager.ListResources(directory, filter))
                result[location] = resource;
        return result;
    }

    public SortedDictionary<Identifier, IReadOnlyList<Resource>> ListResourceStacks(string directory, Predicate<Identifier> filter)
    {
        CheckTrailingDirectoryPath(directory);
        var result = new SortedDictionary<Identifier, IReadOnlyList<Resource>>();
        foreach (var manager in _namespacedManagers.Values)
            foreach (var (location, resources) in manager.ListResourceStacks(directory, filter))
                result[location] = resources;
        return result;
    }

    //ListResourcesForNamespace lists one namespace only, used by NC's per-namespace facade lookup
    public SortedDictionary<Identifier, Resource> ListResourcesForNamespace(string namespaceName, string directory,
        Predicate<Identifier> filter)
    {
        CheckTrailingDirectoryPath(directory);
        return _namespacedManagers.TryGetValue(namespaceName, out var manager)
            ? manager.ListResources(directory, filter)
            : new SortedDictionary<Identifier, Resource>();
    }

    public IReadOnlyList<PackResources> ListPacks() => _packs;

    //Close releases every pack this manager owns; NC pack instances are shared across reloads, so the facade never calls it
    public void Close()
    {
        foreach (var pack in _packs) pack.Dispose();
    }

    private static void CheckTrailingDirectoryPath(string directory)
    {
        if (directory.EndsWith('/')) throw new ArgumentException($"Trailing slash in path {directory}");
    }
}
