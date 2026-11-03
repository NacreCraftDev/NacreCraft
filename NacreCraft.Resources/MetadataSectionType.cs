using NacreCraft.Codec;

namespace NacreCraft.Resources;

//MetadataSectionType metadata section descriptor, maps to vanilla net.minecraft.server.packs.metadata.MetadataSectionType
//Pairs a section name with the codec that reads that section out of pack.mcmeta
public sealed record MetadataSectionType<T>(string Name, Codec<T> ValueCodec)
{
    //WithValue binds a decoded value back to its section type, maps to vanilla MetadataSectionType.withValue
    public TypedValue<T> WithValue(T value) => new(this, value);

    //TypedValue a section type paired with the value read out of it, maps to vanilla MetadataSectionType.WithValue
    //The nested class is renamed because C# does not allow a type and a method to share a name
    public sealed record TypedValue<TValue>(MetadataSectionType<TValue> Type, TValue Value);
}
