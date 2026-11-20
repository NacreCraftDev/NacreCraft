using System.Text.Json.Nodes;
using NacreCraft.Game.Data;

namespace NacreCraft.Game.Data.Info;

//CommandsReport writes the command tree as JSON, maps to vanilla net.minecraft.data.info.CommandsReport
//Vanilla builds a Commands dispatcher from a registry lookup and serializes it with ArgumentUtils. NC's CommandManager
//registers commands against a live MinecraftServer (save, difficulty and other commands capture it), so the tree cannot be
//built without a server. This provider emits an empty root with a note instead of a fabricated tree
public sealed class CommandsReport(PackOutput output) : DataProvider
{
    public Task Run(CachedOutput cache)
    {
        var root = new JsonObject
        {
            ["note"] = "Command tree generation requires a live server in NC; this report is intentionally empty",
            ["type"] = "root",
            ["children"] = new JsonObject(),
        };
        var path = Path.Combine(output.GetOutputFolder(PackOutput.Target.REPORTS), "commands.json");
        return DataProvider.SaveStable(cache, root, path);
    }

    public string GetName() => "Command Syntax";
}
