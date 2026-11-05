using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NacreCraft.Logging;

namespace NacreCraft.Game.Data;

//DataProvider one data generator, maps to vanilla net.minecraft.data.DataProvider
public interface DataProvider
{
    Task Run(CachedOutput cache);

    string GetName();

    //SaveStable serializes a JSON tree with two-space indentation and writes it through the cache
    //The hash feeds the cache so an unchanged file is skipped; the key order of the JsonObject is preserved
    public static Task SaveStable(CachedOutput cache, JsonNode? root, string path)
    {
        try
        {
            var text = root?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null";
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = Convert.ToHexString(SHA1.HashData(bytes));
            return cache.WriteIfNeeded(path, bytes, hash);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save file to {path}: {e.Message}");
            return Task.CompletedTask;
        }
    }
}
