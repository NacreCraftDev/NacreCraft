using NacreCraft.Logging;
using NacreCraft.Registry;

namespace NacreCraft.Resources;

//FallbackResourceManager the pack stack of one namespace, maps to vanilla net.minecraft.server.packs.resources.FallbackResourceManager
//Packs are pushed lowest priority first, so a lookup walks the stack backwards and the last pack wins a conflict
public sealed class FallbackResourceManager : ResourceProvider
{
    private readonly List<PackEntry> _fallbacks = new();
    private readonly PackType _type;
    private readonly string _namespace;

    public FallbackResourceManager(PackType type, string namespaceName)
    {
        _type = type;
        _namespace = namespaceName;
    }

    public void Push(PackResources pack) => _fallbacks.Add(new PackEntry(pack.PackId, pack, null));

    public void Push(PackResources pack, Predicate<Identifier> filter) => _fallbacks.Add(new PackEntry(pack.PackId, pack, filter));

    //PushFilterOnly records a filter without contents, used when a pack filters a namespace it does not provide
    public void PushFilterOnly(string name, Predicate<Identifier> filter) => _fallbacks.Add(new PackEntry(name, null, filter));

    public ISet<string> GetNamespaces() => new HashSet<string> { _namespace };

    public Resource? GetResource(Identifier location)
    {
        for (var i = _fallbacks.Count - 1; i >= 0; i--)
        {
            var entry = _fallbacks[i];
            var pack = entry.Resources;
            if (pack is not null)
            {
                //The probe stream must be released right away, otherwise folder packs keep the file handle open
                using (var probe = pack.GetResource(_type, location))
                {
                    if (probe is not null)
                    {
                        return new Resource(pack, location,
                            new IoSupplier<Stream>(() => pack.GetResource(_type, location)!),
                            CreateStackMetadataFinder(location, i));
                    }
                }
            }
            if (!entry.IsFiltered(location)) continue;
            Log.Warning($"Resource {location} not found, but was filtered by pack {entry.Name}");
            return null;
        }
        return null;
    }

    //GetResourceStack collects every pack's copy of one resource, maps to vanilla getResourceStack
    //The result is ordered lowest priority first, the last entry is the winner of a conflict
    public IReadOnlyList<Resource> GetResourceStack(Identifier location)
    {
        var metadataLocation = GetMetadataLocation(location);
        var result = new List<Resource>();
        var filterMeta = false;
        string? lastFilterName = null;
        for (var i = _fallbacks.Count - 1; i >= 0; i--)
        {
            var entry = _fallbacks[i];
            var pack = entry.Resources;
            if (pack is not null)
            {
                using var probe = pack.GetResource(_type, location);
                if (probe is not null)
                {
                    var source = pack;
                    IoSupplier<ResourceMetadata> metadataGetter = filterMeta
                        ? ResourceMetadata.EmptySupplier
                        : new IoSupplier<ResourceMetadata>(() =>
                        {
                            using var metadataStream = source.GetResource(_type, metadataLocation);
                            return metadataStream is not null
                                ? ResourceMetadata.FromJsonStream(metadataStream)
                                : ResourceMetadata.Empty;
                        });
                    result.Add(new Resource(pack, location, new IoSupplier<Stream>(() => source.GetResource(_type, location)!),
                        metadataGetter));
                }
            }
            if (entry.IsFiltered(location))
            {
                lastFilterName = entry.Name;
                break;
            }
            if (!entry.IsFiltered(metadataLocation)) continue;
            filterMeta = true;
        }
        if (result.Count == 0 && lastFilterName is not null)
            Log.Warning($"Resource {location} not found, but was filtered by pack {lastFilterName}");
        result.Reverse();
        return result;
    }

    //ListResources lists the winning copy of every resource under a directory, maps to vanilla listResources
    //Resource metadata comes from the same pack as the resource, or a higher pack when it overrides only the sidecar
    public SortedDictionary<Identifier, Resource> ListResources(string directory, Predicate<Identifier> filter)
    {
        var fileLocations = new Dictionary<Identifier, ResourceWithSource>();
        var metaLocations = new Dictionary<Identifier, ResourceWithSource>();
        for (var i = 0; i < _fallbacks.Count; i++)
        {
            var entry = _fallbacks[i];
            entry.FilterAll(fileLocations.Keys);
            entry.FilterAll(metaLocations.Keys);
            var pack = entry.Resources;
            if (pack is null) continue;
            var packIndex = i;
            pack.ListResources(_type, _namespace, directory, (id, streamSupplier) =>
            {
                if (IsMetadata(id))
                {
                    if (filter(GetIdentifierFromMetadata(id))) metaLocations[id] = new ResourceWithSource(pack, streamSupplier, packIndex);
                }
                else if (filter(id))
                {
                    fileLocations[id] = new ResourceWithSource(pack, streamSupplier, packIndex);
                }
            });
        }
        var result = new SortedDictionary<Identifier, Resource>();
        foreach (var (location, resource) in fileLocations)
        {
            var metadataLocation = GetMetadataLocation(location);
            var metadataGetter = metaLocations.TryGetValue(metadataLocation, out var meta) && meta.PackIndex >= resource.PackIndex
                ? ConvertToMetadata(meta.Stream)
                : ResourceMetadata.EmptySupplier;
            result[location] = new Resource(resource.Source, location, resource.Stream, metadataGetter);
        }
        return result;
    }

