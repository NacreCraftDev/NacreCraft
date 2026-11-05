namespace NacreCraft.Storage;

//WritableLevelData maps to vanilla net.minecraft.world.level.storage.WritableLevelData
//Degraded: the vanilla LevelData interface is represented by the existing LevelData class in this assembly, so this
//interface declares the read accessors itself instead of extending a LevelData interface
public interface WritableLevelData
{
    RespawnData GetRespawnData();

    long GetGameTime();

    bool IsHardcore();

    //GetDifficultyName, the difficulty key, maps to vanilla getDifficulty; Difficulty lives in the Game layer
    string GetDifficultyName();

    bool IsDifficultyLocked();

    void SetSpawn(RespawnData respawnData);
}
