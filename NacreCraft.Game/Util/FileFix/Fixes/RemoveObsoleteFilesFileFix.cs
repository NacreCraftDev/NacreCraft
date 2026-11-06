using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.Util.FileFix.Fixes;

//RemoveObsoleteFilesFileFix deletes files no longer used by the current save format, maps to vanilla net.minecraft.util.filefix.fixes.RemoveObsoleteFilesFileFix
public sealed class RemoveObsoleteFilesFileFix : FileFix
{
    public RemoveObsoleteFilesFileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
        : base(schema, fixer)
    {
    }

    public override void MakeFixer()
    {
        AddFileFixOperation(FileFixOperations.Delete("data/villages.dat"));
        AddFileFixOperation(FileFixOperations.Delete("data/villages_end.dat"));
        AddFileFixOperation(FileFixOperations.Delete("data/villages_nether.dat"));
        AddFileFixOperation(FileFixOperations.Delete("data/advancements"));
        AddFileFixOperation(FileFixOperations.Delete("data/functions"));

        var leftovers = LegacyStructureFileFix.OverworldLegacyStructures
            .Append("Village")
            .Concat(LegacyStructureFileFix.NetherLegacyStructures)
            .Concat(LegacyStructureFileFix.EndLegacyStructures);
        foreach (var structure in leftovers)
        {
            AddFileFixOperation(FileFixOperations.Delete($"data/{structure}.dat"));
            AddFileFixOperation(FileFixOperations.Delete($"data/{structure}_index.dat"));
        }
    }
}
