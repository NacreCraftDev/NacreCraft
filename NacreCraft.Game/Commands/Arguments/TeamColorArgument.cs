using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//TeamColorArgument scoreboard team color, maps to vanilla net.minecraft.commands.arguments.TeamColorArgument
//Teams are not ported, so the color is kept as a name/id pair
public sealed class TeamColorArgument : ArgumentType<TeamColorArgument.TeamColor>
{
    public static readonly DynamicCommandExceptionType ErrorInvalidColor =
        new(value => new TranslatableMessage("argument.team_color.invalid", value));

    public static readonly IReadOnlyList<TeamColor> Values = new[]
    {
        new TeamColor(0, "white"), new TeamColor(1, "orange"), new TeamColor(2, "magenta"),
        new TeamColor(3, "light_blue"), new TeamColor(4, "yellow"), new TeamColor(5, "lime"),
        new TeamColor(6, "pink"), new TeamColor(7, "gray"), new TeamColor(8, "light_gray"),
        new TeamColor(9, "cyan"), new TeamColor(10, "purple"), new TeamColor(11, "blue"),
        new TeamColor(12, "brown"), new TeamColor(13, "green"), new TeamColor(14, "red"),
        new TeamColor(15, "black"),
    };

    public static TeamColorArgument TeamColorArg() => new();

    public TeamColor Parse(StringReader reader)
    {
        var name = reader.ReadUnquotedString();
        foreach (var color in Values)
            if (color.Name == name) return color;
        throw ErrorInvalidColor.CreateWithContext(reader, name);
    }

    public IReadOnlyList<string> Examples => new[] { "red", "blue" };

    public sealed record TeamColor(int Id, string Name);
}
