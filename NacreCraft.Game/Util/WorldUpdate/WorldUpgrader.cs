using NacreCraft.Config;
using NacreCraft.DataFixer;
using NacreCraft.Logging;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using GameLevelStem = NacreCraft.Game.World.Level.LevelGen.Dimension.LevelStem;
using LevelStem = NacreCraft.Registry.LevelStem;

namespace NacreCraft.Game.Util.WorldUpdate;

//WorldUpgrader runs the optimize-world pass over every dimension, maps to vanilla net.minecraft.util.worldupdate.WorldUpgrader
//The work runs on a background thread; the client polls the progress object for the stage text and percentage
public sealed class WorldUpgrader : IDisposable
{
    private readonly UpgradeStatusTranslator _statusTranslator = new();
    private readonly Registry<LevelStem> _dimensions;
    private readonly HashSet<ResourceKey<Level>> _levels;
    private readonly bool _eraseCache;
    private readonly bool _recreateRegionFiles;
    private readonly LevelStorageAccess _levelStorage;
    private readonly Thread _thread;
    private readonly NacreCraft.DataFixer.DataFixer _dataFixer;
    private readonly UpgradeProgress _upgradeProgress = new();
    private readonly SavedDataStorage _overworldSavedDataStorage;

    public WorldUpgrader(
        LevelStorageAccess levelSource,
        NacreCraft.DataFixer.DataFixer dataFixer,
        RegistryAccess registryAccess,
        bool eraseCache,
        bool recreateRegionFiles)
    {
        _dimensions = registryAccess.LookupOrThrow(Registries.LEVEL_STEM);
        _levels = _dimensions.RegistryKeySet.Select(Registries.LevelStemToLevel).ToHashSet();
        _eraseCache = eraseCache;
        _dataFixer = dataFixer;
        _levelStorage = levelSource;
        _recreateRegionFiles = recreateRegionFiles;
        _overworldSavedDataStorage = new SavedDataStorage(
            Path.Combine(levelSource.GetDimensionPath(LevelKeys.OVERWORLD), "data"), dataFixer);
        _overworldSavedDataStorage.SetRegistryAccess(registryAccess);
        _thread = new Thread(Work)
        {
            IsBackground = true,
            Name = "World Upgrader"
        };
        _thread.Start();
    }

    public static CompoundTag GetDataFixContextTag(Registry<LevelStem> dimensions, ResourceKey<Level> dimension)
    {
        var stem = dimensions.GetValueOrThrow(Registries.LevelToLevelStem(dimension));
        var generator = (stem as GameLevelStem)?.Generator;
        return GetChunkDataFixContextTag(dimension, generator?.GetTypeNameForDataFixer());
    }

    //GetChunkDataFixContextTag writes the dimension id and, when known, the generator id, maps to ChunkMap.getChunkDataFixContextTag
    public static CompoundTag GetChunkDataFixContextTag(ResourceKey<Level> dimension, Identifier? generatorIdentifier)
    {
        var contextTag = new CompoundTag();
        contextTag.PutString("dimension", dimension.Identifier.ToString());
        if (generatorIdentifier is { } id) contextTag.PutString("generator", id.ToString());
        return contextTag;
    }

    public static bool VerifyChunkPosAndEraseCache(ChunkPos pos, CompoundTag upgradedTag)
    {
        VerifyChunkPos(pos, upgradedTag);
        var changed = upgradedTag.Contains("Heightmaps");
        upgradedTag.Remove("Heightmaps");
        changed |= upgradedTag.Contains("isLightOn");
        upgradedTag.Remove("isLightOn");

        var sections = upgradedTag.GetListOrEmpty("sections");
        for (var i = 0; i < sections.Count; i++)
        {
            if (sections.GetCompound(i) is not { } section) continue;
            changed |= section.Contains("BlockLight");
            section.Remove("BlockLight");
            changed |= section.Contains("SkyLight");
            section.Remove("SkyLight");
        }
        return changed;
    }

    public static bool VerifyChunkPos(ChunkPos pos, CompoundTag upgradedTag)
    {
        var storedPos = new ChunkPos(upgradedTag.GetIntOr("xPos", 0), upgradedTag.GetIntOr("zPos", 0));
        if (!storedPos.Equals(pos))
            Log.Warning($"Chunk {pos.X},{pos.Z} has invalid position {storedPos.X},{storedPos.Z}");
        return false;
    }

