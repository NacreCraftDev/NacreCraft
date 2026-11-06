using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block.Dispenser;

//BoatDispenseItemBehavior places a boat on water in front, maps to vanilla net.minecraft.core.dispenser.BoatDispenseItemBehavior
//The entity type is the generic registry type; boat entity classes themselves are not ported yet, so registration comes later
public class BoatDispenseItemBehavior : DefaultDispenseItemBehavior
{
    private readonly DefaultDispenseItemBehavior _defaultDispenseItemBehavior = new();
    private readonly EntityType<object> _type;

    public BoatDispenseItemBehavior(EntityType<object> type) => _type = type;

    protected override ItemStack Execute(BlockSource source, ItemStack dispensed)
    {
        var direction = FacingOf(source.State).ToPrimitive();
        var level = source.Level;
        var center = source.Center;
        var justOutsideDispenser = 0.5625 + _type.Width / 2.0;
        var spawnX = center.X + direction.StepX * justOutsideDispenser;
        var spawnY = center.Y + direction.StepY * 1.125;
        var spawnZ = center.Z + direction.StepZ * justOutsideDispenser;
        var frontPos = source.Pos.Offset(direction);
        double yOffset;
        if (level.GetFluidState(frontPos).IsWater)
        {
            yOffset = 1.0;
        }
        else if (level.GetBlockState(frontPos) is not { } frontState || frontState.Owner.IsAir)
        {
            if (!level.GetFluidState(frontPos.Offset(0, -1, 0)).IsWater)
                return _defaultDispenseItemBehavior.Dispense(source, dispensed);
            yOffset = 0.0;
        }
        else
        {
            return _defaultDispenseItemBehavior.Dispense(source, dispensed);
        }
        var boat = _type.Create(level);
        if (boat is not null)
        {
            boat.Pos = new Vec3(spawnX, spawnY + yOffset, spawnZ);
            boat.YRot = Direction.GetYRot(direction);
            level.AddEntity(boat);
            dispensed.Shrink(1);
        }
        return dispensed;
    }

    //PlaySound boats use the plain dispense sound, maps to vanilla playSound
    protected override void PlaySound(BlockSource source)
        => source.Level.LevelEvent(LevelEvent.SoundDispenserDispense, source.Pos, 0);
}
