using NacreCraft.Commands.Arguments;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ObjectiveArgument scoreboard objective name, maps to vanilla net.minecraft.commands.arguments.ObjectiveArgument
//The scoreboard subsystem is not ported, so the name is parsed and returned without resolution
public sealed class ObjectiveArgument : ArgumentType<string>
{
    public static ObjectiveArgument Objective() => new();

    public string Parse(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        return reader.String[start..reader.Cursor];
    }
}
