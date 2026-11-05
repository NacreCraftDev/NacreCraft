using NacreCraft.Game.World.Level;
using NacreCraft.Storage;

namespace NacreCraft.Game.Server;

//ServerPlayerGameMode per-player game mode state, maps to vanilla net.minecraft.server.level.ServerPlayerGameMode
//Owns the current and previous game type and the interaction hooks (block breaking, item use)
//Those interaction paths live on the packet handler in NC, so only the mode state is kept here
public class ServerPlayerGameMode(ServerPlayer player)
{
    protected readonly ServerPlayer Player = player;

    protected ServerLevel Level => Player.Level;

    public GameType GameModeForPlayer { get; private set; } = player.GameType;

    public GameType PreviousGameModeForPlayer { get; private set; } = player.GameType;

    public virtual void Tick() { }

    //SetGameModeForPlayer switches the mode and remembers the previous one, maps to vanilla setGameModeForPlayer
    public virtual void SetGameModeForPlayer(GameType gameType, GameType? previousGameType)
    {
        PreviousGameModeForPlayer = previousGameType ?? GameModeForPlayer;
        GameModeForPlayer = gameType;
        Player.GameType = gameType;
    }

    //ChangeGameMode switches only when the mode differs, maps to vanilla changeGameMode
    public virtual bool ChangeGameMode(GameType gameType)
    {
        if (gameType == GameModeForPlayer) return false;
        SetGameModeForPlayer(gameType, GameModeForPlayer);
        return true;
    }

    public bool IsCreative => GameModeForPlayer == GameType.Creative;

    public bool IsSurvival => GameModeForPlayer == GameType.Survival;
}
