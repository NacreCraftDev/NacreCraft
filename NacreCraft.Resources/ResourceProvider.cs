using NacreCraft.Registry;

namespace NacreCraft.Resources;

//ResourceProvider a single-namespace resource lookup, maps to vanilla net.minecraft.server.packs.resources.ResourceProvider
//Vanilla's provider is bound to one pack type; NC does the same, the caller picks the type when building the provider
public interface ResourceProvider
{
    //Empty a provider that never finds anything, maps to vanilla ResourceProvider.EMPTY
    static ResourceProvider Empty => EmptyResourceProvider.Instance;

    //GetResource looks up one resource, null when no pack provides it
    Resource? GetResource(Identifier location);

    //GetResourceOrThrow throws instead of returning null, maps to vanilla getResourceOrThrow
    Resource GetResourceOrThrow(Identifier location)
        => GetResource(location) ?? throw new FileNotFoundException(location.ToString());

    //Open opens the resource stream, maps to vanilla ResourceProvider.open
    Stream Open(Identifier location) => GetResourceOrThrow(location).Open();

    //OpenAsReader opens the resource as text, maps to vanilla ResourceProvider.openAsReader
    StreamReader OpenAsReader(Identifier location) => GetResourceOrThrow(location).OpenAsReader();

    //FromMap serves the given resources and nothing else, maps to vanilla ResourceProvider.fromMap
    static ResourceProvider FromMap(IReadOnlyDictionary<Identifier, Resource> map) => new MapResourceProvider(map);
}

//EmptyResourceProvider the empty provider singleton
internal sealed class EmptyResourceProvider : ResourceProvider
{
    public static readonly EmptyResourceProvider Instance = new();

    public Resource? GetResource(Identifier location) => null;
}

//MapResourceProvider a provider backed by an existing map
internal sealed class MapResourceProvider : ResourceProvider
{
    private readonly IReadOnlyDictionary<Identifier, Resource> _map;

    public MapResourceProvider(IReadOnlyDictionary<Identifier, Resource> map) => _map = map;

    public Resource? GetResource(Identifier location) => _map.GetValueOrDefault(location);
}
