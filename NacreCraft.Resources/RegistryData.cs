using System.Text.Json;
using NacreCraft.Codec;
using NacreCraft.Registry;

namespace NacreCraft.Resources;

//RegistryData, data-driven registry declaration, maps to vanilla RegistryDataLoader.RegistryData
//Pairs "which registry" with "how the element is decoded" so RegistryDataLoader can scan and load by directory
//The non-generic base class lets declarations of different element types share one list
public abstract class RegistryData
{
    //RegistryId is the registry's own identifier, it determines the scan directory data/<namespace>/<RegistryId.Path>/
    public abstract Identifier RegistryId { get; }

    //PrepareHolder creates the standalone holder for one element before any decoding
    //Every file element gets one up front, so an element referencing a later one resolves to an unbound holder instead of failing
    internal abstract void PrepareHolder(Identifier elementId);

    //RemoveHolder drops the holder of an element that failed to decode, so freeze does not report it as unbound
    internal abstract void RemoveHolder(Identifier elementId);

    //TryDecode decodes one element without registering it, so decoding can run in parallel and registration stays on one thread
    internal abstract bool TryDecode(JsonElement root, Identifier elementId, RegistryAccess context, out object? value, out string error);

    //Register writes a decoded element into the target registry, must run on the loading thread
    internal abstract void Register(Identifier elementId, object value);

    //Boxed, weakly typed registry loading, for registries declared as Registry<object> on the Registries side
    //The Registry project cannot reference the Game density function/noise settings types in reverse, those registries can only be declared with object keys
    //Elements decoded by the strongly typed codec are boxed and written into the object registry
    public static RegistryData Boxed<T>(WritableRegistry<object> registry, Codec<T> codec) where T : class
        => new BoxedRegistryData<T>(registry, codec);

    //Decode decodes a parsed element with the codec, a failure throws and the caller turns it into an error string
    //RegistryData<T> and BoxedRegistryData<T> differ only in how they write to the registry, reading and decoding share this one place
    //JsonElementOps is read-only and does not build a JsonNode DOM, which keeps the data loading path small and fast
    private protected static T Decode<T>(JsonElement root, RegistryAccess context, Codec<T> codec) where T : class
    {
        var ops = new RegistryOps<JsonValueRef>(JsonElementOps.Instance, context);
        return codec.Parse(ops, new JsonValueRef(root)).GetOrThrow();
    }
}

//RegistryData<T>, registry data declaration for a concrete element type
public sealed class RegistryData<T> : RegistryData where T : class
{
    public RegistryData(WritableRegistry<T> registry, Codec<T> elementCodec)
    {
        Registry = registry;
        ElementCodec = elementCodec;
    }

    //Registry is the target registry, loading must happen before Freeze
    public WritableRegistry<T> Registry { get; }

    //ElementCodec, the element JSON codec
    public Codec<T> ElementCodec { get; }

    public override Identifier RegistryId => Registry.Key.Identifier;

    internal override void PrepareHolder(Identifier elementId)
    {
        if (Registry is MappedRegistry<T> mapped)
            mapped.GetOrCreateHolder(elementId);
    }

    internal override void RemoveHolder(Identifier elementId)
    {
        if (Registry is MappedRegistry<T> mapped)
            mapped.RemoveHolder(elementId);
    }

    internal override bool TryDecode(JsonElement root, Identifier elementId, RegistryAccess context, out object? value, out string error)
    {
        error = string.Empty;
        try
        {
            value = Decode(root, context, ElementCodec);
            return true;
        }
        catch (Exception ex)
        {
            value = null;
            error = $"{elementId}: {ex.Message}";
            return false;
        }
    }

    internal override void Register(Identifier elementId, object value)
    {
        var typed = (T)value;
        Registry.Register(ResourceKey<T>.Create(Registry.Key, elementId), typed, RegistrationInfo.BuiltIn);
        //The element's own identifier can only be backfilled from the registry name, the vanilla id lives only in the registry
        if (typed is RegistryIdentified identified) identified.SetRegistryId(elementId);
    }
}

//BoxedRegistryData<T>, loading implementation for strongly typed elements into a weakly typed registry
//No such form exists in vanilla, it is a compromise forced by the layering between this project's Registry and Game projects
internal sealed class BoxedRegistryData<T> : RegistryData where T : class
{
    private readonly WritableRegistry<object> _registry;
    private readonly Codec<T> _codec;

    public BoxedRegistryData(WritableRegistry<object> registry, Codec<T> codec)
    {
        _registry = registry;
        _codec = codec;
    }

    public override Identifier RegistryId => _registry.Key.Identifier;

    internal override void PrepareHolder(Identifier elementId)
    {
        if (_registry is MappedRegistry<object> mapped)
            mapped.GetOrCreateHolder(elementId);
    }

    internal override void RemoveHolder(Identifier elementId)
    {
        if (_registry is MappedRegistry<object> mapped)
            mapped.RemoveHolder(elementId);
    }

    internal override bool TryDecode(JsonElement root, Identifier elementId, RegistryAccess context, out object? value, out string error)
    {
        error = string.Empty;
        try
        {
            value = Decode(root, context, _codec);
            return true;
        }
        catch (Exception ex)
        {
            value = null;
            error = $"{elementId}: {ex.Message}";
            return false;
        }
    }

    internal override void Register(Identifier elementId, object value)
    {
        _registry.Register(ResourceKey<object>.Create(_registry.Key, elementId), value, RegistrationInfo.BuiltIn);
        //The element's own identifier can only be backfilled from the registry name, the vanilla id lives only in the registry
        if (value is RegistryIdentified identified) identified.SetRegistryId(elementId);
    }
}
