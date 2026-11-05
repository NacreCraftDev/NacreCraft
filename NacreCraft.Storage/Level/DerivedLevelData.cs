using NacreCraft.Storage.Chunk;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//DerivedLevelData maps to vanilla net.minecraft.world.level.storage.DerivedLevelData
//Wraps a per-dimension ServerLevelData and takes the shared world settings from WorldData
public class DerivedLevelData : ServerLevelData
{
    private readonly WorldData _worldData;
    private readonly ServerLevelData _wrapped;

    public DerivedLevelData(WorldData worldData, ServerLevelData wrapped)
    {
        _worldData = worldData;
        _wrapped = wrapped;
    }

    public RespawnData GetRespawnData() => _wrapped.GetRespawnData();

    public long GetGameTime() => _wrapped.GetGameTime();

    public string GetLevelName() => _worldData.GetLevelName();

    public int GetGameTypeId() => _worldData.GetGameTypeId();

    public void SetGameTime(long time)
    {
        //Derived data has no own clock; the overworld data owns it
    }

    public void SetSpawn(RespawnData respawnData) => _wrapped.SetSpawn(respawnData);

    public void SetGameTypeId(int gameTypeId)
    {
        //The game mode is stored in WorldData, not in the derived view
    }

    public bool IsHardcore() => _worldData.IsHardcore();

    public bool IsAllowCommands() => _worldData.IsAllowCommands();

    public void SetAllowCommands(bool allowCommands)
    {
        //The command flag is stored in WorldData, not in the derived view
    }

    public bool IsInitialized() => _wrapped.IsInitialized();

    public void SetInitialized(bool initialized)
    {
        //Initialized is owned by the overworld data
    }

    public string GetDifficultyName() => _worldData.GetDifficultyName();

    public bool IsDifficultyLocked() => _worldData.IsDifficultyLocked();

    public void FillCrashReportCategory(CrashReportCategory category, LevelHeightAccessor levelHeightAccessor)
    {
        category.SetDetail("Derived", true);
        _wrapped.FillCrashReportCategory(category, levelHeightAccessor);
    }
}
