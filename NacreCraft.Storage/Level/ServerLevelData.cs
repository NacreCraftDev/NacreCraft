using NacreCraft.Storage.Chunk;
using NacreCraft.Util;

namespace NacreCraft.Storage;

//ServerLevelData maps to vanilla net.minecraft.world.level.storage.ServerLevelData
//Degraded: GameType lives in the Game layer, so the game mode is carried as the vanilla numeric id; the difficulty as its key
public interface ServerLevelData : WritableLevelData
{
    string GetLevelName();

    int GetGameTypeId();

    bool IsInitialized();

    void SetInitialized(bool initialized);

    bool IsAllowCommands();

    void SetAllowCommands(bool allowCommands);

    void SetGameTypeId(int gameTypeId);

    void SetGameTime(long time);

    //FillCrashReportCategory adds the level details, maps to the vanilla override of LevelData.fillCrashReportCategory
    void FillCrashReportCategory(CrashReportCategory category, LevelHeightAccessor levelHeightAccessor)
    {
        category.SetDetail("Level name", GetLevelName);
        category.SetDetail("Level game mode", () =>
            $"Game mode id: {GetGameTypeId()}. Hardcore: {IsHardcore()}. Commands: {IsAllowCommands()}");
    }
}
