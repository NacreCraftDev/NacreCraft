using System.IO.Compression;

namespace NacreCraft.Resources;

//IoSupplier, a deferred stream factory, maps to vanilla net.minecraft.server.packs.resources.IoSupplier
//Every Get opens a fresh stream, so a resource can be read again without holding the handle in between
public sealed class IoSupplier<T>
{
    private readonly Func<T> _get;

    public IoSupplier(Func<T> get)
    {
        _get = get;
    }

    public T Get() => _get();

    //Create opens the file on every Get, maps to vanilla IoSupplier.create(Path)
    public static IoSupplier<Stream> Create(string path) => new(() => File.OpenRead(path));

    //Create opens the zip entry on every Get, maps to vanilla IoSupplier.create(ZipFile, ZipEntry)
    public static IoSupplier<Stream> Create(ZipArchive archive, ZipArchiveEntry entry) => new(entry.Open);
}
