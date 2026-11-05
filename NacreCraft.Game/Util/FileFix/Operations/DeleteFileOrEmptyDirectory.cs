using NacreCraft.Game.Util.WorldUpdate;

namespace NacreCraft.Game.Util.FileFix;

//DeleteFileOrEmptyDirectory removes a file or an empty directory, maps to vanilla net.minecraft.util.filefix.operations.DeleteFileOrEmptyDirectory
public sealed record DeleteFileOrEmptyDirectory(string Target) : FileFixOperation
{
    public void Fix(IWorldFileSystem fileSystem, UpgradeProgress upgradeProgress)
        => FileFixUtil.DeleteFileOrEmptyDirectory(fileSystem, Target);
}