    public void Cancel()
    {
        _upgradeProgress.SetCanceled();
        _thread.Join();
    }

    public bool IsFinished() => _upgradeProgress.IsFinished();

    public IReadOnlySet<ResourceKey<Level>> Levels() => _levels;

    public float DimensionProgress(ResourceKey<Level> dimension) => _upgradeProgress.GetDimensionProgress(dimension);

    public float GetTotalProgress() => _upgradeProgress.GetTotalProgress();

    public int GetTotalChunks() => _upgradeProgress.GetTotalChunks();

    public int GetConverted() => _upgradeProgress.GetConverted();

    public int GetSkipped() => _upgradeProgress.GetSkipped();

    public NacreCraft.Network.Chat.Component GetStatus() => _statusTranslator.Translate(_upgradeProgress);

    public void Dispose() => _overworldSavedDataStorage.Dispose();

    private void Work()
    {
        try
        {
            var conversionTime = Environment.TickCount64;
            var currentVersion = SharedConstants.WorldDataVersion;

            Log.Info("Upgrading entities");
            UpgradeLevels(DataFixTypes.EntityChunk, new RegionStorageUpgrader.Builder(_dataFixer)
                .SetTypeAndFolderName("entities")
                .SetRecreateRegionFiles(_recreateRegionFiles)
                .TrackProgress(_upgradeProgress));

            Log.Info("Upgrading POIs");
            UpgradeLevels(DataFixTypes.PoiChunk, new RegionStorageUpgrader.Builder(_dataFixer)
                .SetTypeAndFolderName("poi")
                .SetDefaultVersion(1945)
                .SetRecreateRegionFiles(_recreateRegionFiles)
                .TrackProgress(_upgradeProgress));

            Log.Info("Upgrading blocks");
            UpgradeLevels(DataFixTypes.Chunk, new RegionStorageUpgrader.Builder(_dataFixer)
                    .SetType("chunk")
                    .SetFolderName("region")
                    .SetRecreateRegionFiles(_recreateRegionFiles)
                    .TrackProgress(_upgradeProgress),
                (levelSpecificBuilder, level) => levelSpecificBuilder
                    .SetDataFixContextTag(GetDataFixContextTag(_dimensions, level))
                    .AddTagModifier(currentVersion,
                        _eraseCache ? VerifyChunkPosAndEraseCache : VerifyChunkPos));

            _overworldSavedDataStorage.SaveAndJoin();
            conversionTime = Environment.TickCount64 - conversionTime;
            Log.Info($"World optimization finished after {conversionTime / 1000} seconds");
            _upgradeProgress.SetFinished(true);
        }
        catch (Exception e)
        {
            Log.Error($"Error upgrading world {e}");
            _upgradeProgress.SetStatus(UpgradeProgress.Status.FAILED);
            _upgradeProgress.SetFinished(true);
        }
    }

    private void UpgradeLevels(DataFixTypes dataFixType, RegionStorageUpgrader.Builder builder)
        => UpgradeLevels(dataFixType, builder, (levelSpecificBuilder, _) => levelSpecificBuilder);

    private void UpgradeLevels(
        DataFixTypes dataFixType,
        RegionStorageUpgrader.Builder builder,
        Func<RegionStorageUpgrader.Builder, ResourceKey<Level>, RegionStorageUpgrader.Builder> levelSpecificBuilder)
    {
        var upgraders = new List<RegionStorageUpgrader>();
        _upgradeProgress.Reset(dataFixType);
        _upgradeProgress.SetType(UpgradeProgress.Type.REGIONS);
        builder.SetDataFixType(dataFixType);

        var previousCopiesFileAmounts = 0;
        foreach (var level in _levels)
        {
            var upgrader = levelSpecificBuilder(builder.Copy(), level).Build(previousCopiesFileAmounts);
            upgrader.Init(level, _levelStorage);
            previousCopiesFileAmounts += upgrader.FileAmount();
            upgraders.Add(upgrader);
        }

        foreach (var upgrader in upgraders) upgrader.Upgrade();
    }
}
