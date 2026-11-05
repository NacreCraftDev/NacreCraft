using NacreCraft.Game.Util.WorldUpdate;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix;

//ApplyInFolders runs a group of operations against every folder a relation resolves, maps to vanilla net.minecraft.util.filefix.operations.ApplyInFolders
public sealed record ApplyInFolders(FileRelation Folders, IReadOnlyList<FileFixOperation> FileFixOperations) : FileFixOperation
{
    public void Fix(IWorldFileSystem fileSystem, UpgradeProgress upgradeProgress)
    {
        foreach (var path in Folders(fileSystem.Root))
        {
            var subFileSystem = fileSystem.Sub(fileSystem.ToRelative(path));
            foreach (var operation in FileFixOperations)
                operation.Fix(subFileSystem, upgradeProgress);
        }
    }
}
