using System.Text;
using NacreCraft.Config;

namespace NacreCraft.Storage;

//LevelSummary maps to vanilla net.minecraft.world.level.storage.LevelSummary
//One entry of the world list: settings, version, lock state and the flags the select-world screen reacts to
//Degraded: LevelSettings and GameType live in the Game layer, so the settings are the existing LevelData class and the game
//mode is the numeric id; chat Components are not available in Storage, so info strings are plain English/translation keys
public class LevelSummary : IComparable<LevelSummary>
{
    //DefaultSeparator, the separator vanilla inserts between info parts, maps to ComponentUtils.DEFAULT_SEPARATOR_TEXT
    private const string DefaultSeparator = " • ";

    private readonly LevelData? _settings;
    private readonly LevelVersion? _levelVersion;
    private readonly string _levelId;
    private readonly bool _requiresManualConversion;
    private readonly bool _requiresFileFixing;
    private readonly bool _locked;
    private readonly bool _experimental;
    private readonly string _icon;
    private string? _info;

    public LevelSummary(
        LevelData? settings,
        LevelVersion? levelVersion,
        string levelId,
        bool requiresManualConversion,
        bool requiresFileFixing,
        bool locked,
        bool experimental,
        string icon)
    {
        _settings = settings;
        _levelVersion = levelVersion;
        _levelId = levelId;
        _requiresManualConversion = requiresManualConversion;
        _requiresFileFixing = requiresFileFixing;
        _locked = locked;
        _experimental = experimental;
        _icon = icon;
    }

    public string GetLevelId() => _levelId;

    public virtual string GetLevelName()
        => string.IsNullOrEmpty(_settings?.LevelName) ? _levelId : _settings!.LevelName;

    public string GetIcon() => _icon;

    public bool RequiresManualConversion() => _requiresManualConversion;

    public bool RequiresFileFixing() => _requiresFileFixing;

    public bool IsExperimental() => _experimental;

    public virtual long GetLastPlayed() => _levelVersion?.LastPlayed ?? -1L;

    public int CompareTo(LevelSummary? other)
    {
        if (other is null) return -1;
        if (GetLastPlayed() < other.GetLastPlayed()) return 1;
        if (GetLastPlayed() > other.GetLastPlayed()) return -1;
        return string.CompareOrdinal(_levelId, other._levelId);
    }

    public LevelData? GetSettings() => _settings;

    public int GetGameModeId() => _settings?.GameTypeId ?? -1;

    public bool IsHardcore() => _settings?.Hardcore ?? false;

    public bool HasCommands() => _settings?.AllowCommands ?? false;

    //GetWorldVersionName names the writing version, maps to vanilla getWorldVersionName
    //The vanilla translatable "selectWorld.versionUnknown" is returned as its translation key
    public string GetWorldVersionName()
        => string.IsNullOrEmpty(_levelVersion?.MinecraftVersionName)
            ? "selectWorld.versionUnknown"
            : _levelVersion!.MinecraftVersionName;

    public LevelVersion? GetLevelVersion() => _levelVersion;

    public bool ShouldBackup() => GetBackupStatus() != BackupStatus.None;

    public bool IsDowngrade() => GetBackupStatus() == BackupStatus.Downgrade;

    //GetBackupStatus compares the save version with this build, maps to vanilla backupStatus
    //Degraded: SharedConstants has no stable flag and DataFixers has no file fixer, so UpgradeToSnapshot and a real
    //file fixing probe cannot be derived; the constructor flag stands in for the latter
    public BackupStatus GetBackupStatus()
    {
        if (_requiresFileFixing) return BackupStatus.FileFixingRequired;
        if (_levelVersion is null) return BackupStatus.None;
        if (_levelVersion.MinecraftVersion.Version > SharedConstants.WorldDataVersion) return BackupStatus.Downgrade;
        return BackupStatus.None;
    }

    public bool IsLocked() => _locked;

    public virtual bool IsDisabled() => IsLocked() || RequiresManualConversion() || !IsCompatible();

    public bool IsCompatible()
        => _levelVersion is null || DataVersion.Current.IsCompatible(_levelVersion.MinecraftVersion);

    public virtual string GetInfo() => _info ??= CreateInfo();

    private string CreateInfo()
    {
        if (IsLocked()) return "selectWorld.locked";
        if (RequiresManualConversion()) return "selectWorld.conversion";
        if (!IsCompatible()) return "selectWorld.incompatible.info: " + GetWorldVersionName();
        var builder = new StringBuilder(IsHardcore() ? "gameMode.hardcore" : "gameMode." + GetGameTypeName(GetGameModeId()));
        if (HasCommands()) builder.Append(DefaultSeparator).Append("selectWorld.commands");
        if (IsExperimental()) builder.Append(DefaultSeparator).Append("selectWorld.experimental");
        builder.Append(DefaultSeparator).Append("selectWorld.version").Append(' ').Append(GetWorldVersionName());
        return builder.ToString();
    }

    public virtual string PrimaryActionMessage()
        => _requiresFileFixing ? "selectWorld.upgrade_and_play" : "selectWorld.select";

    public virtual bool PrimaryActionActive() => !IsDisabled();

    public virtual bool CanUpload() => !RequiresManualConversion() && !IsLocked();

    public virtual bool CanEdit() => !IsDisabled() && !RequiresFileFixing();

    public virtual bool CanRecreate() => !IsDisabled() && !RequiresFileFixing();

    public bool CanDelete() => true;

    //GetGameTypeName names the game mode, maps to vanilla GameType.getName
    private static string GetGameTypeName(int gameTypeId) => gameTypeId switch
    {
        0 => "survival",
        1 => "creative",
        2 => "adventure",
        3 => "spectator",
        _ => "unknown",
    };

    //BackupStatus maps to the vanilla nested enum LevelSummary.BackupStatus
    public enum BackupStatus
    {
        None = 0,
        Downgrade = 1,
        UpgradeToSnapshot = 2,
        FileFixingRequired = 3,
    }

    //SymlinkLevelSummary maps to the vanilla nested LevelSummary.SymlinkLevelSummary
    public sealed class SymlinkLevelSummary : LevelSummary
    {
        public SymlinkLevelSummary(string levelId, string icon)
            : base(null, null, levelId, false, false, false, false, icon)
        {
        }

        public override string GetLevelName() => GetLevelId();

        public override string GetInfo() => "symlink_warning.title";

        public override long GetLastPlayed() => -1L;

        public override bool IsDisabled() => false;

        public override string PrimaryActionMessage() => "symlink_warning.more_info";

        public override bool PrimaryActionActive() => true;

        public override bool CanUpload() => false;

        public override bool CanEdit() => false;

        public override bool CanRecreate() => false;
    }

    //CorruptedLevelSummary maps to the vanilla nested LevelSummary.CorruptedLevelSummary
    public sealed class CorruptedLevelSummary : LevelSummary
    {
        private readonly long _lastPlayed;

        public CorruptedLevelSummary(string levelId, string icon, long lastPlayed)
            : base(null, null, levelId, false, false, false, false, icon)
        {
            _lastPlayed = lastPlayed;
        }

        public override string GetLevelName() => GetLevelId();

        public override string GetInfo() => "recover_world.warning";

        public override long GetLastPlayed() => _lastPlayed;

        public override bool IsDisabled() => false;

        public override string PrimaryActionMessage() => "recover_world.button";

        public override bool PrimaryActionActive() => true;

        public override bool CanUpload() => false;

        public override bool CanEdit() => false;

        public override bool CanRecreate() => false;
    }
}
