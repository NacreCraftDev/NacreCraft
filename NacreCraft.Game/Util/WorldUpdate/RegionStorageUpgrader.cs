using System.Text.RegularExpressions;
using NacreCraft.Config;
using NacreCraft.DataFixer;
using NacreCraft.Logging;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.WorldUpdate;

//RegionStorageUpgrader upgrades one region folder and writes the converted chunks back, maps to vanilla net.minecraft.util.worldupdate.RegionStorageUpgrader
//Vanilla subclasses SimpleRegionStorage to write into a new_ folder while reading the old one; this port keeps two storages instead,
//because the read/write split is the only part of that subclass the upgrade path uses
public class RegionStorageUpgrader
{
    private const string NewDirectoryPrefix = "new_";
    private static readonly Regex RegionFilePattern = new(@"^r\.(-?[0-9]+)\.(-?[0-9]+)\.mca$", RegexOptions.Compiled);

    private readonly DataFixer.DataFixer _dataFixer;
    private readonly UpgradeProgress _upgradeProgress;
    private readonly string _type;
    private readonly string _folderName;
    private readonly DataFixTypes _dataFixType;
    private readonly int _defaultVersion;
    private readonly bool _recreateRegionFiles;
    private readonly int _startIndex;
    private readonly CompoundTag? _dataFixContextTag;
    private readonly SortedDictionary<int, TagModifier> _tagModifiers;
    private readonly List<RegionFile> _completedFiles = new();

    protected Task? PreviousWriteFuture;
    private ResourceKey<Level>? _dimensionKey;
    private SimpleRegionStorage? _storage;
    private SimpleRegionStorage? _writeStorage;
    private List<FileToUpgrade>? _files;

    protected RegionStorageUpgrader(
        DataFixer.DataFixer dataFixer,
        DataFixTypes dataFixType,
        string type,
        string folderName,
        int defaultVersion,
        bool recreateRegionFiles,
        UpgradeProgress upgradeProgress,
        int startIndex,
        CompoundTag? dataFixContextTag,
        SortedDictionary<int, TagModifier> tagModifiers)
    {
        _dataFixer = dataFixer;
        _dataFixType = dataFixType;
        _type = type;
        _folderName = folderName;
        _defaultVersion = defaultVersion;
        _recreateRegionFiles = recreateRegionFiles;
        _upgradeProgress = upgradeProgress;
        _startIndex = startIndex;
        _dataFixContextTag = dataFixContextTag;
        _tagModifiers = tagModifiers;
    }

    public void Init(ResourceKey<Level> dimensionKey, LevelStorageAccess levelStorage)
    {
        var info = new RegionStorageInfo(levelStorage.LevelId, dimensionKey, _type);
        var regionFolder = Path.Combine(levelStorage.GetDimensionPath(dimensionKey), _folderName);
        _dimensionKey = dimensionKey;
        _storage = CreateStorage(info, regionFolder);
        _files = GetFilesToProcess(info, regionFolder);
    }

    public void Upgrade()
    {
        if (_dimensionKey is null || _storage is null || _files is null)
            throw new InvalidOperationException("RegionStorageUpgrader has not been initialized");
        if (_files.Count == 0) return;

        var totalSize = (float)_upgradeProgress.GetTotalFileFixStats().TotalOperationsValue;
        _upgradeProgress.SetStatus(UpgradeProgress.Status.UPGRADING);
        var index = 0;
        while (!_upgradeProgress.IsCanceled())
        {
            var worked = false;
            if (index < _files.Count)
            {
                var fileToUpgrade = _files[index];
                index++;
                var converted = true;
                foreach (var chunkPos in fileToUpgrade.ChunksToUpgrade)
                {
                    converted = converted && ProcessOnePosition(_storage, chunkPos);
                    worked = true;
                }
                if (_recreateRegionFiles)
                {
                    if (converted) _completedFiles.Add(fileToUpgrade.File);
                    else Log.Error($"Failed to convert region file {fileToUpgrade.File.Path}");
                }
            }

            var nextIndex = index;
            var dimensionProgress = totalSize == 0 ? 0.0f : nextIndex / totalSize;
            var totalProgress = totalSize == 0 ? 0.0f : (_startIndex + nextIndex) / totalSize;
            _upgradeProgress.SetDimensionProgress(_dimensionKey, dimensionProgress);
            _upgradeProgress.SetTotalProgress(totalProgress);
            if (!worked) break;
        }

        _upgradeProgress.SetStatus(UpgradeProgress.Status.FINISHED);
        try
        {
            Finish();
        }
        catch (Exception e)
        {
            Log.Error($"Error upgrading chunk {e}");
        }
    }

