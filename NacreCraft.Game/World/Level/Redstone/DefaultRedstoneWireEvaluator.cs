using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Redstone;
using NacreCraft.Storage.Updates;

namespace NacreCraft.Game.World.Level.Redstone;

//DefaultRedstoneWireEvaluator the classic wire power rule, maps to vanilla net.minecraft.world.level.redstone.DefaultRedstoneWireEvaluator
public sealed class DefaultRedstoneWireEvaluator(Blocks.RedstoneWireBlock wireBlock) : RedstoneWireEvaluator(wireBlock)
{
    public override void UpdatePowerStrength(ServerLevel level, BlockPos pos, BlockState state,
        Orientation? orientation, bool skipShapeUpdates)
    {
        var targetStrength = CalculateTargetStrength(level, pos);
        if (state.GetValue(BlockStateProperties.Power) == targetStrength) return;
        if (level.GetBlockState(pos) is { } current && current == state)
            level.SetBlock(pos, state.SetValue(BlockStateProperties.Power, targetStrength),
                BlockUpdateFlags.Clients);
        level.UpdateNeighborsAt(pos, WireBlock);
        foreach (var direction in Direction.Values)
            level.UpdateNeighborsAt(pos.Offset(direction), WireBlock);
    }

    //CalculateTargetStrength takes the max of neighbor signals and adjacent wire, maps to vanilla calculateTargetStrength
    private int CalculateTargetStrength(ServerLevel level, BlockPos pos)
    {
        var blockSignal = GetBlockSignal(level, pos);
        if (blockSignal == 15) return blockSignal;
        return Math.Max(blockSignal, GetIncomingWireSignal(level, pos));
    }
}
