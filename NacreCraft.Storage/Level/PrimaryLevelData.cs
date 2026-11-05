using NacreCraft.Codec;
using NacreCraft.Config;
using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Storage.Chunk;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//PrimaryLevelData maps to vanilla net.minecraft.world.level.storage.PrimaryLevelData
//The authoritative level.dat content: settings, spawn, game time, brands and the save format version
//Degraded: LevelSettings and GameType live in the Game layer, so the settings are carried by the existing LevelData class
//and the game mode as its numeric id. The Dynamic overloads of writeVersionTag/writeLastPlayed are omitted because level.dat
//is written through CompoundTag in this project
public class PrimaryLevelData : ServerLevelData, WorldData
{
    //LevelNameTag, the level name field, maps to vanilla LEVEL_NAME
    public const string LevelNameTag = "LevelName";

    //SinglePlayerUuidTag, the single player uuid field, maps to vanilla SINGLEPLAYER_UUID
    public const string SinglePlayerUuidTag = "singleplayer_uuid";

    private LevelData _settings;
    private readonly SpecialWorldProperty _specialWorldProperty;
    private readonly Lifecycle _worldGenSettingsLifecycle;
    private RespawnData _respawnData;
    private long _gameTime;
    private readonly Guid? _singlePlayerUuid;
    private readonly int _version;
    private bool _initialized;
    private readonly HashSet<string> _knownServerBrands;
    private bool _wasModded;
    private readonly HashSet<string> _removedFeatureFlags;

    //SpecialWorldProperty, maps to the vanilla nested enum PrimaryLevelData.SpecialWorldProperty
    public enum SpecialWorldProperty
    {
        None = 0,
        Flat = 1,
        Debug = 2,
    }

    public PrimaryLevelData(LevelData settings, SpecialWorldProperty specialWorldProperty, Lifecycle worldGenSettingsLifecycle)
        : this(
            null,
            false,
            RespawnData.Of(RespawnData.OverworldDimension, settings.SpawnX, settings.SpawnY, settings.SpawnZ, settings.SpawnYaw, settings.SpawnPitch),
            0L,
            WorldData.AnvilVersionId,
            false,
            new HashSet<string>(),
            new HashSet<string>(),
            settings,
            specialWorldProperty,
            worldGenSettingsLifecycle)
    {
    }

    private PrimaryLevelData(
        Guid? singlePlayerUuid,
        bool wasModded,
        RespawnData respawnData,
        long gameTime,
        int version,
        bool initialized,
        HashSet<string> knownServerBrands,
        HashSet<string> removedFeatureFlags,
        LevelData settings,
        SpecialWorldProperty specialWorldProperty,
        Lifecycle worldGenSettingsLifecycle)
    {
        _singlePlayerUuid = singlePlayerUuid;
        _wasModded = wasModded;
        _respawnData = respawnData;
        _gameTime = gameTime;
        _version = version;
        _initialized = initialized;
        _knownServerBrands = knownServerBrands;
        _removedFeatureFlags = removedFeatureFlags;
        _settings = settings;
        _specialWorldProperty = specialWorldProperty;
        _worldGenSettingsLifecycle = worldGenSettingsLifecycle;
        ApplyRespawnToSettings();
    }

    //Parse reads a level.dat Data layer, maps to vanilla PrimaryLevelData.parse
    //Degraded: the single player uuid is stored as a string here, vanilla uses UUIDUtil.CODEC and writes an int array
    public static PrimaryLevelData Parse(
        Dynamic<Tag> input,
        LevelData settings,
        SpecialWorldProperty specialWorldProperty,
        Lifecycle worldGenSettingsLifecycle)
    {
        var gameTime = input.Get("Time").AsLong(0L);
        var levelVersion = LevelVersion.Parse(input);
        var uuidText = input.Get(SinglePlayerUuidTag).AsString(string.Empty);
        Guid? singlePlayerUuid = Guid.TryParse(uuidText, out var parsedUuid) ? parsedUuid : null;
        var wasModded = input.Get("WasModded").AsBoolean(false);
        var respawnData = ParseRespawnData(input.Value as CompoundTag);
        var initialized = input.Get("initialized").AsBoolean(true);
        var brands = input.Get("ServerBrands").AsStreamOpt()
            .Select(entry => entry.AsString(string.Empty))
            .Where(brand => brand.Length > 0);
        var removedFlags = input.Get("removed_features").AsStreamOpt()
            .Select(entry => entry.AsString(string.Empty))
            .Where(flag => flag.Length > 0);
        return new PrimaryLevelData(
            singlePlayerUuid,
            wasModded,
            respawnData,
            gameTime,
            levelVersion.LevelDataVersion,
            initialized,
            new HashSet<string>(brands),
            new HashSet<string>(removedFlags),
            settings,
            specialWorldProperty,
            worldGenSettingsLifecycle);
    }

    //CreateTag builds the level.dat Data layer, maps to vanilla createTag/setTagData
    public CompoundTag CreateTag(Guid? singlePlayerUuid)
    {
        var tag = _settings.CreateTag();
        tag.Put("ServerBrands", StringCollectionToTag(_knownServerBrands));
        tag.PutBoolean("WasModded", _wasModded);
        if (_removedFeatureFlags.Count > 0)
            tag.Put("removed_features", StringCollectionToTag(_removedFeatureFlags));
        WriteVersionTag(tag);
        NbtUtils.AddCurrentDataVersion(tag);
        tag.PutBoolean("initialized", _initialized);
        tag.PutLong("Time", _gameTime);
        WriteLastPlayed(tag);
        var uuid = singlePlayerUuid ?? _singlePlayerUuid;
        if (uuid is not null)
            tag.PutString(SinglePlayerUuidTag, uuid.Value.ToString());
        return tag;
    }

