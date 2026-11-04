using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Registry.State;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//BlockStateArgument block state argument, maps to vanilla net.minecraft.commands.arguments.blocks.BlockStateArgument
//Syntax: namespace:path[property=value,...]{block entity nbt}; both sections are optional, the parsing itself lives in BlockStateParser
public sealed class BlockStateArgument : ArgumentType<BlockInput>
{
    public static BlockStateArgument Block() => new();

    public BlockInput Parse(StringReader reader)
    {
        var result = BlockStateParser.ParseForBlock(reader, allowNbt: true);
        return new BlockInput(result.BlockState, new HashSet<PropertyBase>(result.Properties.Keys), result.Nbt);
    }

    //ListSuggestions suggests the block id, a property name or a property value depending on where the cursor is
    public Task<Suggestions> ListSuggestions<S>(CommandContext<S> context, SuggestionsBuilder builder)
        => BlockStateParser.FillSuggestions(builder, forTesting: false, allowNbt: true);

    public IReadOnlyList<string> Examples => new[] { "minecraft:stone", "stone[foo=bar]", "foo{bar=baz}" };
}
