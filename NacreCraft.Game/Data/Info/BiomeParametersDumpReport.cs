using System.Text.Json.Nodes;
using NacreCraft.Codec;
using NacreCraft.Game.Data;
using NacreCraft.Game.World.Level.LevelGen;

namespace NacreCraft.Game.Data.Info;

//BiomeParametersDumpReport dumps the multi-noise parameter table of every known preset, maps to vanilla net.minecraft.data.info.BiomeParametersDumpReport
//Vanilla encodes Climate.ParameterList through its codec; NC presets expose their point list and biome keys directly, so this
//provider writes the same {"biomes":[{"biome":..., "parameters":...}]} shape by hand
public sealed class BiomeParametersDumpReport(PackOutput output) : DataProvider
{
    public Task Run(CachedOutput cache)
    {
        var tasks = new List<Task>();
        foreach (var preset in new[] { MultiNoisePreset.Overworld, MultiNoisePreset.Nether })
        {
            var biomes = new JsonArray();
            var points = preset.Points;
            var keys = preset.BiomeKeys;
            var count = Math.Min(points.Count, keys.Count);
            for (var i = 0; i < count; i++)
            {
                biomes.Add(new JsonObject
                {
                    ["biome"] = keys[i].Identifier.ToString(),
                    ["parameters"] = Climate.ParameterPointCodec.EncodeStart(JsonOps.Instance, points[i]).GetOrThrow(),
                });
            }
            var root = new JsonObject { ["biomes"] = biomes };
            var path = Path.Combine(output.GetOutputFolder(PackOutput.Target.REPORTS), "biome_parameters",
                preset.Id.Path + ".json");
            tasks.Add(DataProvider.SaveStable(cache, root, path));
        }
        return Task.WhenAll(tasks);
    }

    public string GetName() => "Biome Parameters";
}
