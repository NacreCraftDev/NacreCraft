using NacreCraft.Logging;

namespace NacreCraft.Resources;

//DownloadCacheCleaner trims the resource-pack download cache, maps to vanilla net.minecraft.server.packs.DownloadCacheCleaner
public static class DownloadCacheCleaner
{
    //VacuumCacheDir deletes the oldest per-pack files until at most maxFiles remain, maps to vanilla vacuumCacheDir
    public static void VacuumCacheDir(string cacheDir, int maxFiles)
    {
        try
        {
            var files = ListFilesWithModificationTimes(cacheDir);
            var toRemove = files.Count - maxFiles;
            if (toRemove <= 0) return;
            files.Sort((left, right) => right.Modified.CompareTo(left.Modified));
            var prioritized = PrioritizeFilesInDirs(files);
            prioritized.Reverse();
            prioritized.Sort((left, right) => right.RemovalPriority.CompareTo(left.RemovalPriority));
            var emptyDirectoryCandidates = new HashSet<string>();
            for (var i = 0; i < toRemove && i < prioritized.Count; i++)
            {
                var entry = prioritized[i];
                try
                {
                    File.Delete(entry.Path);
                    if (entry.RemovalPriority == 0)
                    {
                        var parent = System.IO.Path.GetDirectoryName(entry.Path);
                        if (!string.IsNullOrEmpty(parent)) emptyDirectoryCandidates.Add(parent);
                    }
                }
                catch (IOException e)
                {
                    Log.Warning($"Failed to delete cache file {entry.Path}: {e.Message}");
                }
            }
            emptyDirectoryCandidates.Remove(cacheDir);
            foreach (var directory in emptyDirectoryCandidates)
            {
                try
                {
                    if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                        Directory.Delete(directory);
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (IOException e)
                {
                    Log.Warning($"Failed to delete empty cache directory {directory}: {e.Message}");
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Failed to vacuum cache dir {cacheDir}: {e.Message}");
        }
    }

    private static List<PathAndTime> ListFilesWithModificationTimes(string cacheDir)
    {
        var result = new List<PathAndTime>();
        if (!Directory.Exists(cacheDir)) return result;
        foreach (var file in Directory.EnumerateFiles(cacheDir, "*", SearchOption.AllDirectories))
        {
            if (System.IO.Path.GetDirectoryName(file) == cacheDir) continue;
            result.Add(new PathAndTime(file, File.GetLastWriteTimeUtc(file)));
        }
        return result;
    }

    //PrioritizeFilesInDirs ranks files inside a pack directory so whole directories empty out together
    private static List<PathAndPriority> PrioritizeFilesInDirs(List<PathAndTime> filesAndDates)
    {
        var countByDir = new Dictionary<string, int>();
        foreach (var entry in filesAndDates)
        {
            var directory = System.IO.Path.GetDirectoryName(entry.Path) ?? string.Empty;
            countByDir[directory] = countByDir.GetValueOrDefault(directory) + 1;
        }
        return filesAndDates
            .Select(entry => new PathAndPriority(entry.Path,
                countByDir.GetValueOrDefault(System.IO.Path.GetDirectoryName(entry.Path) ?? string.Empty)))
            .ToList();
    }

    private sealed record PathAndTime(string Path, DateTime Modified);

    private sealed record PathAndPriority(string Path, int RemovalPriority);
}
