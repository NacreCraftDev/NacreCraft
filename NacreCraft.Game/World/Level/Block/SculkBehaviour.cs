using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.Block;

//SculkBehaviour what a block does when a sculk charge reaches it, maps to vanilla net.minecraft.world.level.block.SculkBehaviour
//A block that corrodes away (stone, dirt) does not implement it and takes the default: hold the charge and let it decay
//Sculk itself and the sculk vein implement it to spend charge on growing more sculk
public interface SculkBehaviour
{
    //Default the behaviour of a block that is not sculk itself, maps to vanilla DEFAULT
    //It never grows anything and lets the charge decay at the slowest rate
    public static readonly SculkBehaviour Default = new DefaultSculkBehaviour();

    //SculkSpreadDelay ticks before this cursor takes its next step, maps to vanilla getSculkSpreadDelay
    byte SculkSpreadDelay => 1;

    //OnDischarged the charge on this block ran out, maps to vanilla onDischarged
    void OnDischarged(ISculkBlockAccess level, BlockState state, BlockPos pos, RandomSource random)
    {
    }

    //DepositCharge whether the block takes a fresh charge, maps to vanilla depositCharge
    bool DepositCharge(ISculkBlockAccess level, BlockPos pos, RandomSource random) => false;

    //AttemptSpreadVein grow a vein onto the given sides, maps to vanilla attemptSpreadVein
    bool AttemptSpreadVein(ISculkBlockAccess level, BlockPos pos, BlockState state, IReadOnlyCollection<Direction>? facings,
        bool postProcess)
        => SpreadVeinEverywhere(level, pos, state, postProcess);

    //CanChangeBlockStateOnSpread whether spreading rewrites the cell the charge stands on, maps to vanilla canChangeBlockStateOnSpread
    bool CanChangeBlockStateOnSpread => true;

    //UpdateDecayDelay the decay delay for the next step, maps to vanilla updateDecayDelay
    int UpdateDecayDelay(int age) => 1;

    //AttemptUseCharge spend some of the charge and return what is left, maps to vanilla attemptUseCharge
    int AttemptUseCharge(SculkSpreader.ChargeCursor cursor, ISculkBlockAccess level, BlockPos originPos,
        RandomSource random, SculkSpreader spreader, bool spreadVeins);

    //SpreadVeinEverywhere the interface default for attemptSpreadVein, maps to vanilla SculkBehaviour.super.attemptSpreadVein
    //It is a static method rather than an interface default so the default behaviour can reach it too
    internal static bool SpreadVeinEverywhere(ISculkBlockAccess level, BlockPos pos, BlockState state, bool postProcess)
        => Blocks.SCULK_VEIN is MultifaceSpreadeableBlock vein
            && vein.Spreader.SpreadAll(state, level, pos, postProcess) > 0;
}

//DefaultSculkBehaviour the fallback for a block with no sculk behaviour of its own, maps to the vanilla anonymous DEFAULT
internal sealed class DefaultSculkBehaviour : SculkBehaviour
{
    //AttemptSpreadVein grows a vein only when the charge brought side information along
    //No sides means it spreads on its own cell; an empty set means every direction was already covered, which
    //falls back to the shared default; a non-empty set needs the cell to be air or water so the vein can regrow
    public bool AttemptSpreadVein(ISculkBlockAccess level, BlockPos pos, BlockState state,
        IReadOnlyCollection<Direction>? facings, bool postProcess)
    {
        if (facings is null)
            return Blocks.SCULK_VEIN is SculkVeinBlock vein
                && vein.SameSpaceSpreader.SpreadAll(state, level, pos, postProcess) > 0;
        if (facings.Count == 0) return SculkBehaviour.SpreadVeinEverywhere(level, pos, state, postProcess);
        if (!state.Owner.IsAir && !(ReferenceEquals(state.Owner, Blocks.WATER) && state.FluidState.IsSource)) return false;
        return SculkVeinBlock.Regrow(level, pos, state, facings);
    }

    //AttemptUseCharge a plain block keeps its charge while the decay delay is still running, then drops it
    public int AttemptUseCharge(SculkSpreader.ChargeCursor cursor, ISculkBlockAccess level, BlockPos originPos,
        RandomSource random, SculkSpreader spreader, bool spreadVeins)
        => cursor.DecayDelay > 0 ? cursor.Charge : 0;

    //UpdateDecayDelay a plain block counts the delay down, maps to vanilla updateDecayDelay
    public int UpdateDecayDelay(int age) => Math.Max(age - 1, 0);
}
