using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix;

//CowWorldFileSystem routes the fix operations into the copy-on-write tree, maps to vanilla running NIO on the COW provider
//Reads fall through to the base world, writes and moves land in the temp area and are collected when the fix completes
public sealed class CowWorldFileSystem : IWorldFileSystem
{
    private readonly CopyOnWriteFileSystem _fileSystem;
    private readonly string _prefix;

    public CowWorldFileSystem(CopyOnWriteFileSystem fileSystem, string prefix = "")
    {
        _fileSystem = fileSystem;
        _prefix = prefix.Replace('\\', '/').Trim('/');
    }

    public string Root => string.IsNullOrEmpty(_prefix) ? "/" : "/" + _prefix;

    public bool Exists(string path) => _fileSystem.FileTree.ByPathOrNull(Resolve(path)) is not null;

    public bool IsDirectory(string path) => _fileSystem.FileTree.ByPathOrNull(Resolve(path)) is DirectoryNode;

    public void Move(string from, string to)
        => _fileSystem.Provider.Move(Resolve(from), Resolve(to), replaceExisting: false);

    public void Delete(string path) => _fileSystem.Provider.Delete(Resolve(path));

    public void CreateDirectories(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var current = string.Empty;
        foreach (var segment in path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = string.IsNullOrEmpty(current) ? segment : current + "/" + segment;
            if (!Exists(current)) _fileSystem.Provider.CreateDirectory(Resolve(current));
        }
    }

    public IReadOnlyList<string> List(string directory)
        => _fileSystem.Provider.NewDirectoryStream(Resolve(directory))
            .Select(path => path.GetFileName()?.ToString())
            .OfType<string>()
            .ToList();

    public string ToRelative(string path)
    {
        var normalized = path.Replace('\\', '/');
        var root = Root;
        if (normalized.StartsWith(root, StringComparison.Ordinal)) normalized = normalized[root.Length..];
        return normalized.Trim('/');
    }

    //The access layer opens real files, and the COW tree has no stable real path for an entry until the fix is collected
    public string? ResolveRealPath(string path) => null;

    public IWorldFileSystem Sub(string path)
        => new CowWorldFileSystem(_fileSystem, string.IsNullOrEmpty(_prefix) ? path : _prefix + "/" + path);

    public Stream? OpenRead(string path)
        => Exists(path) ? _fileSystem.Provider.OpenChannel(Resolve(path), FileMode.Open, FileAccess.Read) : null;

    public Stream OpenWrite(string path)
        => _fileSystem.Provider.OpenChannel(Resolve(path), FileMode.Create, FileAccess.Write);

    private CopyOnWriteFSPath Resolve(string path)
        => _fileSystem.GetPath(Root.TrimEnd('/') + "/" + path.Replace('\\', '/').TrimStart('/'));
}
