using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util;
using NacreCraft.Util.Collection;
using NacreCraft.Util.Random;
using BlockTags = NacreCraft.Game.World.Level.Block.BlockTags;

namespace NacreCraft.Game.World.Level.Block;

//SculkSpreader carries the charges a sculk catalyst pushes outward, maps to vanilla net.minecraft.world.level.block.SculkSpreader
//A charge sits on a block that knows SculkBehaviour; each step asks that block to spend some of it, then the remainder walks
//to a neighbouring sculk cell and keeps going until it runs out
//The save codec is not ported: it would need the charge cursor codec, and nothing here persists a catalyst yet
public sealed class SculkSpreader
{
    //MaxGrowthRateRadius the reach within which a growth still costs the full charge, maps to vanilla MAX_GROWTH_RATE_RADIUS
    public const int MaxGrowthRateRadius = 24;

    //MaxCharge one cursor never holds more than this, maps to vanilla MAX_CHARGE
    public const int MaxCharge = 1000;

    //MaxDecayFactor the hardest a single step may decay, maps to vanilla MAX_DECAY_FACTOR
    public const float MaxDecayFactor = 0.5f;

    //ShriekerPlacementRate one in this many growths becomes a shrieker, maps to vanilla SHRIEKER_PLACEMENT_RATE
    public const int ShriekerPlacementRate = 11;

    //MaxCursorDistance a cursor further than this from the origin is dropped, maps to vanilla MAX_CURSOR_DISTANCE
    public const int MaxCursorDistance = 1024;

    //MaxCursors how many charges may be in flight at once, maps to vanilla MAX_CURSORS
    private const int MaxCursors = 32;

    private readonly bool _isWorldGeneration;
    private readonly TagKey<NacreCraft.Registry.Block> _replaceableBlocks;
    private readonly int _growthSpawnCost;
    private readonly int _noGrowthRadius;
    private readonly int _chargeDecayRate;
    private readonly int _additionalDecayRate;

    public SculkSpreader(bool isWorldGeneration, TagKey<NacreCraft.Registry.Block> replaceableBlocks,
        int growthSpawnCost, int noGrowthRadius, int chargeDecayRate, int additionalDecayRate)
    {
        _isWorldGeneration = isWorldGeneration;
        _replaceableBlocks = replaceableBlocks;
        _growthSpawnCost = growthSpawnCost;
        _noGrowthRadius = noGrowthRadius;
        _chargeDecayRate = chargeDecayRate;
        _additionalDecayRate = additionalDecayRate;
    }

    //CreateLevelSpreader the parameters a live catalyst uses, maps to vanilla createLevelSpreader
    public static SculkSpreader CreateLevelSpreader()
        => new(false, BlockTags.SculkReplaceable, 10, 4, 10, 5);

    //CreateWorldGenSpreader the parameters world generation uses, maps to vanilla createWorldGenSpreader
    public static SculkSpreader CreateWorldGenSpreader()
        => new(true, BlockTags.SculkReplaceableWorldGen, 50, 1, 5, 10);

    public TagKey<NacreCraft.Registry.Block> ReplaceableBlocks => _replaceableBlocks;

    public int GrowthSpawnCost => _growthSpawnCost;

    public int NoGrowthRadius => _noGrowthRadius;

    public int ChargeDecayRate => _chargeDecayRate;

    public int AdditionalDecayRate => _additionalDecayRate;

    public bool IsWorldGeneration => _isWorldGeneration;

    //Cursors the charges in flight, for diagnostics
    public List<ChargeCursor> Cursors { get; private set; } = [];

    public void Clear() => Cursors.Clear();

    //AddCursors drops a block of charge in, split into cursors of at most MAX_CHARGE, maps to vanilla addCursors
    public void AddCursors(BlockPos startPos, int charge)
    {
        while (charge > 0)
        {
            var current = Math.Min(charge, MaxCharge);
            AddCursor(new ChargeCursor(startPos, current));
            charge -= current;
        }
    }

    private void AddCursor(ChargeCursor cursor)
    {
        if (Cursors.Count >= MaxCursors) return;
        Cursors.Add(cursor);
    }

