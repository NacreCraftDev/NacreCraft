using System.Text.Json.Nodes;
using NacreCraft.Codec;

namespace NacreCraft.Resources;

//ResourceMetadata the metadata sidecar of a resource, maps to vanilla net.minecraft.server.packs.resources.ResourceMetadata
//Vanilla declares this as an interface; NC uses an abstract class so the shared empty instances can be fields
public abstract class ResourceMetadata
{
    //Empty the metadata shared by every resource without a sidecar
    public static readonly ResourceMetadata Empty = new EmptyMetadata();

    //EmptySupplier a supplier that always hands back Empty
    public static readonly IoSupplier<ResourceMetadata> EmptySupplier = new(() => Empty);

    //FromJsonStream parses a .mcmeta document, every section is decoded on demand by GetSection
    public static ResourceMetadata FromJsonStream(Stream input)
    {
        JsonNode? root;
        using (var reader = new StreamReader(input))
        {
            root = JsonOps.Parse(reader.ReadToEnd()).GetOrThrow();
        }
        if (root is not JsonObject obj) throw new InvalidDataException("metadata root must be a JSON object");
        return new JsonObjectMetadata(obj);
    }

    //GetSection reads one section, empty when the sidecar has no such section
    public abstract Optional<T> GetSection<T>(MetadataSectionType<T> type);

    //GetTypedSection reads one section and pairs the value with its type, maps to vanilla getTypedSection
    public Optional<MetadataSectionType<T>.TypedValue<T>> GetTypedSection<T>(MetadataSectionType<T> type)
        => GetSection(type).Map(type.WithValue);

    //Of builds metadata holding a single section, maps to vanilla ResourceMetadata.of
    public static ResourceMetadata Of<T>(MetadataSectionType<T> type, T value)
        => new MapBased(new Dictionary<object, object?> { [type] = value });

    //Of builds metadata holding two sections, maps to vanilla ResourceMetadata.of
    public static ResourceMetadata Of<T1, T2>(MetadataSectionType<T1> type1, T1 value1,
        MetadataSectionType<T2> type2, T2 value2)
        => new MapBased(new Dictionary<object, object?> { [type1] = value1, [type2] = value2 });

    //EmptyMetadata the metadata of a resource with no sidecar, every section is missing
    private sealed class EmptyMetadata : ResourceMetadata
    {
        public override Optional<T> GetSection<T>(MetadataSectionType<T> type) => Optional<T>.Empty();
    }

    //JsonObjectMetadata a parsed .mcmeta root, sections are decoded lazily by their own codec
    private sealed class JsonObjectMetadata : ResourceMetadata
    {
        private readonly JsonObject _root;

        public JsonObjectMetadata(JsonObject root) => _root = root;

        public override Optional<T> GetSection<T>(MetadataSectionType<T> type)
        {
            if (!_root.TryGetPropertyValue(type.Name, out var raw) || raw is null) return Optional<T>.Empty();
            return Optional<T>.Of(type.ValueCodec.Parse(JsonOps.Instance, raw).GetOrThrow());
        }
    }

    //MapBased metadata assembled from already decoded values, maps to vanilla ResourceMetadata.MapBased
    private sealed class MapBased : ResourceMetadata
    {
        private readonly Dictionary<object, object?> _values;

        public MapBased(Dictionary<object, object?> values) => _values = values;

        public override Optional<T> GetSection<T>(MetadataSectionType<T> type)
            => _values.TryGetValue(type, out var value) ? Optional<T>.Of((T)value!) : Optional<T>.Empty();
    }
}
