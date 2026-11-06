namespace NacreCraft.Game.World.Level.Block;

//LevelEvent world event ids, maps to vanilla net.minecraft.world.level.block.LevelEvent
//Only the ids the block behaviors currently send are listed; the rest are added as their senders land
public class LevelEvent
{
    //SoundDispenserDispense item shot out of a dispenser, maps to vanilla SOUND_DISPENSER_DISPENSE
    public const int SoundDispenserDispense = 1000;

    //SoundDispenserFail the dispenser had nothing it could do, maps to vanilla SOUND_DISPENSER_FAIL
    public const int SoundDispenserFail = 1001;

    //SoundDispenserLaunch a projectile left the dispenser, maps to vanilla SOUND_DISPENSER_LAUNCH
    public const int SoundDispenserLaunch = 1002;
}
