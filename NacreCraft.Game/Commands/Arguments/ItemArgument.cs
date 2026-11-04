using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ItemInput item argument parse result, maps to vanilla ItemInput
//Carries the item itself, the identifier and the component patch; the identifier is used for the receipt display name
public sealed record ItemInput(Item Item, Identifier Id, DataComponentPatch Components);

//ItemArgument item argument, maps to vanilla net.minecraft.commands.arguments.item.ItemArgument
//The syntax lives in ItemParser; this class only wires it into the command tree
//The network id goes through item_stack, matching vanilla give's item argument type
public sealed class ItemArgument : ArgumentType<ItemInput>
{
    private readonly ItemParser _parser = new();

    public static ItemArgument Item() => new();

    public ItemInput Parse(StringReader reader) => _parser.Parse(reader);

    //GetItemInput gets the parsed item and identifier
    public static ItemInput GetItemInput(CommandContext<CommandSourceStack> context, string name)
        => context.GetArgument<ItemInput>(name);

    //GetItem gets the parsed item
    public static Item GetItem(CommandContext<CommandSourceStack> context, string name)
        => GetItemInput(context, name).Item;

    public Task<Suggestions> ListSuggestions<S>(CommandContext<S> context, SuggestionsBuilder builder)
        => _parser.FillSuggestions(builder);

    public IReadOnlyList<string> Examples => ItemParser.ExamplesList;
}
