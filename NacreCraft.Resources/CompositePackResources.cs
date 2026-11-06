using NacreCraft.Registry;

namespace NacreCraft.Resources;

//CompositePackResources a primary pack with overlays layered on top, maps to vanilla net.minecraft.server.packs.CompositePackResources
public sealed class CompositePackResources : PackResources
{
    private readonly PackResources _primaryPackResources;
    private readonly IReadOnlyList<PackResources> _packResourcesStack;

    public CompositePackResources(PackResources primaryPackResources, IReadOnlyList<PackResources> overlayPackResources)
        : base(primaryPackResources.PackId)
    {
        _primaryPackResources = primaryPackResources;
        var stack = new List<PackResources>(overlayPackResources.Count + 1);
        for (var i = overlayPackResources.Count - 1; i >= 0; i--) stack.Add(overlayPackResources[i]);
        stack.Add(primaryPackResources);
        _packResourcesStack = stack;
    }

    public override Stream? GetRootResource(string path) => _primaryPackResources.GetRootResource(path);

    public override Stream? GetResource(PackType type, Identifier location)
    {
        foreach (var packResources in _packResourcesStack)
        {
            var resource = packResources.GetResource(type, location);
            if (resource is not null) return resource;
        }
        return null;
    }

    public override void ListResources(PackType type, string namespaceName, string pathPrefix, ISet<Identifier> output)
        => ListResources(type, namespaceName, pathPrefix, (identifier, _) => output.Add(identifier));

    public override void ListResources(PackType type, string namespaceName, string pathPrefix,
        Action<Identifier, IoSupplier<Stream>> output)
    {
        var result = new Dictionary<Identifier, IoSupplier<Stream>>();
        foreach (var packResources in _packResourcesStack)
            packResources.ListResources(type, namespaceName, pathPrefix, (identifier, supplier) =>
            {
                if (!result.ContainsKey(identifier)) result[identifier] = supplier;
            });
        foreach (var (identifier, supplier) in result) output(identifier, supplier);
    }

    public override ISet<string> GetNamespaces(PackType type)
    {
        var result = new HashSet<string>();
        foreach (var packResources in _packResourcesStack) result.UnionWith(packResources.GetNamespaces(type));
        return result;
    }

    public override T? GetMetadataSection<T>(MetadataSectionType<T> type) where T : class
        => _primaryPackResources.GetMetadataSection(type);

    public override void Dispose()
    {
        foreach (var packResources in _packResourcesStack) packResources.Dispose();
    }
}
