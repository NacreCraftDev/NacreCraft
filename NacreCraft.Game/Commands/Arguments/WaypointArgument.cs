using NacreCraft.Commands;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Game.World.Waypoints;

namespace NacreCraft.Game.Commands.Arguments;

//WaypointArgument resolves a command target to a waypoint transmitter, maps to vanilla net.minecraft.commands.arguments.WaypointArgument
//Vanilla shares the entity selector argument; this helper runs the same single-entity resolution and rejects entities without the interface
public static class WaypointArgument
{
    public static readonly SimpleCommandExceptionType ErrorNotAWaypoint =
        new(new TranslatableMessage("argument.waypoint.invalid"));

    public static WaypointTransmitter GetWaypoint(CommandContext<CommandSourceStack> context, string name)
    {
        var entity = EntityArgument.GetSingleTarget(context, name).Source;
        return entity as WaypointTransmitter ?? throw ErrorNotAWaypoint.Create();
    }
}
