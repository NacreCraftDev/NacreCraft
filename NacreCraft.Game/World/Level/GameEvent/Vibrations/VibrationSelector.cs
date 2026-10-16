using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.GameEvents.Vibrations;

//VibrationSelector picks which of the vibrations arriving in the same tick is worth delivering
//maps to vanilla net.minecraft.world.level.gameevent.vibrations.VibrationSelector
//Only one per tick survives: the nearest wins, and at equal distance the higher frequency wins
//A later tick never replaces an earlier one, so the held candidate is always the earliest still undelivered
public sealed class VibrationSelector
{
    private VibrationEvent? _currentVibrationData;

    public VibrationSelector()
    {
    }

    public VibrationSelector(VibrationInfo? currentVibration, long tick)
        => _currentVibrationData = currentVibration is null ? null : new VibrationEvent(currentVibration, tick);

    //CurrentVibration the candidate held right now, null when nothing is waiting
    public VibrationInfo? CurrentVibration => _currentVibrationData?.Event;

    //AddCandidate offers one vibration, maps to vanilla addCandidate
    public void AddCandidate(VibrationInfo vibration, long tickTime)
    {
        if (ShouldReplaceVibration(vibration, tickTime))
            _currentVibrationData = new VibrationEvent(vibration, tickTime);
    }

    //ShouldReplaceVibration decides whether the newcomer beats the one held, maps to vanilla shouldReplaceVibration
    private bool ShouldReplaceVibration(VibrationInfo vibration, long tickTime)
    {
        if (_currentVibrationData is not { } previous) return true;
        if (tickTime != previous.Tick) return false;
        if (vibration.Distance < previous.Event.Distance) return true;
        if (vibration.Distance > previous.Event.Distance) return false;
        return VibrationSystem.GetGameEventFrequency(vibration.GameEvent)
            > VibrationSystem.GetGameEventFrequency(previous.Event.GameEvent);
    }

    //ChosenCandidate takes the winner once its tick has passed, maps to vanilla chosenCandidate
    public VibrationInfo? ChosenCandidate(long time)
        => _currentVibrationData is { } data && data.Tick < time ? data.Event : null;

    //StartOver clears the selection, called once a vibration was scheduled, maps to vanilla startOver
    public void StartOver() => _currentVibrationData = null;

    //VibrationEvent a candidate together with the tick it arrived in, maps to vanilla VibrationSelector.VibrationEvent
    private sealed record VibrationEvent(VibrationInfo Event, long Tick);
}
