using NacreCraft.Game.World.Level.Block;

namespace NacreCraft.Game.World.Level.Block.Dispenser;

//OptionalDispenseItemBehavior behavior that may or may not have done something, maps to vanilla net.minecraft.core.dispenser.OptionalDispenseItemBehavior
//A failed attempt plays the fail sound instead of the dispense sound
public abstract class OptionalDispenseItemBehavior : DefaultDispenseItemBehavior
{
    private bool _success = true;

    public bool IsSuccess() => _success;

    public void SetSuccess(bool success) => _success = success;

    //PlaySound plays the dispense or fail sound by the result, maps to vanilla playSound
    protected override void PlaySound(BlockSource source)
        => source.Level.LevelEvent(IsSuccess() ? LevelEvent.SoundDispenserDispense : LevelEvent.SoundDispenserFail,
            source.Pos, 0);
}
