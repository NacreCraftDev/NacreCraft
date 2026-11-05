using NacreCraft.Game.Util.WorldUpdate;

namespace NacreCraft.Game.Util.FileFix;

//FileFixOperation one step of a world file fix, maps to vanilla net.minecraft.util.filefix.operations.FileFixOperation
public interface FileFixOperation
{
    void Fix(IWorldFileSystem fileSystem, UpgradeProgress upgradeProgress);
}
