using System.Text.Json;
using NacreCraft.Config;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Schemas;
using NacreCraft.Game.Util.WorldUpdate;
using NacreCraft.Logging;
using NacreCraft.Nbt;
using NacreCraft.Storage;
using NacreCraft.Util;

namespace NacreCraft.Game.Util.FileFix;

//FileFixerUpper orchestrates the file-level world upgrade, maps to vanilla net.minecraft.util.filefix.FileFixerUpper
//The COW tree is driven through IWorldFileSystem so the operations see the same paths whether they run on the real world
//folder or on the copy-on-write overlay
public sealed class FileFixerUpper
{
    private const int FileFixerIntroductionVersion = 4772;
    private const string FileFixDirectoryName = "filefix";
    private const string NewWorldTempName = "new_world";
    private const string UpgradeInProgressName = "upgrade_in_progress.json";

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly DataFixerBuilder.Result _dataFixer;
    private readonly IReadOnlyList<FileFix> _fileFixes;
    private readonly int _latestFileFixerVersion;

    public FileFixerUpper(DataFixerBuilder.Result dataFixer, IReadOnlyList<FileFix> fileFixes, int latestFileFixerVersion)
    {
        _dataFixer = dataFixer;
        _fileFixes = fileFixes.ToList();
        _latestFileFixerVersion = latestFileFixerVersion;
    }

    public static int WorldVersionToFileFixerVersion(int levelDataVersion)
        => levelDataVersion < FileFixerIntroductionVersion ? 0 : levelDataVersion;

    public bool RequiresFileFixing(int levelDataVersion)
        => WorldVersionToFileFixerVersion(levelDataVersion) < _latestFileFixerVersion;

    public CompoundTag Fix(LevelStorageAccess worldAccess, CompoundTag levelDataTag, UpgradeProgress upgradeProgress)
        => Fix(worldAccess, levelDataTag, upgradeProgress, SharedConstants.WorldDataVersion);

    public CompoundTag Fix(LevelStorageAccess worldAccess, CompoundTag levelDataTag, UpgradeProgress upgradeProgress, int toVersion)
    {
        var loadedVersion = NbtUtils.GetDataVersion(levelDataTag);
        if (RequiresFileFixing(loadedVersion))
        {
            Log.Info($"Starting upgrade for world \"{worldAccess.LevelId}\"");
            var worldFolder = worldAccess.WorldDir;
            var fileFixDirectory = Path.Combine(worldFolder, FileFixDirectoryName);
            var tempWorld = Path.Combine(fileFixDirectory, NewWorldTempName);
            List<FileMove> moves;
            try
            {
                moves = StartOrContinueFileFixing(upgradeProgress, toVersion, worldFolder, tempWorld, fileFixDirectory, loadedVersion);
            }
            catch (IOException e)
            {
                throw new AbortedFileFixException(e);
            }
            try
            {
                SwapInFixedWorld(worldAccess, moves, fileFixDirectory, tempWorld);
            }
            catch (AbortedFileFixException e)
            {
                if (e.NotRevertedMoves.Count == 0) Cleanup(fileFixDirectory);
                throw;
            }
            levelDataTag = worldAccess.GetUnfixedDataTag();
            loadedVersion = NbtUtils.GetDataVersion(levelDataTag);
        }
        var fixedLevelDataTag = DataFixTypes.Level.Update(_dataFixer.Fixer(), levelDataTag, loadedVersion, SharedConstants.WorldDataVersion);
        return AddVersionsToLevelData(fixedLevelDataTag, toVersion);
    }

    private List<FileMove> StartOrContinueFileFixing(UpgradeProgress upgradeProgress, int toVersion,
        string worldFolder, string tempWorld, string fileFixDirectory, int loadedVersion)
    {
        var upgradeInProgressFile = Path.Combine(fileFixDirectory, UpgradeInProgressName);
        if (File.Exists(upgradeInProgressFile))
        {
            Log.Warning("Found previously interrupted world upgrade, attempting to continue it");
            return ReadMoves(worldFolder, tempWorld, upgradeInProgressFile);
        }
        if (Directory.Exists(fileFixDirectory)) DeleteDirectory(fileFixDirectory);
        try
        {
            Directory.CreateDirectory(fileFixDirectory);
            return ApplyFileFixersOnCow(upgradeProgress, loadedVersion, toVersion, worldFolder, fileFixDirectory, tempWorld);
        }
        catch
        {
            Cleanup(fileFixDirectory);
            throw;
        }
    }

