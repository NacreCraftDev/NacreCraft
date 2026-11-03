using NacreCraft.Registry;

namespace NacreCraft.Resources;

//ResourceManager the resource pack aggregation the game looks resources up through
//Maps to vanilla net.minecraft.server.packs.resources.MultiPackResourceManager, kept pack-type agnostic
//NC serves both assets/ and data/ from one instance, so every lookup names its pack type instead of binding a manager to one
public sealed class ResourceManager
{
    //packs ordered lowest priority first
    private List<Pack> _packs = new();
    //one vanilla-shaped manager per pack type, rebuilt whenever the pack selection changes
    private readonly Dictionary<PackType, MultiPackResourceManager> _managers = new();
    //_listingCache memoizes directory listings per type+namespace+prefix
    //Listeners ask for overlapping prefixes during one reload and every miss walks the pack sources again
    private readonly Dictionary<(PackType Type, string Namespace, string Prefix), IReadOnlyList<Identifier>> _listingCache = new();

    public IReadOnlyList<Pack> Packs => _packs;

    //SetPacks replaces the whole pack list, maps to the fresh MultiPackResourceManager vanilla builds on every reload
    //The caller passes the repository selection, which is already ordered lowest priority first
    public void SetPacks(IReadOnlyList<Pack> packs)
    {
        _packs = new List<Pack>(packs);
        _listingCache.Clear();
        var resources = _packs.Select(p => p.Resources).ToList();
        foreach (var type in Enum.GetValues<PackType>())
        {
            _managers[type] = new MultiPackResourceManager(type, resources);
        }
    }

    //GetResource fetches the resource for type+location, the last pack holding it wins
    public Resource? GetResource(PackType type, Identifier location)
        => _managers.TryGetValue(type, out var manager) ? manager.GetResource(location) : null;

    //GetResourceStack collects every pack's copy of one resource, lowest priority first
    public IReadOnlyList<Resource> GetResourceStack(PackType type, Identifier location)
        => _managers.TryGetValue(type, out var manager) ? manager.GetResourceStack(location) : Array.Empty<Resource>();

    //ListResources lists resources matching namespace + pathPrefix across all packs
    public IEnumerable<Resource> ListResources(PackType type, string namespaceName, string pathPrefix)
    {
        foreach (var id in ListResourceIds(type, namespaceName, pathPrefix))
        {
            var resource = GetResource(type, id);
            if (resource != null) yield return resource;
        }
    }

    //ListResourceIds collects the matching identifiers, memoized per type+namespace+prefix
    //The result is sorted by identifier, matching vanilla FallbackResourceManager which collects into a TreeMap
    //Sidecar .mcmeta files are not part of the listing, they attach to their resource through Resource.Metadata
    public IReadOnlyList<Identifier> ListResourceIds(PackType type, string namespaceName, string pathPrefix)
    {
        var key = (type, namespaceName, pathPrefix);
        if (_listingCache.TryGetValue(key, out var cached)) return cached;
        List<Identifier> ids = new();
        if (_managers.TryGetValue(type, out var manager))
        {
            ids.AddRange(manager.ListResourcesForNamespace(namespaceName, pathPrefix, _ => true).Keys);
        }
        _listingCache[key] = ids;
        return ids;
    }

    //GetNamespaces returns the union of namespaces across all packs
    public ISet<string> GetNamespaces(PackType type)
        => _managers.TryGetValue(type, out var manager) ? manager.GetNamespaces() : new HashSet<string>();
}
