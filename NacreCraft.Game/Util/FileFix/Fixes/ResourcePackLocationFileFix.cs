using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.Util.FileFix.Fixes;

//ResourcePackLocationFileFix moves resources.zip into resourcepacks/, maps to vanilla net.minecraft.util.filefix.fixes.ResourcePackLocationFileFix
public sealed class ResourcePackLocationFileFix : FileFix
{
    public ResourcePackLocationFileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
        : base(schema, fixer)
    {
    }

    public override void MakeFixer()
        => AddFileFixOperation(FileFixOperations.Move("resources.zip", "resourcepacks/resources.zip"));
}
