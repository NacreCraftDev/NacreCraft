using NacreCraft.Game.Util.WorldUpdate;

namespace NacreCraft.Game.Util.FileFix;

//Move moves one file or directory, maps to vanilla net.minecraft.util.filefix.operations.Move
public sealed record Move(string From, string To) : FileFixOperation
{
    //Relative rebases the move under a source and target folder, maps to vanilla relative
    public Move Relative(string sourceFolder, string targetFolder)
        => new($"{sourceFolder}/{From}", $"{targetFolder}/{To}");

    public void Fix(IWorldFileSystem fileSystem, UpgradeProgress upgradeProgress)
        => FileFixUtil.MoveFile(fileSystem, From, To);
}
