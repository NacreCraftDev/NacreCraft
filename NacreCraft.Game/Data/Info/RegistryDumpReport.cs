using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using NacreCraft.Game.Data;
using NacreCraft.Registry;

namespace NacreCraft.Game.Data.Info;

//RegistryDumpReport dumps every built-in registry with its element protocol ids, maps to vanilla net.minecraft.data.info.RegistryDumpReport
//NC has no meta registry, so the registry list and ids come from the static BuiltInRegistries fields in declaration order
public sealed class RegistryDumpReport(PackOutput output) : DataProvider
{
    public Task Run(CachedOutput cache)
    {
        var root = new JsonObject();
        var registryId = 0;
        foreach (var (key, registry) in BuiltInRegistries.EnumerateRegistries())
        {
            root[key.ToString()] = DumpRegistry(registry, registryId);
            registryId++;
        }
        var path = Path.Combine(output.GetOutputFolder(PackOutput.Target.REPORTS), "registries.json");
        return DataProvider.SaveStable(cache, root, path);
    }

    public string GetName() => "Registry Dump";

    //DumpRegistry writes one registry: optional default key, registry protocol id and the element table
    private static JsonObject DumpRegistry(object registry, int registryId)
    {
        var result = new JsonObject();
        if (registry.GetType().GetProperty("DefaultKey")?.GetValue(registry) is Identifier defaultKey)
            result["default"] = defaultKey.ToString();
        result["protocol_id"] = registryId;

        var entries = new JsonObject();
        if (registry.GetType().GetProperty("EntrySet")?.GetValue(registry) is IEnumerable entrySet)
        {
            var getId = registry.GetType().GetMethods()
                .FirstOrDefault(method => method.Name == "GetId" && method.GetParameters().Length == 1);
            foreach (var entry in entrySet)
            {
                var keyObject = entry.GetType().GetProperty("Key")?.GetValue(entry);
                var value = entry.GetType().GetProperty("Value")?.GetValue(entry);
                if (keyObject is null || value is null) continue;
                if (keyObject.GetType().GetProperty("Identifier")?.GetValue(keyObject) is not Identifier identifier)
                    continue;
                var protocolId = -1;
                if (getId is not null)
                {
                    try
                    {
                        if (getId.Invoke(registry, new[] { value }) is int id) protocolId = id;
                    }
                    catch (Exception)
                    {
                        protocolId = -1;
                    }
                }
                entries[identifier.ToString()] = new JsonObject { ["protocol_id"] = protocolId };
            }
        }
        result["entries"] = entries;
        return result;
    }
}