    //ListResourceStacks lists every pack's copy of each resource under a directory, maps to vanilla listResourceStacks
    public SortedDictionary<Identifier, IReadOnlyList<Resource>> ListResourceStacks(string directory, Predicate<Identifier> filter)
    {
        var foundResources = new Dictionary<Identifier, EntryStack>();
        foreach (var entry in _fallbacks)
        {
            ApplyPackFiltersToExistingResources(entry, foundResources);
            ListPackResources(entry, directory, filter, foundResources);
        }
        var result = new SortedDictionary<Identifier, IReadOnlyList<Resource>>();
        foreach (var stack in foundResources.Values)
        {
            if (stack.FileSources.Count == 0) continue;
            var resources = new List<Resource>(stack.FileSources.Count);
            foreach (var source in stack.FileSources)
            {
                var metadataGetter = stack.MetaSources.TryGetValue(source.Source, out var meta)
                    ? ConvertToMetadata(meta)
                    : ResourceMetadata.EmptySupplier;
                resources.Add(new Resource(source.Source, stack.FileLocation, source.Stream, metadataGetter));
            }
            result[stack.FileLocation] = resources;
        }
        return result;
    }

    public IEnumerable<PackResources> ListPacks()
        => _fallbacks.Select(e => e.Resources).OfType<PackResources>();

    //CreateStackMetadataFinder resolves the sidecar as seen from one pack downwards, maps to vanilla createStackMetadataFinder
    private IoSupplier<ResourceMetadata> CreateStackMetadataFinder(Identifier location, int finalPackIndex)
    {
        return new IoSupplier<ResourceMetadata>(() =>
        {
            var metadataLocation = GetMetadataLocation(location);
            for (var i = _fallbacks.Count - 1; i >= finalPackIndex; i--)
            {
                var entry = _fallbacks[i];
                var pack = entry.Resources;
                if (pack is not null)
                {
                    using var stream = pack.GetResource(_type, metadataLocation);
                    if (stream is not null) return ResourceMetadata.FromJsonStream(stream);
                }
                if (entry.IsFiltered(metadataLocation)) break;
            }
            return ResourceMetadata.Empty;
        });
    }

    private static IoSupplier<ResourceMetadata> ConvertToMetadata(IoSupplier<Stream> input)
        => new(() =>
        {
            using var stream = input.Get();
            return ResourceMetadata.FromJsonStream(stream);
        });

    private static bool IsMetadata(Identifier location) => location.Path.EndsWith(".mcmeta", StringComparison.Ordinal);

    private static Identifier GetIdentifierFromMetadata(Identifier location)
        => location.WithPath(location.Path[..^".mcmeta".Length]);

    private static Identifier GetMetadataLocation(Identifier location)
        => location.WithSuffix(".mcmeta");

    //ApplyPackFiltersToExistingResources drops the sources a pack filters out, maps to vanilla applyPackFiltersToExistingResources
    private static void ApplyPackFiltersToExistingResources(PackEntry entry, Dictionary<Identifier, EntryStack> foundResources)
    {
        foreach (var stack in foundResources.Values)
        {
            if (entry.IsFiltered(stack.FileLocation))
            {
                stack.FileSources.Clear();
                continue;
            }
            if (entry.IsFiltered(stack.MetadataLocation)) stack.MetaSources.Clear();
        }
    }

    private void ListPackResources(PackEntry entry, string directory, Predicate<Identifier> filter,
        Dictionary<Identifier, EntryStack> foundResources)
    {
        var pack = entry.Resources;
        if (pack is null) return;
        pack.ListResources(_type, _namespace, directory, (id, streamSupplier) =>
        {
            if (IsMetadata(id))
            {
                var actualId = GetIdentifierFromMetadata(id);
                if (!filter(actualId)) return;
                GetOrAddStack(foundResources, actualId).MetaSources[pack] = streamSupplier;
            }
            else
            {
                if (!filter(id)) return;
                GetOrAddStack(foundResources, id).FileSources.Add(new ResourceWithSource(pack, streamSupplier, 0));
            }
        });
    }

    private static EntryStack GetOrAddStack(Dictionary<Identifier, EntryStack> foundResources, Identifier fileLocation)
    {
        if (foundResources.TryGetValue(fileLocation, out var existing)) return existing;
        var stack = new EntryStack(fileLocation);
        foundResources[fileLocation] = stack;
        return stack;
    }

    //PackEntry one entry of the fallback stack, a pack plus the paths it refuses to serve
    private sealed record PackEntry(string Name, PackResources? Resources, Predicate<Identifier>? Filter)
    {
        public void FilterAll(ICollection<Identifier> locations)
        {
            if (Filter is null) return;
            foreach (var location in locations.Where(l => Filter(l)).ToList()) locations.Remove(location);
        }

        public bool IsFiltered(Identifier location) => Filter is not null && Filter(location);
    }

    //EntryStack the copies of one resource across the stack plus their sidecars
    private sealed class EntryStack
    {
        public Identifier FileLocation { get; }
        public Identifier MetadataLocation { get; }
        public List<ResourceWithSource> FileSources { get; } = new();
        public Dictionary<PackResources, IoSupplier<Stream>> MetaSources { get; } = new();

        public EntryStack(Identifier fileLocation)
        {
            FileLocation = fileLocation;
            MetadataLocation = GetMetadataLocation(fileLocation);
        }
    }

    //ResourceWithSource one resource together with the pack it came from
    private sealed record ResourceWithSource(PackResources Source, IoSupplier<Stream> Stream, int PackIndex);
}
