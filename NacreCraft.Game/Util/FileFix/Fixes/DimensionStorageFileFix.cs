using NacreCraft.DataFixer.Schemas;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix.Fixes;

//DimensionStorageFileFix moves the old dimension folders and saved data into dimensions/, maps to vanilla net.minecraft.util.filefix.fixes.DimensionStorageFileFix
public sealed class DimensionStorageFileFix : FileFix
{
    public DimensionStorageFileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
        : base(schema, fixer)
    {
    }

    public override void MakeFixer()
    {
        AddFileFixOperation(FileFixOperations.ApplyInFolders(FileRelations.DIMENSIONS_DATA, new FileFixOperation[]
        {
            FileFixOperations.Move("chunks.dat", "minecraft/chunk_tickets.dat"),
            FileFixOperations.Move("raids.dat", "minecraft/raids.dat"),
            FileFixOperations.Move("world_border.dat", "minecraft/world_border.dat")
        }));

        AddFileFixOperation(FileFixOperations.GroupMove(new Dictionary<string, string>
        {
            ["data"] = "dimensions/minecraft/overworld/data/minecraft",
            ["DIM-1/data"] = "dimensions/minecraft/the_nether/data/minecraft",
            ["DIM1/data"] = "dimensions/minecraft/the_end/data/minecraft"
        }, new[]
        {
            FileFixOperations.Move("chunks.dat", "chunk_tickets.dat"),
            FileFixOperations.MoveSimple("raids.dat"),
            FileFixOperations.Move("raids_end.dat", "raids.dat"),
            FileFixOperations.MoveSimple("world_border.dat")
        }));

        AddFileFixOperation(FileFixOperations.Delete("DIM1/data"));
        AddFileFixOperation(FileFixOperations.Delete("DIM-1/data"));

        AddFileFixOperation(FileFixOperations.ApplyInFolders(FileRelations.DATA, new FileFixOperation[]
        {
            FileFixOperations.Move("scoreboard.dat", "minecraft/scoreboard.dat"),
            FileFixOperations.Move("stopwatches.dat", "minecraft/stopwatches.dat"),
            FileFixOperations.MoveRegex("command_storage_([a-z0-9_.-]+)\\.dat", "$1/command_storage.dat"),
            FileFixOperations.Move("idcounts.dat", "minecraft/maps/last_id.dat"),
            FileFixOperations.MoveRegex("map_(\\d+)\\.dat", "minecraft/maps/$1.dat"),
            FileFixOperations.Move("random_sequences.dat", "minecraft/random_sequences.dat")
        }));

        AddFileFixOperation(FileFixOperations.GroupMove(new Dictionary<string, string>
        {
            [""] = "dimensions/minecraft/overworld",
            ["DIM-1"] = "dimensions/minecraft/the_nether",
            ["DIM1"] = "dimensions/minecraft/the_end"
        }, new[]
        {
            FileFixOperations.MoveSimple("region"),
            FileFixOperations.MoveSimple("entities"),
            FileFixOperations.MoveSimple("poi")
        }));

        AddFileFixOperation(FileFixOperations.Delete("DIM-1"));
        AddFileFixOperation(FileFixOperations.Delete("DIM1"));
    }
}
