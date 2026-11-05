namespace NacreCraft.Game.Util.FileFix;

//IWorldFileSystem the filesystem surface the file fix operations need, the C# stand-in for the NIO paths vanilla uses
//Vanilla routes every Files.move/delete through whichever provider backs the path, so a COW path and a real path share the
//same operation code; this abstraction keeps that property because System.IO cannot dispatch to the COW tree
public interface IWorldFileSystem
{
    //Root the directory FileRelation resolves against, a real world folder or the COW root
    string Root { get; }

    bool Exists(string path);

    bool IsDirectory(string path);

    void Move(string from, string to);

    void Delete(string path);

    void CreateDirectories(string path);

    //List names of the entries directly under the directory
    IReadOnlyList<string> List(string directory);

    //ToRelative turns a path produced by FileRelation into one relative to Root
    string ToRelative(string path);

    //Resolve a relative path to the underlying path the access layer can open, null when the filesystem is virtual
    string? ResolveRealPath(string path);

    //Sub a filesystem rooted at a subfolder, paths then resolve below that folder
    IWorldFileSystem Sub(string path);

    //OpenRead opens a file for reading, null when the COW tree has no such entry
    Stream? OpenRead(string path);

    //OpenWrite opens a file for writing; a COW write copies the base file into the temp area first
    Stream OpenWrite(string path);
}

public static class WorldFileSystems
{
    //Combine a relative base folder with a file name, always using the virtual separator
    public static string Combine(string baseDirectory, string name)
    {
        if (string.IsNullOrEmpty(baseDirectory)) return name.Replace('\\', '/');
        return baseDirectory.TrimEnd('/') + "/" + name.Replace('\\', '/');
    }

    //Parent the containing folder of a relative path, empty at the root
    public static string Parent(string path)
    {
        var normalized = path.Replace('\\', '/').TrimEnd('/');
        var index = normalized.LastIndexOf('/');
        return index < 0 ? string.Empty : normalized[..index];
    }
}
