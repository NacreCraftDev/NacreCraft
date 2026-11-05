using NacreCraft.Util;

namespace NacreCraft.Game.Util.FileFix;

//FileFixException base of the world upgrade failures, maps to vanilla net.minecraft.util.filefix.FileFixException
public class FileFixException : Exception
{
    protected readonly FileSystemCapabilities? Capabilities;

    public FileFixException(Exception? cause, FileSystemCapabilities? fileSystemCapabilities)
        : base(cause?.Message, cause)
    {
        Capabilities = fileSystemCapabilities;
    }

    protected virtual CrashReport CreateCrashReport()
    {
        var report = CrashReport.ForThrowable(this, "Upgrading world failed with errors");
        var capabilities = report.AddCategory("File system capabilities");
        capabilities.SetDetail("Hard Links", Capabilities is null ? "null" : Capabilities.HardLinks.ToString());
        capabilities.SetDetail("Atomic Move", Capabilities is null ? "null" : Capabilities.AtomicMove.ToString());
        return report;
    }

    public ReportedException MakeReportedException() => new(CreateCrashReport());
}

public sealed class AbortedFileFixException : FileFixException
{
    public AbortedFileFixException(Exception cause, IReadOnlyList<NacreCraft.Storage.FileMove> notRevertedMoves,
        FileSystemCapabilities? capabilities = null)
        : base(cause, capabilities)
    {
        NotRevertedMoves = notRevertedMoves;
    }

    public AbortedFileFixException(Exception cause) : this(cause, Array.Empty<NacreCraft.Storage.FileMove>())
    {
    }

    public IReadOnlyList<NacreCraft.Storage.FileMove> NotRevertedMoves { get; }

    protected override CrashReport CreateCrashReport()
    {
        var report = base.CreateCrashReport();
        var failedReverts = report.AddCategory("Moves that failed to revert");
        for (var i = 0; i < NotRevertedMoves.Count; i++)
            failedReverts.SetDetail(i.ToString(), $"{NotRevertedMoves[i].From} -> {NotRevertedMoves[i].To}");
        return report;
    }
}

public sealed class AtomicMoveNotSupportedFileFixException(FileSystemCapabilities capabilities)
    : FileFixException(null, capabilities);

public sealed class CanceledFileFixException() : FileFixException(null, null);

public sealed class FailedCleanupFileFixException(Exception cause, string newWorldFolderName,
    FileSystemCapabilities? capabilities) : FileFixException(cause, capabilities)
{
    public string NewWorldFolderName => newWorldFolderName;

    protected override CrashReport CreateCrashReport()
    {
        var report = base.CreateCrashReport();
        report.AddCategory("World upgrade").SetDetail("New Name", NewWorldFolderName);
        return report;
    }
}