    public int FileAmount() => _files?.Count ?? 0;

    protected virtual bool TryProcessOnePosition(SimpleRegionStorage storage, ChunkPos pos)
    {
        var chunkTag = storage.Read(pos).GetAwaiter().GetResult();
        if (!chunkTag.IsPresent) return false;

        var tag = chunkTag.Get();
        var version = NbtUtils.GetDataVersion(tag);
        var latestVersion = SharedConstants.WorldDataVersion;
        var changed = false;
        foreach (var (neededVersion, modifier) in _tagModifiers)
        {
            tag = UpgradeTag(storage, tag, neededVersion);
            changed |= modifier(pos, tag);
        }

        var upgradedTag = UpgradeTag(storage, tag, latestVersion);
        changed |= version < latestVersion;
        if (changed || _recreateRegionFiles)
        {
            PreviousWriteFuture?.GetAwaiter().GetResult();
            var target = _recreateRegionFiles ? _writeStorage! : _storage!;
            PreviousWriteFuture = target.Write(pos, upgradedTag);
            return true;
        }
        return false;
    }

    protected virtual CompoundTag UpgradeTag(SimpleRegionStorage storage, CompoundTag chunkTag, int targetVersion)
        => storage.UpgradeChunkTag(chunkTag, _defaultVersion, _dataFixContextTag, targetVersion);

    private SimpleRegionStorage CreateStorage(RegionStorageInfo info, string regionFolder)
    {
        if (!_recreateRegionFiles)
            return new SimpleRegionStorage(info, regionFolder, _dataFixer, true, _dataFixType);

        _writeStorage = new SimpleRegionStorage(info.WithTypeSuffix("target"),
            ResolveRecreateDirectory(regionFolder), _dataFixer, true, _dataFixType);
        return new SimpleRegionStorage(info.WithTypeSuffix("source"), regionFolder, _dataFixer, true, _dataFixType);
    }

    private List<FileToUpgrade> GetFilesToProcess(RegionStorageInfo info, string regionFolder)
    {
        var filesToUpgrade = GetAllChunkPositions(info, regionFolder);
        _upgradeProgress.AddTotalFileFixOperations(filesToUpgrade.Count);
        _upgradeProgress.AddTotalChunks(filesToUpgrade.Sum(file => file.ChunksToUpgrade.Count));
        return filesToUpgrade;
    }

