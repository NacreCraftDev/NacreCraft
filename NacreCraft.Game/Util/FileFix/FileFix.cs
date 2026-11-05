using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Schemas;
using NacreCraft.Game.Util.WorldUpdate;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix;

//FileFix one version's file-level world fix, maps to vanilla net.minecraft.util.filefix.FileFix
//Vanilla reads the global DataFixer inside content fixes; the fixer is injected here because this project has none
public abstract class FileFix
{
    private readonly Schema _schema;
    private readonly NacreCraft.DataFixer.DataFixer _fixer;
    private readonly List<FileFixOperation> _fileFixOperations = new();

    protected FileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
    {
        _schema = schema;
        _fixer = fixer;
        MakeFixer();
    }

    public Schema GetSchema() => _schema;

    public int GetVersion() => DataFixUtils.GetVersion(_schema.GetVersionKey());

    public void AddFileFixOperation(FileFixOperation fileFixOperation) => _fileFixOperations.Add(fileFixOperation);

    //AddFileContentFix registers a content fixer and freezes its file accesses, maps to vanilla addFileContentFix
    public void AddFileContentFix(ModifyContent.FileAccessFunction fileAccessFunction)
    {
        var fileAccessProvider = new FileAccessProvider(GetVersion(), _fixer);
        var fixFunction = fileAccessFunction(fileAccessProvider);
        fileAccessProvider.Freeze();
        _fileFixOperations.Add(new ModifyContent(fileAccessProvider, fixFunction));
    }

    public int CountFileOperations() => _fileFixOperations.Count;

    //RunFixOperations runs every operation, stopping on cancel, maps to vanilla runFixOperations
    public void RunFixOperations(IWorldFileSystem fileSystem, UpgradeProgress upgradeProgress)
    {
        foreach (var fileFixOperation in _fileFixOperations)
        {
            if (upgradeProgress.IsCanceled()) throw new CanceledFileFixException();
            fileFixOperation.Fix(fileSystem, upgradeProgress);
            upgradeProgress.IncrementFinishedOperations();
        }
    }

    public abstract void MakeFixer();
}
