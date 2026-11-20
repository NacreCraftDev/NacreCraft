using NacreCraft.Game.Server;
using NacreCraft.Util;

namespace NacreCraft.Game.Server.Players;

//SleepStatus counts how many players are sleeping and decides whether the night may be skipped, maps to vanilla net.minecraft.server.players.SleepStatus
//Only non-spectator players count as active; the needed count is a percentage of the active players, at least one
public sealed class SleepStatus
{
    private int _activePlayers;
    private int _sleepingPlayers;

    //AreEnoughSleeping whether enough players are currently sleeping, maps to vanilla areEnoughSleeping
    public bool AreEnoughSleeping(int sleepPercentageNeeded)
        => _sleepingPlayers >= SleepersNeeded(sleepPercentageNeeded);

    //AreEnoughDeepSleeping whether enough players have slept long enough, maps to vanilla areEnoughDeepSleeping
    public bool AreEnoughDeepSleeping(int sleepPercentageNeeded, IReadOnlyList<ServerPlayer> players)
    {
        var deepSleepers = players.Count(player => player.IsSleepingLongEnough);
        return deepSleepers >= SleepersNeeded(sleepPercentageNeeded);
    }

    //SleepersNeeded the number of sleepers required for the given percentage, maps to vanilla sleepersNeeded
    public int SleepersNeeded(int sleepPercentageNeeded)
        => Math.Max(1, Mth.Ceil(_activePlayers * sleepPercentageNeeded / 100.0f));

    //RemoveAllSleepers forgets the sleeping count, maps to vanilla removeAllSleepers
    public void RemoveAllSleepers() => _sleepingPlayers = 0;

    //AmountSleeping the current sleeping count
    public int AmountSleeping() => _sleepingPlayers;

    //Update recounts active and sleeping players, maps to vanilla update
    //Returns whether the counts changed while anybody was sleeping, so the caller only broadcasts when the answer can flip
    public bool Update(IReadOnlyList<ServerPlayer> players)
    {
        var oldActivePlayers = _activePlayers;
        var oldSleepingPlayers = _sleepingPlayers;
        _activePlayers = 0;
        _sleepingPlayers = 0;
        foreach (var player in players)
        {
            if (player.IsSpectator()) continue;
            _activePlayers++;
            if (player.IsSleeping) _sleepingPlayers++;
        }
        return (oldSleepingPlayers > 0 || _sleepingPlayers > 0)
               && (oldActivePlayers != _activePlayers || oldSleepingPlayers != _sleepingPlayers);
    }
}
