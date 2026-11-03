using NacreCraft.Util;

namespace NacreCraft.Storage;

//CopyOnWriteFSProvider, access entry point of the copy-on-write filesystem, maps to vanilla net.minecraft.util.filefix.virtualfilesystem.CopyOnWriteFSProvider
//Vanilla extends FileSystemProvider; here only the used behaviors are kept: open stream, list directory, add/remove/move, check access
public sealed class CopyOnWriteFSProvider
{
    public const string Scheme = "x-mc-copy-on-write";

    private readonly CopyOnWriteFileSystem _fs;

    public CopyOnWriteFSProvider(CopyOnWriteFileSystem fileSystem) => _fs = fileSystem;

    public string GetScheme() => Scheme;

    //OpenChannel opens a file stream; before writing ensure the file is copied into the temp directory, and when creating attach the node to the file tree
    //Mode maps the java OpenOption set: CreateNew is CREATE_NEW, Create is CREATE plus truncate, OpenOrCreate is CREATE, Append adds WRITE and CREATE
    public Stream OpenChannel(CopyOnWriteFSPath path, FileMode mode, FileAccess access)
    {
        var node = _fs.FileTree.ByPathOrNull(path);
        switch (node)
        {
            case FileNode file:
                if (WantsWrite(access)) file.EnsureCopy();
                return OpenStream(file.StoragePath, mode, access);
            case DirectoryNode:
                throw new CowFSFileSystemException($"{path}: not a regular file");
            default:
                if (!Creates(mode)) throw new CowFSNoSuchFileException(path.ToString());
                var parentPath = path.GetParent() ?? throw new CowFSNoSuchFileException(path.ToString());
                var parent = _fs.FileTree.DirectoryByPath(parentPath);
                var tempFile = _fs.CreateTemporaryFilePath();
                var result = OpenStream(tempFile, mode, access);
                parent.AddChild(new FileNode(path, tempFile, true));
                return result;
        }
    }

    //WantsWrite whether the open options update the file content, maps to vanilla wantsWrite
    //.NET rejects append together with read only access, so any access other than read writes
    private static bool WantsWrite(FileAccess access) => access != FileAccess.Read;

    //Creates whether the open options create a missing file, maps to the CREATE and CREATE_NEW checks of vanilla
    private static bool Creates(FileMode mode)
        => mode is FileMode.Create or FileMode.CreateNew or FileMode.OpenOrCreate or FileMode.Append;

    //NewDirectoryStream lists the child paths of a directory
    public IReadOnlyList<CopyOnWriteFSPath> NewDirectoryStream(CopyOnWriteFSPath dir, Func<CopyOnWriteFSPath, bool>? filter = null)
    {
        var directoryNode = _fs.FileTree.DirectoryByPath(dir);
        var result = new List<CopyOnWriteFSPath>();
        foreach (var child in directoryNode.Children)
        {
            if (filter is null || filter(child.Path)) result.Add(child.Path);
        }
        return result;
    }

    //CreateDirectory creates an empty directory, throws when the name already exists
    public void CreateDirectory(CopyOnWriteFSPath dir)
    {
        var parentPath = dir.GetParent() ?? throw new CowFSFileAlreadyExistsException(dir.ToString());
        var parentFolder = _fs.FileTree.DirectoryByPath(parentPath);
        var name = dir.GetFileName()?.ToString() ?? throw new CowFSFileAlreadyExistsException(dir.ToString());
        if (parentFolder.GetChild(name) is not null) throw new CowFSFileAlreadyExistsException(dir.ToString());
        parentFolder.AddChild(new DirectoryNode(dir));
    }

    //Delete detaches a node; both the root node and a non-empty directory are rejected
    public void Delete(CopyOnWriteFSPath path)
    {
        var node = _fs.FileTree.ByPath(path);
        if (node.Parent is null) throw new CowFSFileSystemException("Can't remove root");
        var name = node.Name ?? throw new CowFSFileSystemException("Node is missing a name");
        if (node is DirectoryNode directory)
        {
            if (directory.Children.Count > 0) throw new CowFSDirectoryNotEmptyException(path.ToString());
        }
        else if (node is FileNode file)
        {
            file.DeleteCopy();
        }
        node.Parent.RemoveChild(name);
    }