    //UpdateCursors advances every cursor one step and merges the ones that ended up on the same cell
    //The charge that landed on each cell is then broadcast as a sculk charge particle event, maps to vanilla updateCursors
    public void UpdateCursors(ISculkBlockAccess level, BlockPos originPos, RandomSource random, bool spreadVeins)
    {
        if (Cursors.Count == 0) return;
        var processed = new List<ChargeCursor>();
        var mergeable = new Dictionary<BlockPos, ChargeCursor>();
        var chargeByPos = new Dictionary<BlockPos, int>();

        foreach (var cursor in Cursors)
        {
            if (cursor.IsPosUnreasonable(originPos)) continue;
            cursor.Update(level, originPos, random, this, spreadVeins);
            if (cursor.Charge <= 0)
            {
                level.LevelEvent(LevelEventSculkChargeDeath, cursor.Pos, 0);
                continue;
            }
            var pos = cursor.Pos;
            chargeByPos[pos] = chargeByPos.GetValueOrDefault(pos) + cursor.Charge;
            if (mergeable.TryGetValue(pos, out var existing))
            {
                //Two cursors on one cell fold together unless that would overflow or world generation split them on purpose
                if (!IsWorldGeneration && cursor.Charge + existing.Charge <= MaxCharge)
                {
                    existing.MergeWith(cursor);
                    continue;
                }
                processed.Add(cursor);
                if (cursor.Charge >= existing.Charge) continue;
                mergeable[pos] = cursor;
                continue;
            }
            mergeable[pos] = cursor;
            processed.Add(cursor);
        }

        foreach (var (pos, charge) in chargeByPos)
        {
            if (charge <= 0) continue;
            if (!mergeable.TryGetValue(pos, out var cursor) || cursor.FacingData is not { } faces) continue;
            //The particle count grows with the logarithm of the charge, maps to vanilla log1p(charge) / 2.3
            var particles = (int)(Math.Log(1.0 + charge) / 2.3) + 1;
            level.LevelEvent(LevelEventSculkCharge, pos, (particles << 6) + MultifaceBlock.Pack(faces));
        }
        Cursors = processed;
    }

    //LevelEventSculkCharge the particle event a live charge broadcasts, maps to vanilla 3006
    private const int LevelEventSculkCharge = 3006;

    //LevelEventSculkChargeDeath the same event with no charge left, maps to vanilla 3006 with data 0
    private const int LevelEventSculkChargeDeath = 3006;

    //ChargeCursor one charge in flight, maps to vanilla SculkSpreader.ChargeCursor
    public sealed class ChargeCursor
    {
        //MaxCursorDecayDelay the largest decay delay a cursor carries, maps to vanilla MAX_CURSOR_DECAY_DELAY
        public const int MaxCursorDecayDelay = 1;

        //The eighteen neighbours that share at least one axis value, which is where a charge may step
        private static readonly Vec3i[] NonCornerNeighbourOffsets = BuildNonCornerNeighbours();

        public ChargeCursor(BlockPos pos, int charge)
        {
            Pos = pos;
            Charge = charge;
            DecayDelay = 1;
        }

        public BlockPos Pos { get; private set; }

        public int Charge { get; private set; }

        public int UpdateDelay { get; private set; }

        public int DecayDelay { get; private set; }

        //FacingData the sides the charge is currently attached to, null when it sits on something that is not multiface
        public HashSet<Direction>? FacingData { get; private set; }

        //IsPosUnreasonable whether the cursor wandered too far from the origin to keep going, maps to vanilla isPosUnreasonable
        public bool IsPosUnreasonable(BlockPos originPos) => DistChessboard(Pos, originPos) > MaxCursorDistance;

        //Update advances this cursor one step, maps to vanilla update
        public void Update(ISculkBlockAccess level, BlockPos originPos, RandomSource random, SculkSpreader spreader,
            bool spreadVeins)
        {
            if (!ShouldUpdate(level, spreader.IsWorldGeneration)) return;
            if (UpdateDelay > 0)
            {
                UpdateDelay--;
                return;
            }

            var currentState = level.GetBlockState(Pos) ?? Blocks.AIR.DefaultBlockState;
            var behaviour = GetBlockBehaviour(currentState);
            if (spreadVeins && behaviour.AttemptSpreadVein(level, Pos, currentState, FacingData, spreader.IsWorldGeneration))
            {
                if (behaviour.CanChangeBlockStateOnSpread)
                {
                    currentState = level.GetBlockState(Pos) ?? Blocks.AIR.DefaultBlockState;
                    behaviour = GetBlockBehaviour(currentState);
                }
            }

            Charge = behaviour.AttemptUseCharge(this, level, originPos, random, spreader, spreadVeins);
            if (Charge <= 0)
            {
                behaviour.OnDischarged(level, currentState, Pos, random);
                return;
            }

            var transferPos = GetValidMovementPos(level, Pos, random);
            if (transferPos is { } target)
            {
                behaviour.OnDischarged(level, currentState, Pos, random);
                Pos = target;
                //World generation keeps the charge near the origin so a catalyst cannot leak across the whole cave
                if (spreader.IsWorldGeneration
                    && !CloserThan(target, originPos.X, originPos.Z, 15.0))
                {
                    Charge = 0;
                    return;
                }
                currentState = level.GetBlockState(target) ?? Blocks.AIR.DefaultBlockState;
            }

            if (currentState.Owner is MultifaceBlock)
                FacingData = new HashSet<Direction>(MultifaceBlock.AvailableFaces(currentState));
            DecayDelay = behaviour.UpdateDecayDelay(DecayDelay);
            UpdateDelay = behaviour.SculkSpreadDelay;
        }

        //MergeWith folds another cursor's charge in, maps to vanilla mergeWith
        internal void MergeWith(ChargeCursor other)
        {
            Charge += other.Charge;
            other.Charge = 0;
            UpdateDelay = Math.Min(UpdateDelay, other.UpdateDelay);
        }

