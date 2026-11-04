using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Game.Server;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//BlockInWorld predicate evaluation context, maps to vanilla BlockInWorld
//The block entity is not on the level; the caller passes it in for the nbt predicate
public sealed record BlockInWorld(PersistentServerLevel Level, BlockPos Pos, BlockEntityManager? BlockEntities)
{
    public BlockState? State => Level.GetBlockState(Pos);
}

//BlockPredicateArgument block predicate argument, maps to vanilla net.minecraft.commands.arguments.blocks.BlockPredicateArgument
//Syntax: <block> | #<tag>, optionally followed by [property=value,...] and {block entity nbt}
//The property section only requires the listed properties to match and ignores the rest; the nbt section matches partially; given tags must be present in the actual tags
public sealed class BlockPredicateArgument : ArgumentType<Predicate<BlockInWorld>>
{
    public static BlockPredicateArgument BlockPredicate() => new();

    public Predicate<BlockInWorld> Parse(StringReader reader)
    {
        var result = BlockStateParser.ParseForTesting(reader, allowNbt: true);
        if (result.Tag is { } tag) return BuildTagPredicate(tag);
        var block = result.Block!;
        var input = new BlockInput(block.BlockState, new HashSet<PropertyBase>(block.Properties.Keys), block.Nbt);
        return input.Test;
    }

    //BuildTagPredicate matches the block tag first, then the vague properties and finally the nbt
    private static Predicate<BlockInWorld> BuildTagPredicate(BlockStateParser.TagResult result) => world =>
    {
        var state = world.State;
        if (state is null || !state.Value.Is(result.Tag)) return false;
        foreach (var (name, value) in result.VagueProperties)
        {
            var matched = false;
            foreach (var propertyValue in state.Value.GetValues())
            {
                if (propertyValue.Property.Name != name) continue;
                matched = propertyValue.ValueName == value;
                break;
            }
            if (!matched) return false;
        }
        if (result.Nbt is null) return true;
        var entity = world.BlockEntities?.Get(world.Pos);
        if (entity is null) return false;
        return MatchesTag(result.Nbt, entity.SaveWithFullMetadata());
    };

    //MatchesTag partial match: every given entry must exist in the actual tag and be equal, maps to vanilla NbtUtils.compareNbt(..., true)
    //Lists require equal length and element-wise matching; other types use equality
    internal static bool MatchesTag(CompoundTag expected, CompoundTag actual)
    {
        foreach (var key in expected.Keys)
        {
            if (!actual.TryGetTag(key, out var actualValue)) return false;
            if (!MatchesTag(expected.Get<Tag>(key)!, actualValue)) return false;
        }
        return true;
    }

    private static bool MatchesTag(Tag expected, Tag actual)
    {
        if (expected is CompoundTag expectedCompound)
            return actual is CompoundTag actualCompound && MatchesTag(expectedCompound, actualCompound);
        if (expected is ListTag expectedList)
        {
            if (actual is not ListTag actualList || actualList.Count != expectedList.Count) return false;
            for (var i = 0; i < expectedList.Count; i++)
            {
                if (!MatchesTag(expectedList[i], actualList[i])) return false;
            }
            return true;
        }
        return expected.Equals(actual);
    }

    //GetBlockPredicate gets the parsed predicate
    public static Predicate<BlockInWorld> GetBlockPredicate(CommandContext<CommandSourceStack> context, string name)
        => context.GetArgument<Predicate<BlockInWorld>>(name);

    //ListSuggestions suggests blocks, tags, property names and property values for the current cursor
    public Task<Suggestions> ListSuggestions<S>(CommandContext<S> context, SuggestionsBuilder builder)
        => BlockStateParser.FillSuggestions(builder, forTesting: true, allowNbt: true);

    public IReadOnlyList<string> Examples => new[] { "stone", "minecraft:stone", "stone[foo=bar]", "#stone", "#stone[foo=bar]{baz=nbt}" };
}