    private static List<FileToUpgrade> GetAllChunkPositions(RegionStorageInfo info, string regionFolder)
    {
        var result = new List<FileToUpgrade>();
        if (!Directory.Exists(regionFolder)) return result;

        foreach (var regionPath in Directory.EnumerateFiles(regionFolder, "*.mca", SearchOption.TopDirectoryOnly))
        {
            var match = RegionFilePattern.Match(Path.GetFileName(regionPath));
            if (!match.Success) continue;

            var xOffset = int.Parse(match.Groups[1].Value) << 5;
            var zOffset = int.Parse(match.Groups[2].Value) << 5;
            var chunkPositions = new List<ChunkPos>();
            try
            {
                using var regionFile = new RegionFile(info, regionPath, regionFolder, true);
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        var pos = new ChunkPos(x + xOffset, z + zOffset);
                        if (regionFile.DoesChunkExist(pos)) chunkPositions.Add(pos);
                    }
                }
                if (chunkPositions.Count > 0) result.Add(new FileToUpgrade(regionFile, chunkPositions));
            }
            catch (Exception e)
            {
                Log.Error($"Failed to read chunks from region file {regionPath} {e}");
            }
        }
        return result;
    }

    private bool ProcessOnePosition(SimpleRegionStorage storage, ChunkPos pos)
    {
        var converted = false;
        try
        {
            converted = TryProcessOnePosition(storage, pos);
        }
        catch (Exception e) when (e is IOException or NacreCraft.Util.ReportedException)
        {
            Log.Error($"Error upgrading chunk {pos} {e}");
            throw;
        }

        if (converted) _upgradeProgress.IncrementConverted();
        else _upgradeProgress.IncrementSkipped();
        return converted;
    }

    private void Finish()
    {
        PreviousWriteFuture?.GetAwaiter().GetResult();
        _storage?.Dispose();
        if (_writeStorage is null) return;

        _writeStorage.Dispose();
        foreach (var regionFile in _completedFiles)
        {
            var filePath = regionFile.Path;
            var directoryPath = Path.GetDirectoryName(filePath)!;
            var newFilePath = Path.Combine(ResolveRecreateDirectory(directoryPath), Path.GetFileName(filePath));
            if (!File.Exists(newFilePath))
            {
                Log.Error($"Failed to replace an old region file. New file {newFilePath} does not exist.");
                continue;
            }
            File.Delete(filePath);
            File.Move(newFilePath, filePath);
        }
    }

    protected static string ResolveRecreateDirectory(string directoryPath)
        => Path.Combine(Path.GetDirectoryName(directoryPath) ?? directoryPath,
            NewDirectoryPrefix + Path.GetFileName(directoryPath));

    public delegate bool TagModifier(ChunkPos pos, CompoundTag tag);

    //Builder collects the folder, data fix type and per-version tag modifiers before the upgrader is built
    public sealed class Builder
    {
        private readonly DataFixer.DataFixer _dataFixer;
        private DataFixTypes? _dataFixType;
        private string? _type;
        private string? _folderName;
        private int _defaultVersion = -1;
        private bool _recreateRegionFiles;
        private UpgradeProgress _upgradeProgress = new UpgradeProgress.Noop();
        private CompoundTag? _dataFixContextTag;
        private SortedDictionary<int, TagModifier> _tagModifiers = new();

        public Builder(DataFixer.DataFixer dataFixer) => _dataFixer = dataFixer;

        public Builder SetDataFixType(DataFixTypes dataFixType)
        {
            _dataFixType = dataFixType;
            return this;
        }

        public Builder SetTypeAndFolderName(string name) => SetType(name).SetFolderName(name);

        public Builder SetType(string type)
        {
            _type = type;
            return this;
        }

        public Builder SetFolderName(string folderName)
        {
            _folderName = folderName;
            return this;
        }

        public Builder SetDefaultVersion(int defaultVersion)
        {
            _defaultVersion = defaultVersion;
            return this;
        }

        public Builder SetRecreateRegionFiles(bool recreateRegionFiles)
        {
            _recreateRegionFiles = recreateRegionFiles;
            return this;
        }

        public Builder TrackProgress(UpgradeProgress upgradeProgress)
        {
            _upgradeProgress = upgradeProgress;
            return this;
        }

        public Builder SetDataFixContextTag(CompoundTag? dataFixContextTag)
        {
            _dataFixContextTag = dataFixContextTag;
            return this;
        }

        public Builder AddTagModifier(int version, TagModifier tagModifier)
        {
            if (_tagModifiers.ContainsKey(version))
                throw new InvalidOperationException("Can't add two fixers for the same data version");
            _tagModifiers[version] = tagModifier;
            return this;
        }

        public Builder Copy()
        {
            var copy = new Builder(_dataFixer)
                .SetDataFixType(_dataFixType ?? throw new InvalidOperationException("DataFixType is not set"))
                .SetType(_type ?? throw new InvalidOperationException("Type is not set"))
                .SetFolderName(_folderName ?? throw new InvalidOperationException("FolderName is not set"))
                .SetDefaultVersion(_defaultVersion)
                .SetRecreateRegionFiles(_recreateRegionFiles)
                .TrackProgress(_upgradeProgress)
                .SetDataFixContextTag(_dataFixContextTag);
            copy._tagModifiers = new SortedDictionary<int, TagModifier>(_tagModifiers);
            return copy;
        }

        public RegionStorageUpgrader Build(int previousCopiesFileAmounts)
            => new(_dataFixer,
                _dataFixType ?? throw new InvalidOperationException("DataFixType is not set"),
                _type ?? throw new InvalidOperationException("Type is not set"),
                _folderName ?? throw new InvalidOperationException("FolderName is not set"),
                _defaultVersion, _recreateRegionFiles, _upgradeProgress, previousCopiesFileAmounts,
                _dataFixContextTag, _tagModifiers);
    }
}
