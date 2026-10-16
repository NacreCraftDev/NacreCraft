using NacreCraft.Registry;

namespace NacreCraft.Resources;

//VanillaPackResources the built-in vanilla pack, maps to vanilla net.minecraft.server.packs.VanillaPackResources
//Vanilla reads this content from inside the game jar; NC reads the same assets/ and data/ directories off the extracted game root
//Built-in metadata covers the case where the game root has no pack.mcmeta, which is how vanilla describes its own pack
public sealed class VanillaPackResources : PackResources
{
    private readonly FolderPackResources _delegate;
    private readonly PackMetadataSection _builtInMetadata;

    public VanillaPackResources(string rootPath, PackMetadataSection builtInMetadata) : base("vanilla")
    {
        _delegate = new FolderPackResources("vanilla", rootPath);
        _builtInMetadata = builtInMetadata;
    }

    public override Stream? GetRootResource(string path) => _delegate.GetRootResource(path);

    public override Stream? GetResource(PackType type, Identifier location) => _delegate.GetResource(type, location);

    public override void ListResources(PackType type, string namespaceName, string pathPrefix, ISet<Identifier> output)
        => _delegate.ListResources(type, namespaceName, pathPrefix, output);

    public override ISet<string> GetNamespaces(PackType type) => _delegate.GetNamespaces(type);

    //GetMetadataSection falls back to the built-in description and format range when the root pack.mcmeta cannot supply one
    //Only the pack section has a built-in fallback; other sections still come from the file
    public override T? GetMetadataSection<T>(MetadataSectionType<T> type) where T : class
    {
        var section = base.GetMetadataSection(type);
        if (section is not null) return section;
        return type.Name == PackMetadataSection.SectionName && _builtInMetadata is T builtIn ? builtIn : null;
    }

    public override void Dispose() => _delegate.Dispose();
}
