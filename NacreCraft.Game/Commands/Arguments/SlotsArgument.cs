using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Commands.Context;
using NacreCraft.Game.World.Inventory;
using NacreCraft.Commands;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//SlotsArgument named slot range, maps to vanilla net.minecraft.commands.arguments.SlotsArgument
public sealed class SlotsArgument : ArgumentType<SlotRange>
{
    private static readonly IReadOnlyList<string> ExampleList =
        new[] { "container.*", "container.5", "weapon" };

    public static readonly DynamicCommandExceptionType ErrorUnknownSlot =
        new(id => new TranslatableMessage("slot.unknown", id));

    public static SlotsArgument Slots() => new();

    public SlotRange Parse(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        var name = reader.String[start..reader.Cursor];
        return SlotRanges.NameToIds(name) is { } ids
            ? SlotRange.Of(name, ids)
            : throw ErrorUnknownSlot.CreateWithContext(reader, name);
    }

    public Task<Suggestions> ListSuggestions<S>(CommandContext<S> context, SuggestionsBuilder builder)
    {
        foreach (var name in SlotRanges.SingleSlotNames()) builder.Add(name);
        return builder.BuildFuture();
    }

    public IReadOnlyList<string> Examples => ExampleList;
}
