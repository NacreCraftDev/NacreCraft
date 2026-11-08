using NacreCraft.Codec;

namespace NacreCraft.Game.Server.JsonRpc;

//Schema one typed OpenRPC schema, maps to vanilla net.minecraft.server.jsonrpc.api.Schema
//Child schemas are stored in their info form; the generic parameter only carries the value codec
public sealed record Schema<T>(Optional<Uri> Reference, IReadOnlyList<string> Types, Optional<SchemaInfo> Items,
    IReadOnlyDictionary<string, SchemaInfo> Properties, IReadOnlyList<string> EnumValues, Codec<T> ValueCodec)
{
    //Info the non-generic schema shape, maps to vanilla info
    public SchemaInfo Info() => new(Reference, Types, Items, Properties, EnumValues);

    public static Schema<T> OfRef(Uri reference, Codec<T> codec)
        => new(Optional<Uri>.Of(reference), Array.Empty<string>(), Optional<SchemaInfo>.Empty(),
            new Dictionary<string, SchemaInfo>(), Array.Empty<string>(), codec);

    public static Schema<T> OfType(string type, Codec<T> codec) => OfTypes(new[] { type }, codec);

    public static Schema<T> OfTypes(IReadOnlyList<string> types, Codec<T> codec)
        => new(Optional<Uri>.Empty(), types, Optional<SchemaInfo>.Empty(),
            new Dictionary<string, SchemaInfo>(), Array.Empty<string>(), codec);

    public static Schema<T> OfEnum(IReadOnlyList<string> enumValues, Codec<T> codec)
        => new(Optional<Uri>.Empty(), new[] { "string" }, Optional<SchemaInfo>.Empty(),
            new Dictionary<string, SchemaInfo>(), enumValues, codec);

    public static Schema<IReadOnlyList<T>> ArrayOf(Schema<T> item)
        => new(Optional<Uri>.Empty(), new[] { "array" }, Optional<SchemaInfo>.Of(item.Info()),
            new Dictionary<string, SchemaInfo>(), Array.Empty<string>(), item.ValueCodec.ListOf());

    public static Schema<T> Record(Codec<T> codec)
        => new(Optional<Uri>.Empty(), new[] { "object" }, Optional<SchemaInfo>.Empty(),
            new Dictionary<string, SchemaInfo>(), Array.Empty<string>(), codec);

    public Schema<T> WithField<TField>(string name, Schema<TField> field)
    {
        var properties = new Dictionary<string, SchemaInfo>(Properties) { [name] = field.Info() };
        return this with { Properties = properties };
    }

    public Schema<IReadOnlyList<T>> AsArray() => ArrayOf(this);

    public static SchemaComponent RegisterSchema(string name, Schema<T> schema)
    {
        var entry = new SchemaComponent(name, ReferenceUtil.CreateLocalReference(name), schema.Info());
        SchemaComponents.Add(entry);
        return entry;
    }

    //RegisterTypedSchema registers the component and keeps the typed schema so methods can reference it with its codec
    public static TypedSchemaComponent<T> RegisterTypedSchema(string name, Schema<T> schema)
    {
        var entry = RegisterSchema(name, schema);
        return new TypedSchemaComponent<T>(entry, schema);
    }

    public static IReadOnlyList<SchemaComponent> GetSchemaRegistry() => SchemaComponents.All();
}

//TypedSchemaComponent a registered schema kept in its typed form, maps to vanilla SchemaComponent<T>
//The document only needs the info half; the typed half lets an RPC method reference the component and still encode values
public sealed class TypedSchemaComponent<T>(SchemaComponent component, Schema<T> schema)
{
    public SchemaComponent Component { get; } = component;

    public SchemaInfo Info => Component.Schema;

    //AsRef references this component, maps to vanilla asRef
    public Schema<T> AsRef() => Schema<T>.OfRef(Component.Ref, schema.ValueCodec);

    //AsArray an array of references, maps to vanilla asRef().asArray()
    public Schema<IReadOnlyList<T>> AsArray() => AsRef().AsArray();
}

//SchemaComponents the process-wide schema registry, maps to vanilla Schema.SCHEMA_REGISTRY
//Held outside the generic type because each closed generic would otherwise get its own static list
public static class SchemaComponents
{
    private static readonly List<SchemaComponent> Registry = new();

    public static void Add(SchemaComponent component) => Registry.Add(component);

    public static IReadOnlyList<SchemaComponent> All() => Registry;
}

//SchemaComponent a named schema in the document components, maps to vanilla SchemaComponent
public sealed record SchemaComponent(string Name, Uri Ref, SchemaInfo Schema);
