namespace NacreCraft.Storage;

//FileAccessFactory opens one world file, maps to vanilla FileResourceType.AccessFactory
//The factory receives the data fixer because this project has no global fixer; vanilla reads DataFixers.getDataFixer()
public delegate T FileAccessFactory<out T>(string path, int dataVersion, NacreCraft.DataFixer.DataFixer fixer) where T : IDisposable;

//FileResourceType describes how to open one kind of world file, maps to vanilla net.minecraft.util.filefix.access.FileResourceType
public sealed class FileResourceType<T> where T : IDisposable
{
    private readonly FileAccessFactory<T> _factory;

    public FileResourceType(FileAccessFactory<T> factory) => _factory = factory;

    public T Create(string path, int dataVersion, NacreCraft.DataFixer.DataFixer fixer)
        => _factory(path, dataVersion, fixer);
}