    //WriteLastPlayed stamps the current epoch milliseconds, maps to vanilla writeLastPlayed
    public static void WriteLastPlayed(CompoundTag tag)
        => tag.PutLong("LastPlayed", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    //WriteVersionTag writes the Version block, maps to vanilla writeVersionTag
    public static void WriteVersionTag(CompoundTag tag)
    {
        var version = new CompoundTag();
        version.PutString("Name", SharedConstants.Version);
        version.PutInt("Id", SharedConstants.WorldDataVersion);
        version.PutBoolean("Snapshot", false);
        version.PutString("Series", DataVersion.MainSeries);
        tag.Put("Version", version);
    }

    public RespawnData GetRespawnData() => _respawnData;

    public long GetGameTime() => _gameTime;

    public Guid? GetSinglePlayerUuid() => _singlePlayerUuid;

    public void SetGameTime(long time) => _gameTime = time;

    public void SetSpawn(RespawnData respawnData)
    {
        _respawnData = respawnData;
        ApplyRespawnToSettings();
    }

    public string GetLevelName() => _settings.LevelName;

    public int GetVersion() => _version;

    public int GetGameTypeId() => _settings.GameTypeId;

    public void SetGameTypeId(int gameTypeId) => _settings.GameTypeId = gameTypeId;

    public bool IsHardcore() => _settings.Hardcore;

    public bool IsAllowCommands() => _settings.AllowCommands;

    public void SetAllowCommands(bool allowCommands) => _settings.AllowCommands = allowCommands;

    public bool IsInitialized() => _initialized;

    public void SetInitialized(bool initialized)
    {
        _initialized = initialized;
        _settings.Initialized = initialized;
    }

    public string GetDifficultyName() => _settings.DifficultyName;

    public void SetDifficultyName(string difficultyName) => _settings.DifficultyName = difficultyName;

    public bool IsDifficultyLocked() => _settings.DifficultyLocked;

    public void SetDifficultyLocked(bool difficultyLocked) => _settings.DifficultyLocked = difficultyLocked;

    public bool IsFlatWorld() => _specialWorldProperty == SpecialWorldProperty.Flat;

    public bool IsDebugWorld() => _specialWorldProperty == SpecialWorldProperty.Debug;

    public Lifecycle WorldGenSettingsLifecycle() => _worldGenSettingsLifecycle;

    public void SetModdedInfo(string serverBrand, bool isModded)
    {
        _knownServerBrands.Add(serverBrand);
        _wasModded |= isModded;
    }

    public bool WasModded() => _wasModded;

    public IReadOnlySet<string> GetKnownServerBrands() => new HashSet<string>(_knownServerBrands);

    public IReadOnlySet<string> GetRemovedFeatureFlags() => new HashSet<string>(_removedFeatureFlags);

    public ServerLevelData OverworldData() => this;

    public LevelData GetLevelSettings() => _settings;

    public void FillCrashReportCategory(CrashReportCategory category)
    {
        category.SetDetail("Known server brands", () => string.Join(", ", _knownServerBrands));
        category.SetDetail("Removed feature flags", () => string.Join(", ", _removedFeatureFlags));
        category.SetDetail("Level was modded", () => _wasModded.ToString());
        category.SetDetail("Level storage version", () => $"0x{_version:X5} - {((WorldData)this).GetStorageVersionName(_version)}");
    }

    public void FillCrashReportCategory(CrashReportCategory category, LevelHeightAccessor levelHeightAccessor)
    {
        category.SetDetail("Level spawn location", () => $"({_respawnData.X}, {_respawnData.Y}, {_respawnData.Z}) in {_respawnData.Dimension}");
        category.SetDetail("Level name", GetLevelName);
        category.SetDetail("Level game mode", () =>
            $"Game mode id: {GetGameTypeId()}. Hardcore: {IsHardcore()}. Commands: {IsAllowCommands()}");
        FillCrashReportCategory(category);
    }

    //ApplyRespawnToSettings keeps the spawn fields of the settings object in step with the authoritative respawn data
    private void ApplyRespawnToSettings()
    {
        _settings.SpawnX = _respawnData.X;
        _settings.SpawnY = _respawnData.Y;
        _settings.SpawnZ = _respawnData.Z;
        _settings.SpawnYaw = _respawnData.Yaw;
        _settings.SpawnPitch = _respawnData.Pitch;
    }

    //ParseRespawnData reads the spawn block, maps to vanilla reads through LevelData.RespawnData.CODEC
    private static RespawnData ParseRespawnData(CompoundTag? root)
    {
        if (root is null) return RespawnData.Default;
        var spawn = root.GetCompoundOrEmpty("spawn");
        var position = spawn.GetIntArray("pos");
        if (position is null || position.Value.Length != 3) return RespawnData.Default;
        var dimension = spawn.GetStringValue("dimension");
        if (dimension.Length == 0) dimension = RespawnData.OverworldDimension;
        return RespawnData.Of(
            dimension,
            position.Value[0],
            position.Value[1],
            position.Value[2],
            spawn.GetFloatValue("yaw"),
            spawn.GetFloatValue("pitch"));
    }

    private static ListTag StringCollectionToTag(IEnumerable<string> values)
    {
        var list = new ListTag();
        foreach (var value in values)
            list.Add(new StringTag(value));
        return list;
    }
}
