namespace NacreCraft.Storage;

//FileAccessProvider owns every file access requested by one fix pass, maps to vanilla net.minecraft.util.filefix.access.FileAccessProvider
//Vanilla binds the world root through a ScopedValue; the C# counterpart is an explicit disposable scope on the provider
public sealed class FileAccessProvider : IDisposable
{
    private readonly List<IDisposable> _accessedFiles = new();
    private readonly NacreCraft.DataFixer.DataFixer _fixer;
    private bool _frozen;
    private string? _baseDirectory;

    public FileAccessProvider(int dataVersion, NacreCraft.DataFixer.DataFixer fixer)
    {
        DataVersion = dataVersion;
        _fixer = fixer;
    }

    public int DataVersion { get; }

    public NacreCraft.DataFixer.DataFixer Fixer => _fixer;

    //BaseDirectory the world root bound for the current scope, null before the first UseBaseDirectory
    public string? BaseDirectory => _baseDirectory;

    //GetFileAccess registers a new access, maps to vanilla getFileAccess
    public FileAccess<T> GetFileAccess<T>(FileResourceType<T> type, FileRelation relation) where T : IDisposable
    {
        if (_frozen) throw new InvalidOperationException("Cannot request new file access here.");
        var fileAccess = new FileAccess<T>(this, type, relation);
        _accessedFiles.Add(fileAccess);
        return fileAccess;
    }

    //Freeze stops new accesses from being registered, maps to vanilla freeze
    public void Freeze() => _frozen = true;

    //UseBaseDirectory binds the world root until the returned scope is disposed, maps to vanilla ScopedValue.where(...).run(...)
    public IDisposable UseBaseDirectory(string baseDirectory)
    {
        var previous = _baseDirectory;
        _baseDirectory = baseDirectory;
        return new BaseDirectoryScope(this, previous);
    }

    public void Dispose()
    {
        foreach (var accessedFile in _accessedFiles) accessedFile.Dispose();
    }

    private sealed class BaseDirectoryScope(FileAccessProvider provider, string? previous) : IDisposable
    {
        public void Dispose() => provider._baseDirectory = previous;
    }
}
