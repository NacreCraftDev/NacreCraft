using System.Text;
using NacreCraft.Registry;

namespace NacreCraft.Resources;

//Resource a single resource, maps to vanilla net.minecraft.server.packs.resources.Resource
//Wraps the source pack, the resource identifier, a deferred stream and the deferred sidecar metadata
public sealed class Resource
{
    //Source the pack this resource came from, maps to vanilla Resource.source
    public PackResources Source { get; }

    //Location the resource identifier inside its namespace
    public Identifier Location { get; }

    //SourcePackId the originating pack id, maps to vanilla Resource.sourcePackId
    public string SourcePackId => Source.PackId;

    private readonly IoSupplier<Stream> _streamSupplier;
    private readonly IoSupplier<ResourceMetadata> _metadataSupplier;
    private ResourceMetadata? _cachedMetadata;

    public Resource(PackResources source, Identifier location, IoSupplier<Stream> streamSupplier,
        IoSupplier<ResourceMetadata> metadataSupplier)
    {
        Source = source;
        Location = location;
        _streamSupplier = streamSupplier;
        _metadataSupplier = metadataSupplier;
    }

    //Without a metadata supplier the resource has no sidecar and shares the empty metadata
    public Resource(PackResources source, Identifier location, IoSupplier<Stream> streamSupplier)
        : this(source, location, streamSupplier, ResourceMetadata.EmptySupplier)
    {
        _cachedMetadata = ResourceMetadata.Empty;
    }

    //Open opens the resource stream, each call returns a fresh stream
    public Stream Open() => _streamSupplier.Get() ?? new MemoryStream();

    //OpenAsReader opens the resource as UTF-8 text, maps to vanilla Resource.openAsReader
    public StreamReader OpenAsReader() => new(Open(), Encoding.UTF8);

    //Metadata reads the sidecar metadata once and caches it, maps to vanilla Resource.metadata
    public ResourceMetadata Metadata()
    {
        _cachedMetadata ??= _metadataSupplier.Get();
        return _cachedMetadata;
    }
}
