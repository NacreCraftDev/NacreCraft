using System.Text.Json.Nodes;
using NacreCraft.Codec;
using NacreCraft.Game.Data;
using NacreCraft.Resources;
using NacreCraft.Registry.Flag;

namespace NacreCraft.Game.Data.Metadata;

//PackMetadataGenerator writes pack.mcmeta, maps to vanilla net.minecraft.data.metadata.PackMetadataGenerator
public sealed class PackMetadataGenerator(PackOutput output) : DataProvider
{
    private readonly Dictionary<string, Func<JsonNode?>> _elements = new();

    public PackMetadataGenerator Add<T>(MetadataSectionType<T> type, T value)
    {
        _elements[type.Name] = () => type.ValueCodec.EncodeStart(JsonOps.Instance, value).Result().OrElse(null);
        return this;
    }

    public Task Run(CachedOutput cache)
    {
        var result = new JsonObject();
        foreach (var (id, data) in _elements) result[id] = data();
        return DataProvider.SaveStable(cache, result, Path.Combine(output.GetOutputFolder(), "pack.mcmeta"));
    }

    public string GetName() => "Pack Metadata";

    //ForFeaturePack the standard feature pack metadata, maps to vanilla forFeaturePack
    //Vanilla reads DetectedVersion.BUILT_IN.packVersion(SERVER_DATA); this project has no WorldVersion, so the
    //same pack format constant the resource loader uses is taken from PackCompatibilityRules
    public static PackMetadataGenerator ForFeaturePack(PackOutput output, string description)
        => new PackMetadataGenerator(output).Add(PackMetadataSection.ServerType,
            new PackMetadataSection(description, PackCompatibilityRules.ServerPackFormat.MinorRange()));

    public static PackMetadataGenerator ForFeaturePack(PackOutput output, string description, FeatureFlagSet flags)
        => ForFeaturePack(output, description).Add(FeatureFlagsMetadataSection.TYPE,
            new FeatureFlagsMetadataSection(flags));
}
