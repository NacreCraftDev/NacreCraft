using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ScoreboardSlotArgument display slot, maps to vanilla net.minecraft.commands.arguments.ScoreboardSlotArgument
//The scoreboard is not ported, so only the display slot enum is kept
public sealed class ScoreboardSlotArgument : ArgumentType<ScoreboardSlotArgument.DisplaySlot>
{
    public static readonly DynamicCommandExceptionType ErrorInvalidSlot =
        new(value => new TranslatableMessage("argument.scoreboard_slot.invalid", value));

    private static readonly string[] Names = { "list", "sidebar", "below_name", "team_color" };

    public static ScoreboardSlotArgument DisplaySlotArgument() => new();

    public DisplaySlot Parse(StringReader reader)
    {
        var name = reader.ReadUnquotedString();
        for (var i = 0; i < Names.Length; i++)
            if (Names[i] == name) return (DisplaySlot)i;
        throw ErrorInvalidSlot.CreateWithContext(reader, name);
    }

    public IReadOnlyList<string> Examples => Names;

    public enum DisplaySlot
    {
        List,
        Sidebar,
        BelowName,
        TeamColor,
    }
}
