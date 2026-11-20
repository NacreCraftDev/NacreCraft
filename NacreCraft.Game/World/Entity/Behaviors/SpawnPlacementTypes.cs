using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//SpawnPlacementTypes the built-in spawn placement rules, maps to vanilla net.minecraft.world.entity.SpawnPlacementTypes
//NC lacks the vanilla NaturalSpawner empty-block helper, so ON_GROUND checks the floor and the replaceable cells above instead
public static class SpawnPlacementTypes
{
    public static readonly SpawnPlacementType NoRestrictions = new NoRestrictionsPlacement();

    public static readonly SpawnPlacementType InWater = new WaterPlacement();

    public static readonly SpawnPlacementType InLava = new LavaPlacement();

    public static readonly SpawnPlacementType OnGround = new GroundPlacement();

    private sealed class NoRestrictionsPlacement : SpawnPlacementType
    {
        public bool IsSpawnPositionOk(ServerLevel level, BlockPos blockPos, NacreCraft.Registry.EntityType<object> type)
            => true;
    }

    private sealed class WaterPlacement : SpawnPlacementType
    {
        public bool IsSpawnPositionOk(ServerLevel level, BlockPos blockPos, NacreCraft.Registry.EntityType<object> type)
        {
            if (type is null || !level.WorldBorder.IsWithinBounds(blockPos)) return false;
            var above = blockPos.Offset(0, 1, 0);
            var fluid = level.GetFluidState(blockPos);
            if (fluid.IsEmpty || !fluid.IsWater) return false;
            var aboveState = level.GetBlockState(above);
            return aboveState is not { } state || state.Owner is not BlockBehaviour behaviour
                   || !behaviour.IsRedstoneConductor(level, above, state);
        }
    }

    private sealed class LavaPlacement : SpawnPlacementType
    {
        public bool IsSpawnPositionOk(ServerLevel level, BlockPos blockPos, NacreCraft.Registry.EntityType<object> type)
        {
            if (type is null || !level.WorldBorder.IsWithinBounds(blockPos)) return false;
            var fluid = level.GetFluidState(blockPos);
            return !fluid.IsEmpty && fluid.IsLava;
        }
    }

    //GroundPlacement the vanilla on-ground rule, maps to the anonymous class of vanilla SpawnPlacementTypes.ON_GROUND
    private sealed class GroundPlacement : SpawnPlacementType
    {
        public bool IsSpawnPositionOk(ServerLevel level, BlockPos blockPos, NacreCraft.Registry.EntityType<object> type)
        {
            if (type is null || !level.WorldBorder.IsWithinBounds(blockPos)) return false;
            var above = blockPos.Offset(0, 1, 0);
            var below = blockPos.Offset(0, -1, 0);
            var belowState = level.GetBlockState(below);
            if (belowState is not { } floor || floor.IsAir) return false;
            return IsEmptySpawnBlock(level, blockPos) && IsEmptySpawnBlock(level, above);
        }

        //IsEmptySpawnBlock whether a cell is replaceable enough for a mob to stand in
        //The vanilla helper also checks the fluid and collision shape; NC approximates with air and replaceable blocks
        private static bool IsEmptySpawnBlock(ServerLevel level, BlockPos pos)
        {
            var state = level.GetBlockState(pos);
            return state is not { } blockState || blockState.IsAir
                   || blockState.Owner is BlockBehaviour behaviour && behaviour.CanBeReplaced;
        }

        public BlockPos AdjustSpawnPosition(ServerLevel level, BlockPos candidate)
        {
            var below = candidate.Offset(0, -1, 0);
            var state = level.GetBlockState(below);
            return state is { } blockState && blockState.IsPathfindable(PathComputationType.LAND) ? below : candidate;
        }
    }
}
