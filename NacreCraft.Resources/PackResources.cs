using NacreCraft.Registry;

namespace NacreCraft.Resources;

//PackResources, resource pack content access, maps to vanilla net.minecraft.server.packs.PackResources
//Provides access from namespace:path to a resource stream
//Subclasses implement the concrete source (folder/zip/embedded resources)
public abstract class PackResources : IDisposable
{
    public string PackId { get; }

    protected PackResources(string packId)
    {
        PackId = packId;
    }

    //GetRootResource gets a stream for a resource at the pack root (such as pack.png)
    public abstract Stream? GetRootResource(string path);

    //GetResource gets a namespace resource stream (such as minecraft:textures/block/stone.png)
    public abstract Stream? GetResource(PackType type, Identifier location);

    //ListResources lists all resource identifiers matching a path prefix
    public abstract void ListResources(PackType type, string namespaceName, string pathPrefix, ISet<Identifier> output);

    //ListResources lists resources together with a supplier that reopens each stream, maps to vanilla PackResources.listResources
    //The default reopens through GetResource, so subclasses only need the identifier form
    public virtual void ListResources(PackType type, string namespaceName, string pathPrefix,
        Action<Identifier, IoSupplier<Stream>> output)
    {
        var ids = new HashSet<Identifier>();
        ListResources(type, namespaceName, pathPrefix, ids);
        foreach (var id in ids) output(id, new IoSupplier<Stream>(() => GetResource(type, id)!));
    }

    //GetNamespaces gets all namespaces contained in the resource pack
    public abstract ISet<string> GetNamespaces(PackType type);

    //GetMetadataSection reads a section out of the pack root pack.mcmeta, maps to vanilla PackResources.getMetadataSection
    //A pack overrides this to supply built-in metadata when pack.mcmeta cannot, which is what the vanilla pack does
    public virtual T? GetMetadataSection<T>(MetadataSectionType<T> type) where T : class
    {
        using var stream = GetRootResource("pack.mcmeta");
        if (stream is null) return null;
        var section = ResourceMetadata.FromJsonStream(stream).GetSection(type);
        return section.IsPresent ? section.Get() : null;
    }

    public virtual void Dispose() { }
}

//PackType, resource type enum, maps to vanilla PackType
//CLIENT_RESOURCES client resources (textures/sounds/models)
//SERVER_DATA server data (advancements/recipes/tags/functions)
public enum PackType
{
    ClientResources,
    ServerData
}
