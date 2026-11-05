using NacreCraft.Commands.Arguments;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ObjectiveCriteriaArgument scoreboard criterion name, maps to vanilla net.minecraft.commands.arguments.ObjectiveCriteriaArgument
//The criterion registry is not ported, so any identifier is accepted
public sealed class ObjectiveCriteriaArgument : ArgumentType<string>
{
    public static ObjectiveCriteriaArgument Criteria() => new();

    public string Parse(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        return reader.String[start..reader.Cursor];
    }
}
