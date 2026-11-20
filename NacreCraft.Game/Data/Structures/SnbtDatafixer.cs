using NacreCraft.DataFixer;
using NacreCraft.Game.Data;
using NacreCraft.Logging;
using NacreCraft.Nbt;
//The DataFixer namespace shadows the type name inside an unqualified reference, so the type gets an alias
using DataFixerType = NacreCraft.DataFixer.DataFixer;

namespace NacreCraft.Game.Data.Structures;

//SnbtDatafixer updates every SNBT structure in the given directories in place, maps to vanilla SnbtDatafixer
//Vanilla is a main entry that bootstraps the game first; NC exposes Run, and callers decide when the game layer is bootstrapped
public static class SnbtDatafixer
{
    //Run bootstraps the game layer and rewrites every .snbt under the given directories
    //A failure in one structure aborts the run with the source path, matching the vanilla RuntimeException wrapping
    public static void Run(IEnumerable<string> structureDirs, DataFixerType? fixer = null)
    {
        NacreCraft.Game.Bootstrap.GameBootstrap.Bootstrap();
        foreach (var directory in structureDirs)
        {
            if (!Directory.Exists(directory))
            {
                Log.Warning($"Structure directory does not exist: {directory}");
                continue;
            }
            foreach (var path in Directory.EnumerateFiles(directory, "*.snbt", SearchOption.AllDirectories))
            {
                try
                {
                    var snbt = File.ReadAllText(path);
                    var read = NbtUtils.SnbtToStructure(snbt);
                    var updated = StructureUpdater.Update(path, read, fixer);
                    NbtToSnbt.WriteSnbt(CachedOutputs.NoCache, path, NbtUtils.StructureToSnbt(updated))
                        .GetAwaiter().GetResult();
                    Log.Info($"Updated structure {path}");
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Path.GetFullPath(path), e);
                }
            }
        }
    }
}
