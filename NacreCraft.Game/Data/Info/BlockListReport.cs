using System.Text.Json.Nodes;
using NacreCraft.Game.Data;
using NacreCraft.Registry;

namespace NacreCraft.Game.Data.Info;

//BlockListReport dumps every block with its properties and expanded states, maps to vanilla net.minecraft.data.info.BlockListReport
//The vanilla definition section encodes the block type through BlockTypes.CODEC; NC has no block-type codec yet, so the
//definition is written as the block id string instead and the difference is recorded in the batch leftovers
public sealed class BlockListReport(PackOutput output) : DataProvider
{
    public Task Run(CachedOutput cache)
    {
        var root = new JsonObject();
        foreach (var entry in BuiltInRegistries.BLOCK.EntrySet)
        {
            var block = entry.Value;
            var blockEntry = new JsonObject();
            var defaultState = block.DefaultBlockState;

            var defaultProperties = defaultState.GetProperties();
            if (defaultProperties.Count > 0)
            {
                var properties = new JsonObject();
                foreach (var property in defaultProperties)
                {
                    var values = new JsonArray();
                    foreach (var value in property.PossibleValuesAsObjects)
                        values.Add(property.GetNameForValue(value));
                    properties[property.Name] = values;
                }
                blockEntry["properties"] = properties;
            }

            var states = new JsonArray();
            foreach (var state in block.AllStates)
            {
                var stateEntry = new JsonObject();
                var stateProperties = new JsonObject();
                foreach (var propertyValue in state.GetValues())
                    stateProperties[propertyValue.Property.Name] =
                        propertyValue.Property.GetNameForValue(propertyValue.Value);
                if (stateProperties.Count > 0) stateEntry["properties"] = stateProperties;
                stateEntry["id"] = state.Id;
                if (state == defaultState) stateEntry["default"] = true;
                states.Add(stateEntry);
            }
            blockEntry["states"] = states;
            blockEntry["definition"] = block.Id.ToString();
            root[entry.Key.Identifier.ToString()] = blockEntry;
        }
        var path = Path.Combine(output.GetOutputFolder(PackOutput.Target.REPORTS), "blocks.json");
        return DataProvider.SaveStable(cache, root, path);
    }

    public string GetName() => "Block List";
}
