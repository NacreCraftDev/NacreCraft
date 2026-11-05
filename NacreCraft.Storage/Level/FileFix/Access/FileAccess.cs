using NacreCraft.Logging;

namespace NacreCraft.Storage;

//FileAccess opens every file a relation resolves and closes them together, maps to vanilla net.minecraft.util.filefix.access.FileAccess
public sealed class FileAccess<T> : IDisposable where T : IDisposable
{
    private readonly FileAccessProvider _provider;
    private readonly FileResourceType<T> _type;
    private readonly FileRelation _relation;
    private List<T>? _files;

    public FileAccess(FileAccessProvider provider, FileResourceType<T> type, FileRelation relation)
    {
        _provider = provider;
        _type = type;
        _relation = relation;
    }

    //Get lazily opens the resolved files, maps to vanilla get
    public List<T> Get()
    {
        if (_files is not null) return _files;
        var baseDirectory = _provider.BaseDirectory
            ?? throw new InvalidOperationException("Cannot access world files");
        _files = _relation(baseDirectory)
            .Select(path => _type.Create(path, _provider.DataVersion, _provider.Fixer))
            .ToList();
        return _files;
    }

    //GetOnlyFile the single resolved file, maps to vanilla getOnlyFile
    public T GetOnlyFile()
    {
        var files = Get();
        if (files.Count != 1)
            throw new InvalidOperationException($"Trying to get only file, but there are {files.Count} files");
        return files[0];
    }

    public void Dispose()
    {
        if (_files is null) return;
        foreach (var file in _files)
        {
            try
            {
                file.Dispose();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to close file: {e}");
            }
        }
        _files = null;
    }
}
