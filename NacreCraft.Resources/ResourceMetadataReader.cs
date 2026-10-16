using System.Text.Json.Nodes;
using NacreCraft.Codec;

namespace NacreCraft.Resources;

//ResourceMetadataReader reads an arbitrary section out of a pack's root pack.mcmeta
//maps to vanilla net.minecraft.server.packs.resources.ResourceMetadata
public static class ResourceMetadataReader
{
    //FileName the metadata file name, shared by every section
    public const string FileName = "pack.mcmeta";

    //Read reads a pack's root pack.mcmeta and decodes one section, null when the file or the section is missing
    public static T? Read<T>(PackResources pack, MetadataSectionType<T> type) where T : class
    {
        using var stream = pack.GetRootResource(FileName);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return Read(reader.ReadToEnd(), type);
    }

    //Read decodes one section out of pack.mcmeta text
    public static T? Read<T>(string json, MetadataSectionType<T> type) where T : class
    {
        if (JsonOps.Parse(json).GetOrThrow() is not JsonObject root) return null;
        if (!root.TryGetPropertyValue(type.Name, out var section) || section is null) return null;
        return type.ValueCodec.Parse(JsonOps.Instance, section).GetOrThrow();
    }
}