        //ShouldUpdate whether the cell is ticked at all, maps to vanilla shouldUpdate
        //A world generation cursor always runs; a live one only runs where blocks tick
        private bool ShouldUpdate(ISculkBlockAccess level, bool isWorldGen)
        {
            if (Charge <= 0) return false;
            if (isWorldGen) return true;
            //Vanilla asks whether the chunk is inside simulation distance; this port has no such query on the level
            return true;
        }

        //GetBlockBehaviour the sculk behaviour of a state, the default one when the block has none
        //maps to vanilla getBlockBehaviour
        private static SculkBehaviour GetBlockBehaviour(BlockState state)
            => state.Owner as SculkBehaviour ?? SculkBehaviour.Default;

        //GetValidMovementPos picks a neighbouring cell a sculk cell can step to, maps to vanilla getValidMovementPos
        //It must be another sculk cell, the way there must be clear, and the target must still touch a substrate
        private static BlockPos? GetValidMovementPos(ISculkBlockAccess level, BlockPos pos, RandomSource random)
        {
            var offsets = new List<Vec3i>(NonCornerNeighbourOffsets);
            RandomCollections.Shuffle(offsets, random);
            foreach (var offset in offsets)
            {
                var neighbour = Offset(pos, offset);
                if (level.GetBlockState(neighbour) is not { } transferee) continue;
                if (transferee.Owner is not SculkBehaviour) continue;
                if (!IsMovementUnobstructed(level, pos, neighbour)) continue;
                if (!SculkVeinBlock.HasSubstrateAccess(level, transferee, neighbour)) continue;
                return neighbour;
            }
            return null;
        }

        //IsMovementUnobstructed whether a charge can step between two diagonal-ish cells, maps to vanilla isMovementUnobstructed
        private static bool IsMovementUnobstructed(ISculkBlockAccess level, BlockPos from, BlockPos to)
        {
            if (DistManhattan(from, to) == 1) return true;
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var dz = to.Z - from.Z;
            var directionX = DirectionFromAxis(Direction.Axis.X, dx >= 0);
            var directionY = DirectionFromAxis(Direction.Axis.Y, dy >= 0);
            var directionZ = DirectionFromAxis(Direction.Axis.Z, dz >= 0);
            if (dx == 0) return IsUnobstructed(level, from, directionY) || IsUnobstructed(level, from, directionZ);
            if (dy == 0) return IsUnobstructed(level, from, directionX) || IsUnobstructed(level, from, directionZ);
            return IsUnobstructed(level, from, directionX) || IsUnobstructed(level, from, directionY);
        }

        //IsUnobstructed whether the neighbour on that side leaves a way through, maps to vanilla isUnobstructed
        private static bool IsUnobstructed(ISculkBlockAccess level, BlockPos from, Direction direction)
        {
            var testPos = from.Offset(direction);
            if (level.GetBlockState(testPos) is not { } state) return true;
            //The support shape does not depend on what surrounds it, so an empty view stands in for the level
            return state.Owner is not BlockBehaviour behaviour
                || !behaviour.IsFaceSturdy(EmptyBlockGetter.Instance, testPos, state, direction.Opposite,
                    SupportType.Full);
        }

        private static Direction DirectionFromAxis(Direction.Axis axis, bool positive)
            => Direction.Values.First(direction => direction.AxisValue == axis
                && (direction.StepX + direction.StepY + direction.StepZ > 0) == positive);

        //BuildNonCornerNeighbours the eighteen offsets that share at least one axis value with the origin
        //maps to vanilla NON_CORNER_NEIGHBOURS, a filter over BlockPos.betweenClosed, hence the z outer, y middle,
        //x inner order; the order matters because the list is shuffled, and a different start order yields a
        //different patch for the same seed
        private static Vec3i[] BuildNonCornerNeighbours()
        {
            var result = new List<Vec3i>(18);
            for (var z = -1; z <= 1; z++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    for (var x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        if (x != 0 && y != 0 && z != 0) continue;
                        result.Add(new Vec3i(x, y, z));
                    }
                }
            }
            return [.. result];
        }

        private static BlockPos Offset(BlockPos pos, Vec3i offset)
            => new(pos.X + offset.X, pos.Y + offset.Y, pos.Z + offset.Z);

        private static int DistManhattan(BlockPos first, BlockPos second)
            => Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) + Math.Abs(first.Z - second.Z);

        private static int DistChessboard(BlockPos first, BlockPos second)
            => Math.Max(Math.Max(Math.Abs(first.X - second.X), Math.Abs(first.Y - second.Y)),
                Math.Abs(first.Z - second.Z));

        //CloserThan whether the cell is within the given horizontal radius of the origin, maps to vanilla closerThan
        private static bool CloserThan(BlockPos pos, int originX, int originZ, double distance)
        {
            var dx = pos.X - originX;
            var dz = pos.Z - originZ;
            return dx * dx + dz * dz < distance * distance;
        }
    }
}