    private static void DeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to delete directory '{directory}': {e.Message}");
        }
    }

    private static void Cleanup(string fileFixDirectory)
    {
        try
        {
            DeleteDirectory(fileFixDirectory);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to clean up: {e}");
        }
    }

    private List<FileMove> ApplyFileFixersOnCow(UpgradeProgress upgradeProgress, int loadedVersion, int toVersion,
        string worldFolder, string fileFixDirectory, string tempWorld)
    {
        var fileSystem = CopyOnWriteFileSystem.Create(
            Path.GetFileName(worldFolder), worldFolder, Path.Combine(fileFixDirectory, "cow"),
            path => path == fileFixDirectory);
        try
        {
            ApplyFileFixers(upgradeProgress, loadedVersion, toVersion, new CowWorldFileSystem(fileSystem));
            var moves = fileSystem.CollectMoveOperations(tempWorld);
            Directory.CreateDirectory(tempWorld);
            CopyOnWriteFileSystem.CreateDirectories(moves.Directories);
            CopyOnWriteFileSystem.MoveFiles(moves.CopiedFiles);
            return moves.PreexistingFiles;
        }
        finally
        {
            fileSystem.Close();
        }
    }

    public void ApplyFileFixers(UpgradeProgress upgradeProgress, int loadedVersion, int toVersion, IWorldFileSystem fileSystem)
    {
        var applicableFixers = GetApplicableFixers(loadedVersion, toVersion);
        upgradeProgress.SetType(UpgradeProgress.Type.FILES);
        CountFiles(applicableFixers, upgradeProgress);
        upgradeProgress.SetStatus(UpgradeProgress.Status.UPGRADING);
        upgradeProgress.SetApplicableFixerAmount(applicableFixers.Count);
        foreach (var fileFix in applicableFixers)
        {
            upgradeProgress.IncrementRunningFileFixer();
            fileFix.RunFixOperations(fileSystem, upgradeProgress);
        }
        WriteUpdatedLevelData(fileSystem, toVersion);
        if (fileSystem.Exists("level.dat_old")) fileSystem.Delete("level.dat_old");
        if (fileSystem.Exists("session.lock")) fileSystem.Delete("session.lock");
    }

    private List<FileFix> GetApplicableFixers(int fromVersion, int toVersion)
    {
        var fileFixerFromVersion = WorldVersionToFileFixerVersion(fromVersion);
        return _fileFixes
            .Where(fileFix => fileFix.GetVersion() > fileFixerFromVersion && fileFix.GetVersion() <= toVersion)
            .ToList();
    }

    private static void CountFiles(IReadOnlyList<FileFix> applicableFixers, UpgradeProgress upgradeProgress)
    {
        upgradeProgress.SetStatus(UpgradeProgress.Status.COUNTING);
        var totalFiles = applicableFixers.Sum(fileFix => fileFix.CountFileOperations());
        upgradeProgress.AddTotalFileFixOperations(totalFiles);
    }

    private void WriteUpdatedLevelData(IWorldFileSystem fileSystem, int toVersion)
    {
        using var input = fileSystem.OpenRead("level.dat")
            ?? throw new FileNotFoundException("level.dat not found in the world folder");
        var unfixedLevelDat = NbtIo.ReadCompressed(input, NbtAccounter.UnlimitedHeap());
        var unfixedDataTag = unfixedLevelDat.GetCompound("Data") ?? new CompoundTag();
        var dataVersion = NbtUtils.GetDataVersion(unfixedDataTag);
        var fixedTag = DataFixTypes.Level.Update(_dataFixer.Fixer(), unfixedDataTag, dataVersion, toVersion);
        AddVersionsToLevelData(fixedTag, toVersion);
        var root = new CompoundTag();
        root.Put("Data", fixedTag);
        using var output = fileSystem.OpenWrite("level.dat");
        NbtIo.WriteCompressed(root, output);
    }

    private static CompoundTag AddVersionsToLevelData(CompoundTag fixedTag, int toVersion)
    {
        NbtUtils.AddDataVersion(fixedTag, toVersion);
        PrimaryLevelData.WriteLastPlayed(fixedTag);
        PrimaryLevelData.WriteVersionTag(fixedTag);
        return fixedTag;
    }

    public static void SwapInFixedWorld(LevelStorageAccess worldAccess, IReadOnlyList<FileMove> moves,
        string fileFixDirectory, string tempWorld)
    {
        var worldFolder = worldAccess.WorldDir;
        var savesDirectory = Path.GetDirectoryName(worldFolder)
            ?? throw new AbortedFileFixException(new IOException($"World folder has no parent: {worldFolder}"));
        var worldName = Path.GetFileName(worldFolder);
        FileSystemCapabilities capabilities;
        string tempWorldTopLevel;
        string oldWorldFolder;
        try
        {
            capabilities = DetectFileSystemCapabilities(fileFixDirectory);
            tempWorldTopLevel = Path.Combine(savesDirectory, FileUtil.FindAvailableName(savesDirectory, worldName + " upgraded", ""));
            oldWorldFolder = Path.Combine(savesDirectory, FileUtil.FindAvailableName(savesDirectory, worldName + " OUTDATED", ""));
        }
        catch (Exception e)
        {
            throw new AbortedFileFixException(e);
        }
        if (!capabilities.AtomicMove) throw new AtomicMoveNotSupportedFileFixException(capabilities);
        Log.Info($"File system capabilities: {capabilities}");
        var movesFile = Path.Combine(fileFixDirectory, UpgradeInProgressName);
        try
        {
            WriteMoves(moves, worldFolder, tempWorld, movesFile);
        }
        catch (Exception e)
        {
            throw new AbortedFileFixException(e, Array.Empty<FileMove>(), capabilities);
        }
        Log.Info($"Applying file structure changes for world \"{worldAccess.LevelId}\"");
        try
        {
            Log.Info("Moving files into new file structure");
            CopyOnWriteFileSystem.MoveFilesWithRetry(moves);
            Log.Info("Moving new world to top level");
            Directory.Move(tempWorld, tempWorldTopLevel);
        }
        catch (Exception e)
        {
            Log.Error($"Encountered error while trying to create new world folder: {e}");
            var failedMoves = CopyOnWriteFileSystem.TryRevertMoves(moves);
            if (failedMoves.Count == 0) TryDeleteFile(movesFile);
            throw new AbortedFileFixException(e, failedMoves, capabilities);
        }
        Log.Info("Complete move");
        TryDeleteFile(movesFile);
        Log.Info("Start cleanup");
        TryDeleteFile(Path.Combine(worldFolder, "level.dat"));
        TryDeleteFile(Path.Combine(worldFolder, "level.dat_old"));
        try
        {
            worldAccess.ReleaseTemporarilyAndRun(() =>
            {
                Log.Info("Moving out old world folder");
                try
                {
                    Directory.Move(worldFolder, oldWorldFolder);
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to move outdated world folder out of the way; will try to delete instead: {e}");
                    try
                    {
                        DeleteDirectory(worldFolder);
                    }
                    catch (Exception e2)
                    {
                        throw new FailedCleanupFileFixException(e2, Path.GetFileName(tempWorldTopLevel), capabilities);
                    }
                }
                Log.Info("Moving in new world folder");
                try
                {
                    Directory.Move(tempWorldTopLevel, worldFolder);
                }
                catch (Exception e)
                {
                    throw new FailedCleanupFileFixException(e, Path.GetFileName(tempWorldTopLevel), capabilities);
                }
            });
        }
        catch (IOException e)
        {
            throw new FailedCleanupFileFixException(e, Path.GetFileName(tempWorldTopLevel), capabilities);
        }
        Log.Info($"Done applying file structure changes for world \"{worldAccess.LevelId}\". Cleaning up outdated data...");
        try
        {
            if (Directory.Exists(oldWorldFolder)) DeleteDirectory(oldWorldFolder);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to clean up old world folder: {e}");
        }
        Log.Info($"Upgrade done for world \"{worldAccess.LevelId}\"");
    }

    private static void WriteMoves(IReadOnlyList<FileMove> moves, string fromDirectory, string toDirectory, string filePath)
    {
        var manifest = new UpgradeInProgress(moves
            .Select(move => new ManifestMove(
                Path.GetRelativePath(fromDirectory, move.From).Replace('\\', '/'),
                Path.GetRelativePath(toDirectory, move.To).Replace('\\', '/')))
            .ToList());
        File.WriteAllText(filePath, JsonSerializer.Serialize(manifest, ManifestJsonOptions));
    }

    private static List<FileMove> ReadMoves(string fromDirectory, string toDirectory, string filePath)
    {
        var manifest = JsonSerializer.Deserialize<UpgradeInProgress>(File.ReadAllText(filePath), ManifestJsonOptions)
            ?? throw new IOException("Empty upgrade manifest");
        return manifest.Moves.Select(move => new FileMove(
            GuardedResolve(fromDirectory, move.From), GuardedResolve(toDirectory, move.To))).ToList();
    }

    //GuardedResolve keeps a manifest entry inside its root, maps to the guarded path codec of vanilla
    private static string GuardedResolve(string root, string relative)
    {
        var resolved = Path.GetFullPath(Path.Combine(root, relative));
        var rootFull = Path.GetFullPath(root);
        if (!resolved.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new IOException($"Manifest path escapes its root: {relative}");
        return resolved;
    }

    public static FileSystemCapabilities DetectFileSystemCapabilities(string dir)
        => new(SupportsAtomicMove(dir), SupportsHardLinks());

    private static bool SupportsAtomicMove(string dir)
    {
        var sourceFile = Path.Combine(dir, Guid.NewGuid().ToString());
        var targetFile = Path.Combine(dir, Guid.NewGuid().ToString());
        try
        {
            File.WriteAllBytes(sourceFile, Array.Empty<byte>());
            File.Move(sourceFile, targetFile);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            TryDeleteFile(sourceFile);
            TryDeleteFile(targetFile);
        }
    }

    //.NET has no portable hard-link API, so the fixer always takes the manifest path
    private static bool SupportsHardLinks() => false;

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to delete {path}: {e.Message}");
        }
    }

    private sealed record UpgradeInProgress(IReadOnlyList<ManifestMove> Moves);

    private sealed record ManifestMove(string From, string To);

    public sealed class Builder
    {
        public readonly List<FileFix> fileFixes = new();
        private readonly int _currentVersion;
        private int _latestFileFixerVersion;
        private readonly List<Schema> _knownSchemas = new();

        public Builder(int currentVersion) => _currentVersion = currentVersion;

        public void AddFixer(FileFix fileFix)
        {
            if (!_knownSchemas.Contains(fileFix.GetSchema()))
                throw new ArgumentException("Tried to add file fixer with unknown schema. Add it through FileFixerUpper#addSchema instead");
            var fileFixVersion = fileFix.GetVersion();
            if (fileFixVersion > _currentVersion)
                throw new ArgumentException($"Tried to add too recent file fix for version: {fileFixVersion}. The data version of the game is: {_currentVersion}");
            if (fileFixes.Count > 0 && fileFixes[^1].GetVersion() > fileFixVersion)
                throw new ArgumentException($"Tried to add too recent file fix for version: {fileFixVersion}. The most recent file fix version is {fileFixes[^1].GetVersion()}");
            fileFixes.Add(fileFix);
        }

        public Schema AddSchema(DataFixerBuilder fixerUpper, int version, Func<int, Schema?, Schema> factory)
        {
            _latestFileFixerVersion = Math.Max(version, _latestFileFixerVersion);
            var schema = fixerUpper.AddSchema(version, factory);
            _knownSchemas.Add(schema);
            return schema;
        }

        public FileFixerUpper Build(DataFixerBuilder.Result dataFixer)
            => new(dataFixer, fileFixes, _latestFileFixerVersion);
    }
}
