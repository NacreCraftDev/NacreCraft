using NacreCraft.Codec;
using NacreCraft.Logging;
using NacreCraft.Nbt;

namespace NacreCraft.Storage;

//LevelStorageSource maps to vanilla net.minecraft.world.level.storage.LevelStorageSource
//The world storage entry point above LevelStorage: lists candidate worlds, reads their summaries and opens access
//Degraded: DirectoryLock has no isLocked probe, so summaries report locked=false; WorldDataConfiguration and the feature
//flag set live in the Game layer, so experimental is always false. The data fixer is kept for callers that upgrade chunks
public sealed class LevelStorageSource
{
    //TagData, the level.dat wrapper key, maps to vanilla TAG_DATA
    public const string TagData = "Data";

    //AllowedSymlinksConfigName, the symlink allow list file name, maps to vanilla ALLOWED_SYMLINKS_CONFIG_NAME
    public const string AllowedSymlinksConfigName = "allowed_symlinks.txt";

    private readonly string _baseDir;
    private readonly string _backupDir;
    private readonly DirectoryValidator _worldDirValidator;
    private readonly LevelStorage _storage;

    public LevelStorageSource(
        string baseDir,
        string backupDir,
        DirectoryValidator worldDirValidator,
        NacreCraft.DataFixer.DataFixer? fixerUpper)
    {
        Directory.CreateDirectory(baseDir);
        _baseDir = baseDir;
        _backupDir = backupDir;
        _worldDirValidator = worldDirValidator;
        _storage = new LevelStorage(baseDir);
    }

    //ParseValidator reads allowed_symlinks.txt, maps to vanilla parseValidator
    //A missing file yields an empty allow list, so every symlink is rejected
    public static DirectoryValidator ParseValidator(string configPath)
    {
        if (!File.Exists(configPath))
            return new DirectoryValidator(new PathAllowList(Array.Empty<PathAllowList.ConfigEntry>()));
        using var reader = File.OpenText(configPath);
        return new DirectoryValidator(PathAllowList.ReadPlain(reader));
    }

    //CreateDefault builds the standard layout under the game directory, maps to vanilla createDefault
    //The data fixer is left null until the Game layer builds one
    public static LevelStorageSource CreateDefault(string path)
        => new(
            Path.Combine(path, "saves"),
            Path.Combine(path, "backup"),
            ParseValidator(Path.Combine(path, AllowedSymlinksConfigName)),
            null);

    public string GetName() => "Anvil";

    //FindLevelCandidates lists folders that hold level.dat or level.dat_old, maps to vanilla findLevelCandidates
    public LevelCandidates FindLevelCandidates()
    {
        if (File.Exists(_baseDir))
            throw new LevelStorageException($"Level folder access failed, not a directory: {_baseDir}");
        if (!Directory.Exists(_baseDir))
        {
            try
            {
                Directory.CreateDirectory(_baseDir);
            }
            catch (Exception e)
            {
                throw new LevelStorageException($"Level folder access failed: {_baseDir}", e);
            }
        }

        try
        {
            var levels = Directory.EnumerateDirectories(_baseDir)
                .Select(CreateLevelDirectory)
                .Where(level => File.Exists(level.DataFile) || File.Exists(level.OldDataFile))
                .ToList();
            return new LevelCandidates(levels);
        }
        catch (Exception e)
        {
            throw new LevelStorageException($"Failed to list level folder: {_baseDir}", e);
        }
    }

    //LoadLevelSummaries reads every candidate world, maps to vanilla loadLevelSummaries
    //Vanilla runs each read on the IO pool and sorts by last played descending, the same shape is kept here
    public async Task<List<LevelSummary>> LoadLevelSummaries(LevelCandidates candidates)
    {
        var tasks = candidates.Levels.Select(level => Task.Run(() => ReadLevelSummary(level)));
        var summaries = await Task.WhenAll(tasks);
        return summaries.OrderBy(summary => summary).ToList();
    }

