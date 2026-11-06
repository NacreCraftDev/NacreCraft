using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
//Both Primitives and Registry.Enums define Direction; geometry uses the primitive one here
using Direction = NacreCraft.Primitives.Direction;

namespace NacreCraft.Game.World.Level.Block.Dispenser;

//MinecartDispenseItemBehavior places a minecart onto a rail in front, maps to vanilla net.minecraft.core.dispenser.MinecartDispenseItemBehavior
//The entity type is the generic registry type; minecart entity classes are not ported yet, so registration comes later
public class MinecartDispenseItemBehavior : DefaultDispenseItemBehavior
{
    private readonly DefaultDispenseItemBehavior _defaultDispenseItemBehavior = new();
    private readonly EntityType<object> _entityType;

    public MinecartDispenseItemBehavior(EntityType<object> entityType) => _entityType = entityType;

    protected override ItemStack Execute(BlockSource source, ItemStack dispensed)
    {
        var direction = FacingOf(source.State).ToPrimitive();
        var level = source.Level;
        var center = source.Center;
        var spawnX = center.X + direction.StepX * 1.125;
        var spawnY = Math.Floor(center.Y) + direction.StepY;
        var spawnZ = center.Z + direction.StepZ * 1.125;
        var front = source.Pos.Offset(direction);
        var blockFront = level.GetBlockState(front);
        double yOffset;
        if (blockFront is { } frontState && frontState.Is(BlockTags.Rails))
        {
            yOffset = IsSlope(RailShapeOf(frontState)) ? 0.6 : 0.1;
        }
        else if (blockFront is null || blockFront.Value.Owner.IsAir)
        {
            var blockBelow = level.GetBlockState(front.Offset(0, -1, 0));
            if (blockBelow is not { } belowState || !belowState.Is(BlockTags.Rails))
                return _defaultDispenseItemBehavior.Dispense(source, dispensed);
            yOffset = direction == Direction.Down || !IsSlope(RailShapeOf(belowState)) ? -0.9 : -0.4;
        }
        else
        {
            return _defaultDispenseItemBehavior.Dispense(source, dispensed);
        }
        var minecart = _entityType.Create(level);
        if (minecart is not null)
        {
            minecart.Pos = new Vec3(spawnX, spawnY + yOffset, spawnZ);
            level.AddEntity(minecart);
            dispensed.Shrink(1);
        }
        return dispensed;
    }

    //RailShapeOf reads the rail shape, falling back to north-south for a rail state without a shape property, maps to vanilla getRailShape
    private static RailShape RailShapeOf(BlockState state)
    {
        if (state.HasProperty(BlockStateProperties.RailShapeAll))
            return state.GetValue(BlockStateProperties.RailShapeAll);
        if (state.HasProperty(BlockStateProperties.RailShapeStraight))
            return state.GetValue(BlockStateProperties.RailShapeStraight);
        return RailShape.north_south;
    }

    //IsSlope whether the shape climbs, maps to vanilla RailShape.isSlope
    private static bool IsSlope(RailShape shape)
        => shape is RailShape.ascending_east or RailShape.ascending_west
            or RailShape.ascending_north or RailShape.ascending_south;

    //PlaySound minecarts use the plain dispense sound, maps to vanilla playSound
    protected override void PlaySound(BlockSource source)
        => source.Level.LevelEvent(LevelEvent.SoundDispenserDispense, source.Pos, 0);
}
