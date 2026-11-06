using NacreCraft.DataFixer.Schemas;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix.Fixes;

//GeneratedStructuresRenameFileFix renames the generated structure folder, maps to vanilla net.minecraft.util.filefix.fixes.GeneratedStructuresRenameFileFix
public sealed class GeneratedStructuresRenameFileFix : FileFix
{
    public GeneratedStructuresRenameFileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
        : base(schema, fixer)
    {
    }

    public override void MakeFixer()
        => AddFileFixOperation(FileFixOperations.ApplyInFolders(FileRelations.GENERATED_NAMESPACES,
            new FileFixOperation[] { FileFixOperations.Move("structures", "structure") }));
}
