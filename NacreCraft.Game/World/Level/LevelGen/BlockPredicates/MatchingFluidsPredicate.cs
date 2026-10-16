using NacreCraft.Codec;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Level.LevelGen.BlockPredicates;

//MatchingFluidsPredicate matches a fluid set, maps to vanilla MatchingFluidsPredicate
public class MatchingFluidsPredicate : StateTestingPredicate
{
    //EmptyFluid empty fluid placeholder, maps to vanilla Fluids.EMPTY; the fluid registry is not populated yet
    public static readonly Fluid EmptyFluid = Fluid.Empty;

    public static readonly Codec<MatchingFluidsPredicate> Codec =
        RecordCodecBuilder.Of2<MatchingFluidsPredicate, Vec3i, HolderSet<Fluid>>(
            StateTestingCodec().ForGetter<MatchingFluidsPredicate, Vec3i>(p => p.Offset),
            new RegistryHolderSetCodec<Fluid>(BuiltInRegistries.FLUID).FieldOf("fluids")
                .ForGetter<MatchingFluidsPredicate, HolderSet<Fluid>>(p => p._fluids),
            (offset, fluids) => new MatchingFluidsPredicate(offset, fluids));

    private readonly HolderSet<Fluid> _fluids;

    public MatchingFluidsPredicate(Vec3i offset, HolderSet<Fluid> fluids) : base(offset) => _fluids = fluids;

    public HolderSet<Fluid> Fluids => _fluids;

    //NacreCraft fluid states only distinguish empty from non-empty, so approximate by checking whether the fluid id is empty
    protected override bool Test(BlockState state)
    {
        if (!_fluids.IsBound) return false;
        var fluidState = state.FluidState;
        foreach (var holder in _fluids)
        {
            var expectsEmpty = holder.Value.Id.Path == "empty";
            if (expectsEmpty == fluidState.IsEmpty) return true;
        }
        return false;
    }

    public override BlockPredicateType Type => BlockPredicateType.MatchingFluids;
}
