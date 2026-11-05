using NacreCraft.Util;

namespace NacreCraft.Game.Util.FileFix;

//RealWorldFileSystem a plain world directory backed by System.IO, maps to the default NIO provider vanilla runs on
public sealed class RealWorldFileSystem(string root) : IWorldFileSystem
{
    public string Root => root;

    public bool Exists(string path) => Path.Exists(Resolve(path));

    public bool IsDirectory(string path) => Directory.Exists(Resolve(path));

    public void Move(string from, string to)
    {
        var source = Resolve(from);
        var target = Resolve(to);
        if (Directory.Exists(source)) Directory.Move(source, target);
        else File.Move(source, target);
    }

    public void Delete(string path)
    {
        var target = Resolve(path);
        if (Directory.Exists(target)) Directory.Delete(target);
        else File.Delete(target);
    }

    public void CreateDirectories(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        FileUtil.CreateDirectoriesSafe(Resolve(path));
    }

    public IReadOnlyList<string> List(string directory)
        => Directory.GetFileSystemEntries(Resolve(directory)).Select(Path.GetFileName).OfType<string>().ToList();

    public string ToRelative(string path)
        => Path.IsPathRooted(path) ? Path.GetRelativePath(root, path).Replace('\\', '/') : path.Replace('\\', '/');

    public string? ResolveRealPath(string path) => Resolve(path);

    public IWorldFileSystem Sub(string path) => new RealWorldFileSystem(Resolve(path));

    public Stream? OpenRead(string path)
    {
        var target = Resolve(path);
        return File.Exists(target) ? File.OpenRead(target) : null;
    }

    public Stream OpenWrite(string path)
    {
        var target = Resolve(path);
        var parent = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(parent)) FileUtil.CreateDirectoriesSafe(parent);
        return new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.Read);
    }

    private string Resolve(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
}
