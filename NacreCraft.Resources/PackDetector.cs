namespace NacreCraft.Resources;

//PackDetector decides what a path in a pack folder is, maps to vanilla net.minecraft.server.packs.repository.PackDetector
//A directory counts only when it carries a pack.mcmeta, a regular file counts only when it ends in .zip
//Vanilla also walks symlinks through DirectoryValidator; NC has no content validation yet, so a symlink is followed as it is
public abstract class PackDetector<T> where T : class
{
    //DetectPackResources classifies one entry, null when the entry is not a pack
    public T? DetectPackResources(string content)
    {
        if (Directory.Exists(content))
            return File.Exists(Path.Combine(content, "pack.mcmeta")) ? CreateDirectoryPack(content) : null;
        if (File.Exists(content) && content.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return CreateZipPack(content);
        return null;
    }

    protected abstract T? CreateZipPack(string content);

    protected abstract T? CreateDirectoryPack(string content);
}
