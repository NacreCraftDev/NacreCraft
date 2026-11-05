using NacreCraft.Game.Util.WorldUpdate;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix;

//ModifyContent runs a content fixer with the world directory bound in the access provider, maps to vanilla net.minecraft.util.filefix.operations.ModifyContent
//The access layer opens real files, so content fixes only run on a real world directory until a COW-aware reader lands
public sealed class ModifyContent(FileAccessProvider fileAccessProvider, ModifyContent.FixFunction fixFunction) : FileFixOperation
{
    public void Fix(IWorldFileSystem fileSystem, UpgradeProgress upgradeProgress)
    {
        var baseDirectory = fileSystem.ResolveRealPath(string.Empty)
            ?? throw new NotSupportedException("Content fixes require the real world filesystem");
        try
        {
            using (fileAccessProvider.UseBaseDirectory(baseDirectory))
                fixFunction(upgradeProgress);
        }
        finally
        {
            fileAccessProvider.Dispose();
        }
    }

    public delegate void FixFunction(UpgradeProgress upgradeProgress);

    public delegate FixFunction FileAccessFunction(FileAccessProvider provider);
}
