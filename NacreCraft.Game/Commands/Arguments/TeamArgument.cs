using NacreCraft.Commands.Arguments;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//TeamArgument scoreboard team name, maps to vanilla net.minecraft.commands.arguments.TeamArgument
//Teams are not ported, so the name is parsed and returned unresolved
public sealed class TeamArgument : ArgumentType<string>
{
    public static TeamArgument Team() => new();

    public string Parse(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        return reader.String[start..reader.Cursor];
    }
}
