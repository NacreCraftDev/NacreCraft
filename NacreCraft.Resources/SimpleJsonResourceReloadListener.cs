using System.Text.Json.Nodes;
using NacreCraft.Codec;
using NacreCraft.Logging;
using NacreCraft.Registry;

namespace NacreCraft.Resources;

//SimpleJsonResourceReloadListener a listener that reads every JSON file under a directory through a codec
//maps to vanilla net.minecraft.server.packs.resources.SimpleJsonResourceReloadListener
//The prepared value is the id to parsed element table; subclasses decide what to do with it in apply
public abstract class SimpleJsonResourceReloadListener<T> : SimplePreparableReloadListener<Dictionary<Identifier, T>>
{
    private readonly DynamicOps<JsonNode?> _ops;
    private readonly Codec<T> _codec;
    private readonly string _directory;
    private readonly PackType _packType;

    protected SimpleJsonResourceReloadListener(Codec<T> codec, string directory, PackType packType = PackType.ServerData)
    {
        _ops = JsonOps.Instance;
        _codec = codec;
        _directory = directory;
        _packType = packType;
    }

    protected override Dictionary<Identifier, T> Prepare(ResourceManager manager, ReloadContext ctx)
    {
        var result = new Dictionary<Identifier, T>();
        ScanDirectory(manager, _packType, _directory, _ops, _codec, result);
        return result;
    }

    //ScanDirectory reads every JSON file under directory into result, maps to vanilla scanDirectory
    //A file that fails to decode is logged and skipped, a duplicate id aborts the reload like vanilla
    public static void ScanDirectory(ResourceManager manager, PackType packType, string directory,
        DynamicOps<JsonNode?> ops, Codec<T> codec, Dictionary<Identifier, T> result)
    {
        foreach (var namespaceName in manager.GetNamespaces(packType))
        {
            foreach (var resource in manager.ListResources(packType, namespaceName, directory))
            {
                var location = resource.Location;
                var id = FileToId(directory, location);
                if (id is null) continue;
                try
                {
                    using var reader = resource.OpenAsReader();
                    var parsed = codec.Parse(ops, JsonOps.Parse(reader.ReadToEnd()).GetOrThrow());
                    if (!parsed.IsSuccess)
                    {
                        Log.Error($"Couldn't parse data file '{id}' from '{location}': {parsed.ErrorMessage}");
                        continue;
                    }
                    if (result.ContainsKey(id.Value))
                        throw new InvalidOperationException($"Duplicate data file ignored with ID {id}");
                    result[id.Value] = parsed.GetOrThrow();
                }
                catch (Exception ex)
                {
                    Log.Error($"Couldn't parse data file '{id}' from '{location}': {ex.Message}");
                }
            }
        }
    }

    //FileToId strips the directory prefix and the .json suffix, maps to vanilla FileToIdConverter.fileToId
    private static Identifier? FileToId(string directory, Identifier location)
    {
        var prefix = directory + "/";
        var path = location.Path;
        if (!path.StartsWith(prefix, StringComparison.Ordinal)) return null;
        if (!path.EndsWith(".json", StringComparison.Ordinal)) return null;
        var idPath = path[prefix.Length..^".json".Length];
        return idPath.Length == 0 ? null : Identifier.FromNamespaceAndPath(location.Namespace, idPath);
    }
}
