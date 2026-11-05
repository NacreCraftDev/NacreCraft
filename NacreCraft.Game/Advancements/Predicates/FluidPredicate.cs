using NacreCraft.Codec;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.Advancements.Predicates;

//FluidPredicate fluid state predicate, maps to vanilla net.minecraft.advancements.predicates.FluidPredicate
//Checks the fluid type set and the block state properties at the position
//NC's FluidState only carries level and falling, so the state properties are checked against the block state when supplied
public sealed record FluidPredicate(
    Optional<HolderSet<Fluid>> Fluids,
    Optional<StatePropertiesPredicate> Properties)
{
    //Codec persistence codec, field names fluids/state, maps to vanilla CODEC
    public static readonly Codec<FluidPredicate> Codec = RecordCodecBuilder.Of2(
        HolderSetCodecs.FluidSet.OptionalFieldOf("fluids")
            .ForGetter((FluidPredicate predicate) => predicate.Fluids),
        StatePropertiesPredicate.Codec.OptionalFieldOf("state")
            .ForGetter((FluidPredicate predicate) => predicate.Properties),
        (fluids, properties) => new FluidPredicate(fluids, properties));

    //Matches checks the fluid type and the block state properties at the position, maps to vanilla matches
    public bool Matches(ServerLevel level, BlockPos pos)
    {
        if (!level.IsLoaded(pos)) return false;
        var state = level.GetFluidState(pos);
        if (Fluids.IsPresent && !ContainsFluid(Fluids.Get(), state.Type)) return false;
        var blockState = level.GetBlockState(pos);
        return !Properties.IsPresent
            || (blockState.HasValue && Properties.Get().Matches(blockState.Value));
    }

    //Matches checks the fluid type; state properties need the block state at the position, maps to vanilla matches
    public bool Matches(FluidState state, BlockState? blockState = null)
    {
        if (Fluids.IsPresent && !ContainsFluid(Fluids.Get(), state.Type)) return false;
        return !Properties.IsPresent
            || (blockState.HasValue && Properties.Get().Matches(blockState.Value));
    }

    private static bool ContainsFluid(HolderSet<Fluid> set, Fluid fluid)
    {
        foreach (var holder in set)
            if (ReferenceEquals(holder.Value, fluid))
                return true;
        return false;
    }

    public sealed class Builder
    {
        private Optional<HolderSet<Fluid>> _fluids = Optional<HolderSet<Fluid>>.Empty();
        private Optional<StatePropertiesPredicate> _properties = Optional<StatePropertiesPredicate>.Empty();

        public static Builder Fluid() => new();

        public Builder Of(HolderSet<Fluid> fluids)
        {
            _fluids = Optional<HolderSet<Fluid>>.Of(fluids);
            return this;
        }

        public Builder SetProperties(StatePropertiesPredicate properties)
        {
            _properties = Optional<StatePropertiesPredicate>.Of(properties);
            return this;
        }

        public FluidPredicate Build() => new(_fluids, _properties);
    }
}
