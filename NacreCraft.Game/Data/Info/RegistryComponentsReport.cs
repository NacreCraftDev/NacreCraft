using System.Text.Json.Nodes;
using NacreCraft.Codec;
using NacreCraft.Game.Data;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;

namespace NacreCraft.Game.Data.Info;

//RegistryComponentsReport writes the default data components of every item as a datapack entry, maps to vanilla net.minecraft.data.info.RegistryComponentsReport
//Vanilla walks DataComponentInitializers pending components; NC items expose their defaults through Item.Components, which is the
//already baked equivalent, and the patch is built from that map
public sealed class RegistryComponentsReport(PackOutput output) : DataProvider
{
    public Task Run(CachedOutput cache)
    {
        var tasks = new List<Task>();
        var registryPath = BuiltInRegistries.ITEM.Key.Identifier;
        foreach (var entry in BuiltInRegistries.ITEM.EntrySet)
        {
            var patch = DataComponentPatch.FromMap(entry.Value.Components);
            if (patch.IsEmpty) continue;
            var encoded = DataComponentPatch.PersistentCodec.EncodeStart(JsonOps.Instance, patch).GetOrThrow();
            var root = new JsonObject { ["minecraft:components"] = encoded };
            var element = entry.Key.Identifier;
            var path = Path.Combine(output.GetOutputFolder(PackOutput.Target.DATA_PACK),
                element.Namespace, registryPath.Path, element.Path + ".json");
            tasks.Add(DataProvider.SaveStable(cache, root, path));
        }
        return Task.WhenAll(tasks);
    }

    public string GetName() => "Default Components";
}
