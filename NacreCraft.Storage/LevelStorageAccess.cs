using NacreCraft.DataFixer;
using NacreCraft.Logging;
using NacreCraft.Nbt;
using NacreCraft.Registry;

namespace NacreCraft.Storage;

//Predefined dimension ResourceKey<Level>, maps to vanilla net.minecraft.world.level.Level.OVERWORLD/NETHER/END
public static class LevelKeys
{
    public static readonly ResourceKey<Level> OVERWORLD =
        ResourceKey<Level>.Create(Registries.DIMENSION, Identifier.WithDefaultNamespace("overworld"));

    public static readonly ResourceKey<Level> NETHER =
        ResourceKey<Level>.Create(Registries.DIMENSION, Identifier.WithDefaultNamespace("the_nether"));

    public static readonly ResourceKey<Level> END =
        ResourceKey<Level>.Create(Registries.DIMENSION, Identifier.WithDefaultNamespace("the_end"));
}

//LevelStorageAccess, the storage access entry point for one world
//Maps to vanilla net.minecraft.world.level.storage.LevelStorageSource.LevelStorageAccess
//Holds the worldDir and DirectoryLock, providing per-dimension path lookup and SimpleRegionStorage creation
//DirectoryLock prevents multiple processes opening the same world
public sealed class LevelStorageAccess : IDisposable
{
    private readonly string _worldDir;
    private readonly string _worldName;
    //One SimpleRegionStorage per dimension, cached to avoid recreating
    private readonly Dictionary<ResourceKey<Level>, SimpleRegionStorage> _dimensionStorages = new();
    //DirectoryLock exclusively holds session.lock, preventing multiple processes operating the same world
    private DirectoryLock? _directoryLock;
    private bool _disposed;

    public string WorldName => _worldName;
    public string WorldDir => _worldDir;

    //LevelId the world folder name, maps to vanilla LevelStorageAccess.getLevelId
    public string LevelId => _worldName;

    //LevelDataPath, the world metadata file path, matches vanilla level.dat
    public string LevelDataPath => Path.Combine(_worldDir, "level.dat");

    //LevelDataOldPath, the backup file path
    public string LevelDataOldPath => Path.Combine(_worldDir, "level.dat_old");

    //HasLock reports whether the directory lock is held; false in no-lock scenarios
    public bool HasLock => _directoryLock is not null;

    internal LevelStorageAccess(string worldDir, string worldName, bool acquireLock = true)
    {
        _worldDir = worldDir;
        _worldName = worldName;
        if (acquireLock)
            _directoryLock = DirectoryLock.Acquire(worldDir);
    }

    //GetDimensionPath gets the dimension data directory path
    //Every dimension, the overworld included, sits under dimensions/<namespace>/<path>; vanilla outlines the folder this
    //way in DimensionType.getStorageFolder so a save keeps all dimensions side by side instead of at the world root
    public string GetDimensionPath(ResourceKey<Level> levelKey)
    {
        var result = Path.Combine(_worldDir, "dimensions", levelKey.Identifier.Namespace, levelKey.Identifier.Path);
        Log.Debug($"GetDimensionPath exit result={result}");
        return result;
    }

    //GetRegionPath gets the region directory path under the dimension
    public string GetRegionPath(ResourceKey<Level> levelKey)
        => Path.Combine(GetDimensionPath(levelKey), "region");

    //CreateRegionStorage creates or reuses the SimpleRegionStorage for the given dimension
    //fixer, the DataFixer instance used to upgrade older chunks
    //dataFixType, the DataFixTypes identifying the upgrade kind
    public SimpleRegionStorage CreateRegionStorage(
        ResourceKey<Level> levelKey,
        NacreCraft.DataFixer.DataFixer fixer,
        DataFixTypes dataFixType,
        bool syncWrites = true)
    {
        Log.Debug($"CreateRegionStorage entry levelKey={levelKey} fixer={fixer} dataFixType={dataFixType} syncWrites={syncWrites}");
        if (_dimensionStorages.TryGetValue(levelKey, out var existing))
        {
            Log.Debug($"CreateRegionStorage exit result={existing}");
            return existing;
        }

        var regionDir = GetRegionPath(levelKey);
        Directory.CreateDirectory(regionDir);
        var info = new RegionStorageInfo(_worldName, levelKey, "chunk");
        var storage = new SimpleRegionStorage(info, regionDir, fixer, syncWrites, dataFixType);
        _dimensionStorages[levelKey] = storage;
        Log.Debug($"CreateRegionStorage exit result={storage}");
        return storage;
    }

    //GetExistingRegionStorage gets an already-created dimension storage, or null when absent
    public SimpleRegionStorage? GetExistingRegionStorage(ResourceKey<Level> levelKey)
        => _dimensionStorages.TryGetValue(levelKey, out var storage) ? storage : null;

    //GetUnfixedDataTag reads the Data compound of level.dat, maps to vanilla getUnfixedDataTag
    //Vanilla returns a Dynamic; the NBT layer here works on CompoundTag directly
    public CompoundTag GetUnfixedDataTag()
    {
        var tag = NbtIo.ReadCompressed(LevelDataPath, NbtAccounter.UnlimitedHeap());
        return tag.GetCompound("Data") ?? new CompoundTag();
    }

    //ReleaseTemporarilyAndRun drops the session lock around an action, maps to vanilla releaseTemporarilyAndRun
    //The upgrade renames the world folder, which Windows refuses while session.lock is still open
    public void ReleaseTemporarilyAndRun(System.Action action)
    {
        _directoryLock?.Dispose();
        _directoryLock = null;
        try
        {
            action();
        }
        finally
        {
            if (!_disposed) _directoryLock = DirectoryLock.Acquire(_worldDir);
        }
    }

    public void Dispose()
    {
        //Log.Debug($"Dispose entry");
        if (_disposed)
        {
            //Log.Debug($"Dispose exit");
            return;
        }
        foreach (var storage in _dimensionStorages.Values)
            storage.Dispose();
        _dimensionStorages.Clear();
        _directoryLock?.Dispose();
        _disposed = true;
        //Log.Debug($"Dispose exit");
    }
}
