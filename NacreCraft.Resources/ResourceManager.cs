using NacreCraft.Registry;

namespace NacreCraft.Resources;

//ResourceManager resource pack aggregation and lookup
//maps to vanilla net.minecraft.server.packs.resources.MultiPackResourceManager
//Packs are held lowest priority first; a lookup walks them backwards so the last pack wins a conflict
public sealed class ResourceManager
{
    //packs ordered lowest priority first
    private List<Pack> _packs = new();
    //cache of pack content accessors grouped by type
    private readonly Dictionary<PackType, List<PackResources>> _byType = new();
    //_listingCache memoizes directory listings per type+namespace+prefix
    //Listeners ask for overlapping prefixes during one reload and every miss walks the pack sources again
    private readonly Dictionary<(PackType Type, string Namespace, string Prefix), List<Identifier>> _listingCache = new();

    public IReadOnlyList<Pack> Packs => _packs;

    //SetPacks replaces the whole pack list, maps to the fresh MultiPackResourceManager vanilla builds on every reload
    //The caller passes the repository selection, which is already ordered lowest priority first
    public void SetPacks(IReadOnlyList<Pack> packs)
    {
        _packs = new List<Pack>(packs);
        _listingCache.Clear();
        RebuildCache();
    }

    //GetResource fetches the resource for type+location, the last pack holding it wins
    public Resource? GetResource(PackType type, Identifier location)
    {
        if (!_byType.TryGetValue(type, out var list)) return null;
        //Vanilla FallbackResourceManager walks its fallbacks backwards, so the most recently pushed pack overrides the earlier ones
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var pack = list[i];
            //the probe stream must be released immediately, otherwise folder packs keep the file handle open
            using (var probe = pack.GetResource(type, location))
            {
                if (probe != null)
                {
                    return new Resource(location, pack.PackId, () => pack.GetResource(type, location) ?? new MemoryStream());
                }
            }
        }
        return null;
    }

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
    //The result is sorted, matching vanilla FallbackResourceManager which collects into a TreeMap
    public IReadOnlyList<Identifier> ListResourceIds(PackType type, string namespaceName, string pathPrefix)
    {
        var key = (type, namespaceName, pathPrefix);
        if (_listingCache.TryGetValue(key, out var cached)) return cached;
        var ids = new List<Identifier>();
        if (_byType.TryGetValue(type, out var list))
        {
            var seen = new HashSet<Identifier>();
            foreach (var pack in list)
            {
                pack.ListResources(type, namespaceName, pathPrefix, seen);
            }
            ids.AddRange(seen);
            ids.Sort();
        }
        _listingCache[key] = ids;
        return ids;
    }

    //GetNamespaces returns the union of namespaces across all packs
    public ISet<string> GetNamespaces(PackType type)
    {
        var result = new HashSet<string>();
        if (_byType.TryGetValue(type, out var list))
        {
            foreach (var pack in list)
            {
                result.UnionWith(pack.GetNamespaces(type));
            }
        }
        return result;
    }

    //RebuildCache rebuilds the per-type accessor cache
    private void RebuildCache()
    {
        _byType.Clear();
        foreach (var type in Enum.GetValues<PackType>())
        {
            _byType[type] = _packs.Select(p => p.Resources).ToList();
        }
    }
}
