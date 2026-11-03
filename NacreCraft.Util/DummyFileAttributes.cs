namespace NacreCraft.Util;

//DummyFileAttributes placeholder attributes for the virtual filesystem, maps to vanilla net.minecraft.util.DummyFileAttributes
//Time is always Unix epoch, size and file key are always empty; only distinguishes directory from file
public abstract class DummyFileAttributes
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.FromUnixTimeMilliseconds(0);

    //Directory directory placeholder attributes
    public static readonly DummyFileAttributes Directory = new DirectoryEntry();

    //File file placeholder attributes
    public static readonly DummyFileAttributes File = new FileEntry();

    public virtual DateTimeOffset LastModifiedTime => Epoch;

    public virtual DateTimeOffset LastAccessTime => Epoch;

    public virtual DateTimeOffset CreationTime => Epoch;

    public virtual bool IsSymbolicLink => false;

    public virtual bool IsOther => false;

    public virtual long Size => 0L;

    public virtual object? FileKey => null;

    public abstract bool IsRegularFile { get; }

    public abstract bool IsDirectory { get; }

    private sealed class DirectoryEntry : DummyFileAttributes
    {
        public override bool IsRegularFile => false;

        public override bool IsDirectory => true;
    }

    private sealed class FileEntry : DummyFileAttributes
    {
        public override bool IsRegularFile => true;

        public override bool IsDirectory => false;
    }
}
