using System.Text.Json.Nodes;
using NacreCraft.Game.Data;
using NacreCraft.Registry;

namespace NacreCraft.Game.Data.Info;

//DatapackStructureReport describes which registries and custom pack folders a datapack can populate, maps to vanilla net.minecraft.data.info.DatapackStructureReport
//Vanilla merges BuiltInRegistries with RegistryDataLoader's worldgen/dimension lists and manual pseudo registries. NC has no
//RegistryDataLoader lists yet, so every built-in registry is reported as a built-in entry and the manual entries are added by id
public sealed class DatapackStructureReport(PackOutput output) : DataProvider
{
    public Task Run(CachedOutput cache)
    {
        var registries = new JsonObject();
        foreach (var (key, _) in BuiltInRegistries.EnumerateRegistries())
            registries[key.ToString()] = BuiltInEntry();
        //Manual entries that are not built-in registries in vanilla: recipes/advancements are pseudo registries, the rest are stable dynamic registries
        registries["minecraft:recipe"] = Entry(elements: true, tags: false, stable: true);
        registries["minecraft:advancement"] = Entry(elements: true, tags: false, stable: true);
        registries["minecraft:loot_table"] = Entry(elements: true, tags: true, stable: true);
        registries["minecraft:item_modifier"] = Entry(elements: true, tags: true, stable: true);
        registries["minecraft:predicate"] = Entry(elements: true, tags: true, stable: true);

        var others = new JsonObject
        {
            ["structure"] = CustomEntry("structure", elements: true, tags: false, stable: true),
            ["function"] = CustomEntry("mcfunction", elements: true, tags: true, stable: true),
        };
        var root = new JsonObject { ["registries"] = registries, ["others"] = others };
        var path = Path.Combine(output.GetOutputFolder(PackOutput.Target.REPORTS), "datapack.json");
        return DataProvider.SaveStable(cache, root, path);
    }

    public string GetName() => "Datapack Structure";

    //BuiltInEntry a registry whose elements are code registered, tags are allowed and the ids are stable
    private static JsonObject BuiltInEntry() => Entry(elements: false, tags: true, stable: true);

    private static JsonObject Entry(bool elements, bool tags, bool stable) => new()
    {
        ["elements"] = elements,
        ["tags"] = tags,
        ["stable"] = stable,
    };

    //CustomEntry a non-registry pack folder with its file format
    private static JsonObject CustomEntry(string format, bool elements, bool tags, bool stable)
    {
        var entry = Entry(elements, tags, stable);
        entry["format"] = format;
        return entry;
    }
}
