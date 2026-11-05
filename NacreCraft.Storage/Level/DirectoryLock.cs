using System.Text;

namespace NacreCraft.Storage;

//DirectoryLock, directory lock, maps to vanilla net.minecraft.world.level.storage.DirectoryLock
//Uses a session.lock file for exclusivity, preventing multiple processes opening the same world
//Exclusivity comes from the share mode alone, no platform-specific calls
public sealed class DirectoryLock : IDisposable
{
    public const string LockFileName = "session.lock";
    private readonly FileStream _stream;

    private DirectoryLock(FileStream stream)
    {
        _stream = stream;
    }

    //Acquire takes the lock in the given directory
    //Throws DirectoryNotFoundException when the directory is missing
    //Throws IOException when the lock is taken, meaning the world is in use by another process
    public static DirectoryLock Acquire(string dir)
    {
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"World directory not found: {dir}");

        var lockPath = Path.Combine(dir, LockFileName);
        //FileShare.None is the whole lock. Windows refuses any second handle through the share mode, and Unix takes it
        //through flock. Adding FileStream.Lock on top would be a second mechanism: Windows keeps that one in a separate
        //table, but a filesystem that collapses both into one slot, which WSL's drvfs does, makes the second call fail
        //against the first and reports it as another process holding the file
        var stream = new FileStream(
            lockPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        try
        {
            //Write the world-open timestamp for external diagnostics
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var bytes = Encoding.UTF8.GetBytes(timestamp.ToString());
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
        return new DirectoryLock(stream);
    }

    //Closing the handle is what releases the lock on both platforms
    public void Dispose() => _stream.Dispose();
}
