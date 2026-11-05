using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Redstone;

namespace NacreCraft.Game.World.Level.Redstone;

//RedstoneWireEvaluator decides the power of one wire cell, maps to vanilla net.minecraft.world.level.redstone.RedstoneWireEvaluator
public abstract class RedstoneWireEvaluator
{
    protected readonly Blocks.RedstoneWireBlock WireBlock;

    protected RedstoneWireEvaluator(Blocks.RedstoneWireBlock wireBlock) => WireBlock = wireBlock;

    public abstract void UpdatePowerStrength(ServerLevel level, BlockPos pos, BlockState state,
        Orientation? orientation, bool skipShapeUpdates);

    protected int GetBlockSignal(ServerLevel level, BlockPos pos) => WireBlock.GetBlockSignal(level, pos);

    //GetWireSignal the power of that cell when it is this wire, maps to vanilla getWireSignal
    protected virtual int GetWireSignal(BlockState? state)
        => state is { } wire && wire.Is(WireBlock) ? wire.GetValue(BlockStateProperties.Power) : 0;

    //GetIncomingWireSignal the strongest neighboring wire minus one, maps to vanilla getIncomingWireSignal
    //The two diagonal cells bypass the decay and whether they apply is decided by whether the neighbor is a conductor
    protected int GetIncomingWireSignal(ServerLevel level, BlockPos pos)
    {
        var abovePos = pos.Offset(Direction.Up);
        var wireSignal = 0;
        foreach (var direction in Blocks.RedstoneWireBlock.HorizontalDirections)
        {
            var neighbourPos = pos.Offset(direction);
            if (level.GetBlockState(neighbourPos) is not { } neighbour) continue;
            wireSignal = Math.Max(wireSignal, GetWireSignal(neighbour));
            if (Blocks.RedstoneWireBlock.IsRedstoneConductor(level, neighbourPos)
                && !Blocks.RedstoneWireBlock.IsRedstoneConductor(level, abovePos))
            {
                var aboveNeighbourPos = neighbourPos.Offset(Direction.Up);
                wireSignal = Math.Max(wireSignal, GetWireSignal(level.GetBlockState(aboveNeighbourPos)));
            }
            else if (!Blocks.RedstoneWireBlock.IsRedstoneConductor(level, neighbourPos))
            {
                var belowNeighbourPos = neighbourPos.Offset(Direction.Down);
                wireSignal = Math.Max(wireSignal, GetWireSignal(level.GetBlockState(belowNeighbourPos)));
            }
        }
        return Math.Max(0, wireSignal - 1);
    }
}
