using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Redstone;
using NacreCraft.Storage.Updates;

namespace NacreCraft.Game.World.Level.Redstone;

//ExperimentalRedstoneWireEvaluator the redstone-experiments wire rule, maps to vanilla net.minecraft.world.level.redstone.ExperimentalRedstoneWireEvaluator
//The experimental rule walks the wires in an orientation frame; NC's block behaviors do not read the orientation yet, so the
//frame only orders the traversal and the neighbor notifications
public sealed class ExperimentalRedstoneWireEvaluator(Blocks.RedstoneWireBlock wireBlock) : RedstoneWireEvaluator(wireBlock)
{
    private readonly Queue<BlockPos> _wiresToTurnOff = new();
    private readonly Queue<BlockPos> _wiresToTurnOn = new();
    private readonly Dictionary<BlockPos, int> _updatedWires = new();

    public override void UpdatePowerStrength(ServerLevel level, BlockPos initialPos, BlockState ignored,
        Orientation? orientation, bool shapeUpdateWiresAroundInitialPosition)
    {
        var initialOrientation = GetInitialOrientation(level, orientation);
        CalculateCurrentChanges(level, initialPos, initialOrientation);
        var initialWire = true;
        foreach (var (pos, packed) in _updatedWires.ToList())
        {
            var newLevel = UnpackPower(packed);
            var state = level.GetBlockState(pos);
            if (state is { } current && current.Is(WireBlock)
                && current.GetValue(BlockStateProperties.Power) != newLevel)
            {
                var updateFlags = BlockUpdateFlags.Clients;
                if (!shapeUpdateWiresAroundInitialPosition || !initialWire)
                    updateFlags |= BlockUpdateFlags.SkipShapeUpdateOnWire;
                level.SetBlock(pos, current.SetValue(BlockStateProperties.Power, newLevel), updateFlags);
            }
            else
            {
                _updatedWires.Remove(pos);
            }
            initialWire = false;
        }
        CauseNeighborUpdates(level);
    }

    private void CauseNeighborUpdates(ServerLevel level)
    {
        foreach (var (wirePos, packed) in _updatedWires)
        {
            var orientation = UnpackOrientation(packed);
            if (level.GetBlockState(wirePos) is not { } state) continue;
            foreach (var neighborDirection in orientation.Neighbors)
            {
                if (!IsConnected(state, neighborDirection)) continue;
                var neighborPos = wirePos.Offset(neighborDirection);
                if (level.GetBlockState(neighborPos) is not { } neighborState) continue;
                var neighborOrientation = orientation.WithFrontPreserveUp(neighborDirection);
                level.NeighborChanged(neighborState, neighborPos, WireBlock, neighborOrientation, false);
                if (!Blocks.RedstoneWireBlock.IsRedstoneConductor(level, neighborPos)) continue;
                foreach (var direction in neighborOrientation.Neighbors)
                {
                    if (direction == neighborDirection.Opposite) continue;
                    level.NeighborChanged(neighborPos.Offset(direction), WireBlock,
                        neighborOrientation.WithFrontPreserveUp(direction));
                }
            }
        }
    }

    private static bool IsConnected(BlockState state, Direction direction)
    {
        var property = Blocks.RedstoneWireBlock.PropertyByDirection(direction);
        return property is null ? direction == Direction.Down : state.GetValue(property).IsConnected();
    }

    private static Orientation GetInitialOrientation(ServerLevel level, Orientation? incomingOrientation)
        => (incomingOrientation ?? Orientation.Random(level.Random))
            .WithUp(Direction.Up)
            .WithSideBias(Orientation.SideBias.LEFT);

