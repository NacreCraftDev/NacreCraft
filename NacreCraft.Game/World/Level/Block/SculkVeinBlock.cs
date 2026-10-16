using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util;
using NacreCraft.Util.Collection;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.Block;

//SculkVeinBlock sculk veins, maps to vanilla net.minecraft.world.level.block.SculkVeinBlock
//A multiface block: thin plates on the inside of whichever sides are occupied, and the surface a sculk charge travels over
//Two spreaders drive it: one grows vein onto the same cell, the other walks to neighbours and wraps around corners
public sealed class SculkVeinBlock : MultifaceSpreadeableBlock, SculkBehaviour
{
    private readonly MultifaceSpreader _veinSpreader;
    private readonly MultifaceSpreader _sameSpaceSpreader;

    public SculkVeinBlock()
    {
        _veinSpreader = new MultifaceSpreader(new SculkVeinSpreaderConfig(this, MultifaceSpreader.DefaultSpreadOrder));
        _sameSpaceSpreader = new MultifaceSpreader(new SculkVeinSpreaderConfig(this, [SpreadKind.SamePosition]));
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("sculk_vein");

    //Spreader the spreader that walks to neighbours, maps to vanilla getSpreader
    public override MultifaceSpreader Spreader => _veinSpreader;

    //SameSpaceSpreader the spreader that only fills the cell it stands on, maps to vanilla getSameSpaceSpreader
    public MultifaceSpreader SameSpaceSpreader => _sameSpaceSpreader;

    //Regrow puts a fresh vein on the given sides of a cell, maps to vanilla regrow
    //Only the sides whose neighbour can carry a plate survive; a cell with none left keeps nothing
    public static bool Regrow(ISculkBlockAccess level, BlockPos pos, BlockState existing, IReadOnlyCollection<Direction> faces)
    {
        var hasFace = false;
        var newState = Blocks.SCULK_VEIN.DefaultBlockState;
        foreach (var face in faces)
        {
            if (!CanAttachTo(level, pos, face)) continue;
            newState = newState.SetValue(GetFaceProperty(face), true);
            hasFace = true;
        }
        if (!hasFace) return false;
        if (!existing.FluidState.IsEmpty) newState = newState.SetValue(Waterlogged, true);
        level.SetBlock(pos, newState);
        return true;
    }

    //OnDischarged the charge ran out: every plate that faces a sculk block is dropped, maps to vanilla onDischarged
    //A vein left with no plates clears itself away, leaving water behind when the cell was waterlogged
    public void OnDischarged(ISculkBlockAccess level, BlockState state, BlockPos pos, RandomSource random)
    {
        if (!ReferenceEquals(state.Owner, this)) return;
        foreach (var direction in Direction.Values)
        {
            var property = GetFaceProperty(direction);
            if (!state.GetValue(property)) continue;
            if (level.GetBlockState(pos.Offset(direction)) is not { } neighbour) continue;
            if (!neighbour.Is(Blocks.SculkState.Owner)) continue;
            state = state.SetValue(property, false);
        }
        if (!HasAnyFace(state))
        {
            //A waterlogged cell leaves the water behind once the last plate is gone
            var stillWater = level.GetBlockState(pos) is { } at && !at.FluidState.IsEmpty;
            state = stillWater ? Blocks.WATER.DefaultBlockState : Blocks.AIR.DefaultBlockState;
        }
        level.SetBlock(pos, state);
    }

    //AttemptUseCharge a vein either puts down a sculk block or lets the charge decay, maps to vanilla attemptUseCharge
    public int AttemptUseCharge(SculkSpreader.ChargeCursor cursor, ISculkBlockAccess level, BlockPos originPos,
        RandomSource random, SculkSpreader spreader, bool spreadVeins)
    {
        if (spreadVeins && AttemptPlaceSculk(spreader, level, cursor.Pos, random)) return cursor.Charge - 1;
        return random.NextInt(spreader.ChargeDecayRate) == 0
            ? Mth.Floor(cursor.Charge * 0.5f)
            : cursor.Charge;
    }

    //AttemptPlaceSculk turns a replaceable neighbour under an occupied plate into a sculk block, maps to vanilla
    //The new block then grows vein of its own in every other direction, so the corrosion creeps outward
    private bool AttemptPlaceSculk(SculkSpreader spreader, ISculkBlockAccess level, BlockPos pos, RandomSource random)
    {
        if (level.GetBlockState(pos) is not { } state) return false;
        var supports = new List<Direction>(Direction.Values);
        RandomCollections.Shuffle(supports, random);
        foreach (var support in supports)
        {
            if (!HasFace(state, support)) continue;
            var supportPos = pos.Offset(support);
            if (level.GetBlockState(supportPos) is not { } supportState) continue;
            if (!supportState.Is(spreader.ReplaceableBlocks)) continue;

            var defaultSculk = Blocks.SculkState;
            level.SetBlock(supportPos, defaultSculk);
            //Vanilla also pushes entities up out of the new block and plays the spread sound; this port has neither
            _veinSpreader.SpreadAll(defaultSculk, level, supportPos, spreader.IsWorldGeneration);

            //Vein hanging off the sides of the new sculk block is no longer needed, maps to vanilla's discharge loop
            var skip = support.Opposite;
            foreach (var veinDirection in Direction.Values)
            {
                if (veinDirection == skip) continue;
                var veinPos = supportPos.Offset(veinDirection);
                if (level.GetBlockState(veinPos) is not { } possibleVein) continue;
                if (!ReferenceEquals(possibleVein.Owner, this)) continue;
                OnDischarged(level, possibleVein, veinPos, random);
            }
            return true;
        }
        return false;
    }

    //HasSubstrateAccess whether any occupied plate faces a block a sculk charge could still eat, maps to vanilla
    //The charge movement uses it to tell a vein worth stepping onto from one that is stuck
    public static bool HasSubstrateAccess(ISculkBlockAccess level, BlockState state, BlockPos pos)
    {
        if (!ReferenceEquals(state.Owner, Blocks.SCULK_VEIN)) return false;
        foreach (var direction in Direction.Values)
        {
            if (!HasFace(state, direction)) continue;
            if (level.GetBlockState(pos.Offset(direction)) is not { } neighbour) continue;
            if (neighbour.Is(BlockTags.SculkReplaceable)) return true;
        }
        return false;
    }

    //SculkVeinSpreaderConfig the vein-specific spread rules, maps to vanilla SculkVeinSpreaderConfig
    private sealed class SculkVeinSpreaderConfig : DefaultSpreaderConfig
    {
        private readonly SpreadKind[] _spreadKinds;

        public SculkVeinSpreaderConfig(SculkVeinBlock block, params SpreadKind[] spreadKinds) : base(block)
            => _spreadKinds = spreadKinds;

        public override SpreadKind[] SpreadTypes => _spreadKinds;

        //A vein may grow from any block, not only from itself, maps to vanilla isOtherBlockValidAsSource
        public override bool IsOtherBlockValidAsSource(BlockState state)
            => !ReferenceEquals(state.Owner, Blocks.SCULK_VEIN);

        //The vein adds three refusals on top of the plain rules, maps to vanilla stateCanBeReplaced
        //It will not grow against a sculk block, a catalyst or a moving piston head, nor across a two block gap
        //where the far side is already solid; a non-water fluid or fire also blocks it
        protected override bool StateCanBeReplaced(ISculkBlockAccess level, BlockPos sourcePos, BlockPos placementPos,
            Direction placementDirection, BlockState existingState)
        {
            var againstPos = placementPos.Offset(placementDirection);
            if (level.GetBlockState(againstPos) is { } againstState
                && againstState.Is(Blocks.SculkState.Owner))
                return false;

            if (DistManhattan(sourcePos, placementPos) == 2)
            {
                var neighbourPos = sourcePos.Offset(placementDirection.Opposite);
                if (level.GetBlockState(neighbourPos) is { } neighbour
                    && neighbour.Owner is BlockBehaviour behaviour
                    && behaviour.IsFaceSturdy(EmptyBlockGetter.Instance, neighbourPos, neighbour, placementDirection,
                        SupportType.Full))
                    return false;
            }

            var fluid = existingState.FluidState;
            if (!fluid.IsEmpty && !fluid.IsWater) return false;
            return existingState.Owner is BlockBehaviour existingBehaviour && existingBehaviour.CanBeReplaced
                || base.StateCanBeReplaced(level, sourcePos, placementPos, placementDirection, existingState);
        }

        private static int DistManhattan(BlockPos first, BlockPos second)
            => Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) + Math.Abs(first.Z - second.Z);
    }
}
