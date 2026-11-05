using NacreCraft.Logging;

namespace NacreCraft.Game.Util.FileFix;

//FileFixUtil filesystem helpers for the fix operations, maps to vanilla net.minecraft.util.filefix.FileFixUtil
public static class FileFixUtil
{
    public const string NamespacePattern = "([a-z0-9_.-]+)";

    //MoveFile moves one entry, skipping when the source is missing or the target already exists, maps to vanilla moveFile
    public static void MoveFile(IWorldFileSystem fileSystem, string from, string to)
    {
        if (!fileSystem.Exists(from)) return;
        if (fileSystem.Exists(to))
        {
            Log.Warning($"Target already exists, skipping move from {from} to {to}");
            return;
        }
        fileSystem.CreateDirectories(WorldFileSystems.Parent(to));
        fileSystem.Move(from, to);
    }

    //DeleteFileOrEmptyDirectory deletes a file or an empty directory, tolerating the single .DS_Store entry
    //maps to vanilla deleteFileOrEmptyDirectory
    public static void DeleteFileOrEmptyDirectory(IWorldFileSystem fileSystem, string file)
    {
        if (!fileSystem.Exists(file)) return;
        if (fileSystem.IsDirectory(file))
        {
            try
            {
                var entries = fileSystem.List(file);
                if (entries.Count == 1 && entries[0] == ".DS_Store")
                {
                    Log.Debug($"Attempting to delete DS_Store at '{file}'");
                    try
                    {
                        fileSystem.Delete(WorldFileSystems.Combine(file, entries[0]));
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Failed to delete file '{entries[0]}' at '{file}': {e.Message}");
                    }
                }
                entries = fileSystem.List(file);
                if (entries.Count != 0)
                {
                    Log.Warning($"Failed to delete directory '{file}', as it's not empty. Content: {string.Join(", ", entries)}");
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to delete directory '{file}' because {e.Message}");
                return;
            }
        }
        try
        {
            fileSystem.Delete(file);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to delete '{file}': {e.Message}");
        }
    }
}
