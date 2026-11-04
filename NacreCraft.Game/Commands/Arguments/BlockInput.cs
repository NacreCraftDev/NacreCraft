using NacreCraft.Game.Server;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.Commands.Arguments;

//BlockInput a parsed block state together with the properties written explicitly and the optional block entity tag
//maps to vanilla net.minecraft.commands.arguments.blocks.BlockInput
//The properties set is what the command wrote, so a test only requires those to match and ignores the other properties
public sealed class BlockInput
{
    public BlockInput(BlockState state, IReadOnlySet<PropertyBase> definedProperties, CompoundTag? tag)
    {
        State = state;
        DefinedProperties = definedProperties;
        Tag = tag;
    }

    //State the block state after the listed properties were applied
    public BlockState State { get; }

    //DefinedProperties the properties the input named explicitly
    public IReadOnlySet<PropertyBase> DefinedProperties { get; }

    //Tag the block entity tag, written after the state in the argument syntax
    public CompoundTag? Tag { get; }

    //Test whether the world block matches this input, maps to vanilla test
    public bool Test(BlockInWorld blockInWorld)
    {
        var state = blockInWorld.State;
        if (state is null || !state.Value.Is(State.Owner)) return false;
        foreach (var property in DefinedProperties)
        {
            if (!Equals(GetPropertyValue(state.Value, property), GetPropertyValue(State, property))) return false;
        }
        if (Tag is null) return true;
        var entity = blockInWorld.BlockEntities?.Get(blockInWorld.Pos);
        if (entity is null) return false;
        return BlockPredicateArgument.MatchesTag(Tag, entity.SaveWithFullMetadata());
    }

    public bool Test(PersistentServerLevel level, BlockPos pos) => Test(new BlockInWorld(level, pos, null));

    //Place writes the state and then the block entity tag, maps to vanilla place
    //The neighbour shape update vanilla applies for the non-skip flags is left to the caller, NC places through its own broadcast chain
    public bool Place(PersistentServerLevel level, BlockEntityManager blockEntities, BlockPos pos, int update)
    {
        var affected = level.SetBlock(pos, State, update);
        if (Tag is null) return affected;
        var entity = blockEntities.Get(pos);
        if (entity is null) return affected;
        entity.LoadAdditional(Tag);
        return true;
    }

    private static object? GetPropertyValue(BlockState state, PropertyBase property)
    {
        foreach (var value in state.GetValues())
            if (ReferenceEquals(value.Property, property))
                return value.Value;
        return null;
    }
}