    //Move re-attaches a node; when the target exists, replaceExisting decides overwrite or error
    //An atomic move is rejected, the virtual tree has no rename that could satisfy it, maps to vanilla ATOMIC_MOVE handling
    public void Move(CopyOnWriteFSPath source, CopyOnWriteFSPath target, bool replaceExisting, bool atomic = false)
    {
        if (source.IsRoot()) throw new CowFSFileSystemException($"{source}: can't move root directory");
        if (atomic) throw new CowFSFileSystemException($"{source} -> {target}: CowFs does not support atomic move");
        var sourceNode = _fs.FileTree.ByPathOrNull(source) ?? throw new CowFSNoSuchFileException(source.ToString());
        var parentPath = target.ToAbsolutePath().GetParent() ?? throw new CowFSFileAlreadyExistsException(target.ToString());
        if (_fs.FileTree.ByPathOrNull(parentPath) is not DirectoryNode targetParent) throw new CowFSNoSuchFileException(target.ToString());
        var newName = target.GetFileName()?.ToString() ?? throw new CowFSNoSuchFileException(target.ToString());
        var oldChild = targetParent.GetChild(newName);
        if (oldChild is not null)
        {
            if (ReferenceEquals(oldChild, sourceNode)) return;
            if (replaceExisting) targetParent.RemoveChild(newName);
            else throw new CowFSFileAlreadyExistsException(target.ToString());
        }
        sourceNode.Parent?.RemoveChild(sourceNode.Name ?? string.Empty);
        sourceNode.SetPath(target);
        targetParent.AddChild(sourceNode);
    }

    //CheckAccess: directories check the temp directory, files check the real storage; write access requires not read-only
    //The failure stays inside the CowFS exception family, the java side raises AccessDeniedException from the backing provider
    public void CheckAccess(CopyOnWriteFSPath path, bool read)
    {
        var node = _fs.FileTree.ByPath(path);
        var target = node is DirectoryNode ? _fs.TmpDirectory : ((FileNode)node).StoragePath;
        if (!read)
        {
            if ((File.GetAttributes(target) & FileAttributes.ReadOnly) != 0)
                throw new CowFSFileSystemException($"{target}: write access denied");
            return;
        }
        if (!File.Exists(target) && !Directory.Exists(target)) throw new CowFSNoSuchFileException(target);
    }

    //ReadAttributes gives placeholder attributes for directories and the backing file attributes for files
    //Vanilla reads the storage path through Files.readAttributes, only directories use the dummy values
    public DummyFileAttributes ReadAttributes(CopyOnWriteFSPath path)
    {
        var node = _fs.FileTree.ByPath(path);
        return node is DirectoryNode ? DummyFileAttributes.Directory : new RealFileAttributes(((FileNode)node).StoragePath);
    }

    public CopyOnWriteFSPath GetRealPath(CopyOnWriteFSPath path) => _fs.FileTree.ByPath(path.ToAbsolutePath()).Path;

    //OpenStream opens the real file with the requested mode, a new stream keeps no sharing so later writes are not blocked
    private static Stream OpenStream(string path, FileMode mode, FileAccess access)
        => new FileStream(path, mode, access, access == FileAccess.Read ? FileShare.Read : FileShare.None);
}

//RealFileAttributes attributes read from the backing file, maps to the Files.readAttributes call of vanilla
//Times are read once at construction, the file tree never mutates a storage file in place
internal sealed class RealFileAttributes : DummyFileAttributes
{
    private readonly DateTimeOffset _lastModifiedTime;
    private readonly DateTimeOffset _lastAccessTime;
    private readonly DateTimeOffset _creationTime;
    private readonly long _size;
    private readonly string _storagePath;

    public RealFileAttributes(string storagePath)
    {
        var info = new FileInfo(storagePath);
        _storagePath = storagePath;
        _lastModifiedTime = info.LastWriteTimeUtc;
        _lastAccessTime = info.LastAccessTimeUtc;
        _creationTime = info.CreationTimeUtc;
        _size = info.Length;
    }

    public override DateTimeOffset LastModifiedTime => _lastModifiedTime;

    public override DateTimeOffset LastAccessTime => _lastAccessTime;

    public override DateTimeOffset CreationTime => _creationTime;

    public override long Size => _size;

    public override object FileKey => _storagePath;

    public override bool IsRegularFile => true;

    public override bool IsDirectory => false;
}