    private void CalculateCurrentChanges(ServerLevel level, BlockPos initialPosition, Orientation initialOrientation)
    {
        var initialState = level.GetBlockState(initialPosition);
        if (initialState is { } startState && startState.Is(WireBlock))
        {
            SetPower(initialPosition, startState.GetValue(BlockStateProperties.Power), initialOrientation);
            _wiresToTurnOff.Enqueue(initialPosition);
        }
        else
        {
            PropagateChangeToNeighbors(level, initialPosition, 0, initialOrientation, true);
        }
        while (_wiresToTurnOff.Count > 0)
        {
            var pos = _wiresToTurnOff.Dequeue();
            var packed = _updatedWires[pos];
            var orientation = UnpackOrientation(packed);
            var oldPower = UnpackPower(packed);
            var blockPower = GetBlockSignal(level, pos);
            var wirePower = GetIncomingWireSignal(level, pos);
            var newPower = Math.Max(blockPower, wirePower);
            int powerToSet;
            if (newPower < oldPower)
            {
                if (blockPower > 0 && !_wiresToTurnOn.Contains(pos)) _wiresToTurnOn.Enqueue(pos);
                powerToSet = 0;
            }
            else
            {
                powerToSet = newPower;
            }
            if (powerToSet != oldPower) SetPower(pos, powerToSet, orientation);
            PropagateChangeToNeighbors(level, pos, powerToSet, orientation, oldPower > newPower);
        }
        while (_wiresToTurnOn.Count > 0)
        {
            var pos = _wiresToTurnOn.Dequeue();
            var packed = _updatedWires[pos];
            var oldPower = UnpackPower(packed);
            var blockPower = GetBlockSignal(level, pos);
            var wirePower = GetIncomingWireSignal(level, pos);
            var newPower = Math.Max(blockPower, wirePower);
            var orientation = UnpackOrientation(packed);
            if (newPower > oldPower) SetPower(pos, newPower, orientation);
            else if (newPower < oldPower)
                throw new InvalidOperationException("Turning off wire while trying to turn it on. Should not happen.");
            PropagateChangeToNeighbors(level, pos, newPower, orientation, false);
        }
    }

    private void PropagateChangeToNeighbors(ServerLevel level, BlockPos pos, int newPower, Orientation orientation,
        bool allowTurningOff)
    {
        foreach (var directionHorizontal in orientation.HorizontalNeighbors)
        {
            var offsetPos = pos.Offset(directionHorizontal);
            EnqueueNeighborWire(level, offsetPos, newPower, orientation.WithFront(directionHorizontal), allowTurningOff);
        }
        foreach (var directionVertical in orientation.VerticalNeighbors)
        {
            var offsetPos = pos.Offset(directionVertical);
            var solidBlock = Blocks.RedstoneWireBlock.IsRedstoneConductor(level, offsetPos);
            foreach (var directionHorizontal in orientation.HorizontalNeighbors)
            {
                var neighbor = pos.Offset(directionHorizontal);
                if (directionVertical == Direction.Up && !solidBlock)
                {
                    EnqueueNeighborWire(level, offsetPos.Offset(directionHorizontal), newPower,
                        orientation.WithFront(directionHorizontal), allowTurningOff);
                    continue;
                }
                if (directionVertical != Direction.Down
                    || Blocks.RedstoneWireBlock.IsRedstoneConductor(level, neighbor)) continue;
                EnqueueNeighborWire(level, offsetPos.Offset(directionHorizontal), newPower,
                    orientation.WithFront(directionHorizontal), allowTurningOff);
            }
        }
    }

    private void EnqueueNeighborWire(ServerLevel level, BlockPos pos, int newFromPower, Orientation orientation,
        bool allowTurningOff)
    {
        if (level.GetBlockState(pos) is not { } state || !state.Is(WireBlock)) return;
        var toPower = GetWireSignal(state);
        if (toPower < newFromPower - 1 && !_wiresToTurnOn.Contains(pos))
        {
            _wiresToTurnOn.Enqueue(pos);
            SetPower(pos, toPower, orientation);
        }
        if (allowTurningOff && toPower > newFromPower && !_wiresToTurnOff.Contains(pos))
        {
            _wiresToTurnOff.Enqueue(pos);
            SetPower(pos, toPower, orientation);
        }
    }

    private void SetPower(BlockPos pos, int newPower, Orientation orientation)
    {
        _updatedWires[pos] = _updatedWires.TryGetValue(pos, out var packed)
            ? PackOrientationAndPower(UnpackOrientation(packed), newPower)
            : PackOrientationAndPower(orientation, newPower);
    }

    protected override int GetWireSignal(BlockState? state)
        => state is { } wire && wire.Is(WireBlock) ? wire.GetValue(BlockStateProperties.Power) : 0;

    private static int PackOrientationAndPower(Orientation orientation, int power)
        => (orientation.Index << 4) | power;

    private static Orientation UnpackOrientation(int packed) => Orientation.FromIndex(packed >> 4);

    private static int UnpackPower(int packed) => packed & 0xF;
}
