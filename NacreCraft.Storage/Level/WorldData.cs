using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//WorldData maps to vanilla net.minecraft.world.level.storage.WorldData
//Holds everything level.dat stores about the world: brands, game rules source settings, spawn, difficulty and the save version
//Degraded: WorldDataConfiguration and FeatureFlagSet live in the Game layer, so getDataConfiguration/setDataConfiguration and
//enabledFeatures are not declared here; the level settings are carried by the existing LevelData class
public interface WorldData
{
    //AnvilVersionId, the Anvil save format id, maps to vanilla ANVIL_VERSION_ID
    public const int AnvilVersionId = 19133;

    //McRegionVersionId, the McRegion save format id, maps to vanilla MCREGION_VERSION_ID
    public const int McRegionVersionId = 19132;

    bool WasModded();

    IReadOnlySet<string> GetKnownServerBrands();

    IReadOnlySet<string> GetRemovedFeatureFlags();

    void SetModdedInfo(string serverBrand, bool isModded);

    ServerLevelData OverworldData();

    //GetLevelSettings, maps to vanilla getLevelSettings
    //Degraded: LevelData has no copy() yet, so the live instance is returned instead of a defensive copy
    LevelData GetLevelSettings();

    CompoundTag CreateTag(Guid? singlePlayerUuid);

    bool IsHardcore();

    int GetVersion();

    string GetLevelName();

    int GetGameTypeId();

    void SetGameTypeId(int gameTypeId);

    bool IsAllowCommands();

    void SetAllowCommands(bool allowCommands);

    string GetDifficultyName();

    void SetDifficultyName(string difficultyName);

    bool IsDifficultyLocked();

    void SetDifficultyLocked(bool difficultyLocked);

    Guid? GetSinglePlayerUuid();

    bool IsFlatWorld();

    bool IsDebugWorld();

    Lifecycle WorldGenSettingsLifecycle();

    //GetStorageVersionName names the save format, maps to vanilla getStorageVersionName
    string GetStorageVersionName(int version) => version switch
    {
        McRegionVersionId => "McRegion",
        AnvilVersionId => "Anvil",
        _ => "Unknown?",
    };

    //FillCrashReportCategory adds the world details, maps to vanilla fillCrashReportCategory
    void FillCrashReportCategory(CrashReportCategory category)
    {
        category.SetDetail("Known server brands", () => string.Join(", ", GetKnownServerBrands()));
        category.SetDetail("Removed feature flags", () => string.Join(", ", GetRemovedFeatureFlags()));
        category.SetDetail("Level was modded", () => WasModded().ToString());
        category.SetDetail("Level storage version", () =>
        {
            var version = GetVersion();
            return $"0x{version:X5} - {GetStorageVersionName(version)}";
        });
    }
}
