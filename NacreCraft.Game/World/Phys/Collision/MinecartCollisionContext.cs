using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Primitives.Phys;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using Direction = NacreCraft.Primitives.Direction;

namespace NacreCraft.Game.World.Phys.Collision;

//MinecartCollisionContext collision context of a minecart, maps to vanilla net.minecraft.world.phys.shapes.MinecartCollisionContext
//While a minecart sits on a rail the block below the rail and the block the slope rises into must not collide, otherwise the cart snags
public sealed class MinecartCollisionContext : EntityCollisionContext
{
    private readonly BlockPos? _ignoreBelow;
    private readonly BlockPos? _slopeIgnore;

    //The minecart entity type is not ported yet, so the rail position is passed in instead of being read from the cart
    //With alwaysStandOnFluid the cart also collides with the fluid surface, matching the vanilla constructor
    public MinecartCollisionContext(NacreCraft.Registry.Entity entity, bool alwaysStandOnFluid, PersistentServerLevel level,
        BlockPos currentRailPos)
        : base(entity, alwaysStandOnFluid, false)
    {
        var state = level.GetBlockState(currentRailPos);
        if (state is null || !Blocks.BaseRail.IsRail(state.Value)) return;
        _ignoreBelow = currentRailPos.Offset(Direction.Down);
        var shape = Blocks.BaseRail.GetRailShape(state.Value);
        if (!shape.IsSlope()) return;
        _slopeIgnore = shape switch
        {
            RailShape.ascending_east => currentRailPos.Offset(Direction.East),
            RailShape.ascending_west => currentRailPos.Offset(Direction.West),
            RailShape.ascending_north => currentRailPos.Offset(Direction.North),
            RailShape.ascending_south => currentRailPos.Offset(Direction.South),
            _ => null,
        };
    }

    public override VoxelShape GetCollisionShape(BlockState state, CollisionGetter getter, BlockPos pos)
        => pos == _ignoreBelow || pos == _slopeIgnore ? Shapes.Empty() : base.GetCollisionShape(state, getter, pos);
}
