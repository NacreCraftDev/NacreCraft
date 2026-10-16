using NacreCraft.Primitives;
using NacreCraft.Registry.State;

namespace NacreCraft.Storage.Updates;

//INeighborUpdater, update dispatcher, maps to vanilla net.minecraft.world.level.redstone.NeighborUpdater
//Both channels share one queue; shape updates and neighbor updates are enqueued by the same rules and interleaved
public interface INeighborUpdater
{
    //ShapeUpdate enqueues a shape update, maps to vanilla shapeUpdate
    void ShapeUpdate(Direction direction, BlockState neighbourState, BlockPos pos, BlockPos neighbourPos,
        int updateFlags, int updateLimit);

    //NeighborChanged enqueues a neighbor update; the state is re-read at that pos on execution, maps to vanilla 3-arg neighborChanged
    void NeighborChanged(BlockPos pos, NacreCraft.Registry.Block changedBlock);

    //NeighborChanged with a state snapshot; not re-read on execution, maps to vanilla 5-arg neighborChanged
    void NeighborChanged(BlockPos pos, BlockState state, NacreCraft.Registry.Block changedBlock, bool movedByPiston);

    //UpdateNeighborsAtExceptFromFacing sends neighbor updates in all six directions, skipping the given direction
    //Maps to vanilla updateNeighborsAtExceptFromFacing
    void UpdateNeighborsAtExceptFromFacing(BlockPos pos, NacreCraft.Registry.Block block, Direction? skipDirection);
}
