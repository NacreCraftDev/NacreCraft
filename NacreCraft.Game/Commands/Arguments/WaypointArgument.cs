using NacreCraft.Commands.Arguments;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//WaypointArgument waypoint operation, maps to vanilla net.minecraft.commands.arguments.WaypointArgument
//The waypoint subsystem is not ported, so the token is returned verbatim
public sealed class WaypointArgument : ArgumentType<string>
{
    public static WaypointArgument Waypoint() => new();

    public string Parse(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        return reader.String[start..reader.Cursor];
    }
}