    //IsNewLevelIdAcceptable checks that the id can be created, maps to vanilla isNewLevelIdAcceptable
    public bool IsNewLevelIdAcceptable(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId)) return false;
        if (levelId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
        try
        {
            var path = GetLevelPath(levelId);
            Directory.CreateDirectory(path);
            Directory.Delete(path);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool LevelExists(string levelId) => Directory.Exists(GetLevelPath(levelId));

    public string GetLevelPath(string levelId) => Path.Combine(_baseDir, levelId);

    public string GetBaseDir() => _baseDir;

    public string GetBackupPath() => _backupDir;

    //ValidateAndCreateAccess rejects out-of-bounds symlinks before opening, maps to vanilla validateAndCreateAccess
    public LevelStorageAccess ValidateAndCreateAccess(string levelId)
    {
        var levelPath = GetLevelPath(levelId);
        var issues = _worldDirValidator.ValidateDirectory(levelPath, allowTopSymlink: true);
        if (issues.Count > 0) throw new ContentValidationException(levelPath, issues);
        return CreateAccess(levelId);
    }

    public LevelStorageAccess CreateAccess(string levelId) => _storage.CreateAccess(levelId);

    public DirectoryValidator GetWorldDirValidator() => _worldDirValidator;

    private LevelDirectory CreateLevelDirectory(string path)
    {
        var name = Path.GetFileName(path);
        return new LevelDirectory(
            name,
            path,
            Path.Combine(path, "level.dat"),
            Path.Combine(path, "level.dat_old"),
            Path.Combine(path, "icon.png"));
    }

    //ReadLevelSummary builds one summary from level.dat, maps to vanilla readLevelSummary/makeLevelSummary
    private static LevelSummary ReadLevelSummary(LevelDirectory level)
    {
        if (!File.Exists(level.DataFile))
            return new LevelSummary.CorruptedLevelSummary(level.DirectoryName, level.IconFile, GetFileModificationTime(level));

        try
        {
            var root = NbtIo.ReadCompressed(level.DataFile, NbtAccounter.UnlimitedHeap());
            var data = root.GetCompound(TagData);
            if (data is null)
            {
                Log.Warning($"Invalid root tag in {level.DataFile}");
                return new LevelSummary.CorruptedLevelSummary(level.DirectoryName, level.IconFile, GetFileModificationTime(level));
            }

            var input = new Dynamic<Tag>(NbtOps.Instance, data);
            var levelVersion = LevelVersion.Parse(input);
            if (levelVersion.LevelDataVersion != WorldData.AnvilVersionId
                && levelVersion.LevelDataVersion != WorldData.McRegionVersionId)
            {
                Log.Warning($"Unknown data version in {level.DataFile}: {levelVersion.LevelDataVersion}");
                return new LevelSummary.CorruptedLevelSummary(level.DirectoryName, level.IconFile, GetFileModificationTime(level));
            }

            var settings = LevelData.Parse(data);
            var requiresManualConversion = levelVersion.LevelDataVersion != WorldData.AnvilVersionId;
            return new LevelSummary(
                settings,
                levelVersion,
                level.DirectoryName,
                requiresManualConversion,
                requiresFileFixing: false,
                locked: false,
                experimental: false,
                icon: level.IconFile);
        }
        catch (Exception e)
        {
            Log.Error($"Exception reading {level.DataFile}: {e.Message}");
            return new LevelSummary.CorruptedLevelSummary(level.DirectoryName, level.IconFile, GetFileModificationTime(level));
        }
    }

    private static long GetFileModificationTime(LevelDirectory level)
    {
        var timestamp = GetFileModificationTime(level.DataFile) ?? GetFileModificationTime(level.OldDataFile);
        return timestamp ?? -1L;
    }

    private static long? GetFileModificationTime(string path)
        => File.Exists(path)
            ? new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero).ToUnixTimeMilliseconds()
            : null;

    //LevelDirectory names one world folder, maps to the vanilla nested LevelStorageSource.LevelDirectory
    public sealed record LevelDirectory(string DirectoryName, string Path, string DataFile, string OldDataFile, string IconFile);

    //LevelCandidates is the list of world folders, maps to the vanilla nested LevelStorageSource.LevelCandidates
    public sealed record LevelCandidates(IReadOnlyList<LevelDirectory> Levels);
}
