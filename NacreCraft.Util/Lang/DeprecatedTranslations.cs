namespace NacreCraft.Network.Chat;

using System.Text.Json;

//DeprecatedTranslations deprecated translation table, maps to vanilla net.minecraft.locale.DeprecatedTranslationsInfo
//Renamed keys carry their value over to the new key and removed keys are dropped from the table
//Vanilla applies it both to the built-in default table and to the client table assembled on reload
public sealed class DeprecatedTranslations
{
    //Empty table used when the file is absent
    public static readonly DeprecatedTranslations Empty = new([], new Dictionary<string, string>(StringComparer.Ordinal));

    private readonly List<string> _removed;
    private readonly Dictionary<string, string> _renamed;

    private DeprecatedTranslations(List<string> removed, Dictionary<string, string> renamed)
    {
        _removed = removed;
        _renamed = renamed;
    }

    //LoadFromJson parses {"removed":[...],"renamed":{from:to}}, tolerating either field being absent
    public static DeprecatedTranslations LoadFromJson(Stream stream)
    {
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var removed = new List<string>();
        if (root.TryGetProperty("removed", out var removedNode) && removedNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in removedNode.EnumerateArray())
                if (entry.GetString() is { } key) removed.Add(key);
        }
        var renamed = new Dictionary<string, string>(StringComparer.Ordinal);
        if (root.TryGetProperty("renamed", out var renamedNode) && renamedNode.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in renamedNode.EnumerateObject())
                if (entry.Value.GetString() is { } target) renamed[entry.Name] = target;
        }
        return new DeprecatedTranslations(removed, renamed);
    }

    //LoadFromDefaultResource reads assets/minecraft/lang/deprecated.json under the program root, returning Empty when absent
    public static DeprecatedTranslations LoadFromDefaultResource()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "minecraft", "lang", "deprecated.json");
        if (!File.Exists(path)) return Empty;
        try
        {
            using var stream = File.OpenRead(path);
            return LoadFromJson(stream);
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    //ApplyTo drops the removed keys and moves the renamed values, maps to vanilla applyToMap
    public void ApplyTo(Dictionary<string, string> translations)
    {
        foreach (var key in _removed) translations.Remove(key);
        foreach (var pair in _renamed)
        {
            //A rename whose source is absent removes the target, so a stale value cannot survive
            if (translations.Remove(pair.Key, out var value)) translations[pair.Value] = value;
            else translations.Remove(pair.Value);
        }
    }
}
