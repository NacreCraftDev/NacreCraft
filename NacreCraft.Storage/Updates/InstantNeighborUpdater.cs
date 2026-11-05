using NacreCraft.Primitives;
using NacreCraft.Registry.State;

namespace NacreCraft.Storage.Updates;

//InstantNeighborUpdater runs every neighbor update immediately instead of collecting it, maps to vanilla net.minecraft.world.level.redstone.InstantNeighborUpdater
public sealed class InstantNeighborUpdater(ServerLevel level) : INeighborUpdater
{
    public void ShapeUpdate(Direction direction, BlockState neighbourState, BlockPos pos, BlockPos neighbourPos,
        int updateFlags, int updateLimit)
        => NeighborUpdater.ExecuteShapeUpdate(level, direction, pos, neighbourPos, neighbourState,
            updateFlags, updateLimit - 1);

    public void NeighborChanged(BlockPos pos, NacreCraft.Registry.Block changedBlock)
    {
        var state = level.GetBlockState(pos);
        if (state is null) return;
        NeighborChanged(pos, state.Value, changedBlock, false);
    }

    public void NeighborChanged(BlockPos pos, BlockState state, NacreCraft.Registry.Block changedBlock,
        bool movedByPiston)
        => NeighborUpdater.ExecuteUpdate(level, state, pos, changedBlock, movedByPiston);

    public void UpdateNeighborsAtExceptFromFacing(BlockPos pos, NacreCraft.Registry.Block block,
        Direction? skipDirection)
    {
        foreach (var direction in BlockUpdateFlags.NeighbourUpdateOrder)
        {
            if (direction == skipDirection) continue;
            NeighborChanged(pos.Offset(direction), block);
        }
    }
}
