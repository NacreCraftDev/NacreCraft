namespace NacreCraft.Resources;

//AbstractPackResources a pack that caches its parsed pack.mcmeta, maps to vanilla net.minecraft.server.packs.AbstractPackResources
public abstract class AbstractPackResources : PackResources
{
    private readonly PackLocationInfo _location;
    private ResourceMetadata? _metadata;

    protected AbstractPackResources(PackLocationInfo location) : base(location.Id) => _location = location;

    public PackLocationInfo Location() => _location;

    public override T? GetMetadataSection<T>(MetadataSectionType<T> type) where T : class
    {
        _metadata ??= LoadMetadata(this);
        var section = _metadata.GetSection(type);
        return section.IsPresent ? section.Get() : null;
    }

    //LoadMetadata reads pack.mcmeta once, maps to vanilla loadMetadata
    public static ResourceMetadata LoadMetadata(PackResources packResources)
    {
        using var stream = packResources.GetRootResource(PackResources.PACK_META);
        return stream is null ? ResourceMetadata.Empty : ResourceMetadata.FromJsonStream(stream);
    }
}
