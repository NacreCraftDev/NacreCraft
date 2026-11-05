using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands;

//WaypointCommand waypoint command, maps to vanilla net.minecraft.server.commands.WaypointCommand
//Degraded: the waypoint manager and waypoint style assets are not ported, and WaypointArgument only
//returns the token verbatim, so the tree mirrors vanilla and every branch reports that waypoints are unavailable
public static class WaypointCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("waypoint")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("list")
                .Executes(ListWaypoints))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("modify")
                .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument(
                        "waypoint", WaypointArgument.Waypoint())
                    .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("color")
                        .Then(RequiredArgumentBuilder<CommandSourceStack, TeamColorArgument.TeamColor>.Argument(
                                "color", TeamColorArgument.TeamColorArg())
                            .Executes(SetColor))
                        .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("hex")
                            .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                                    "color", HexColorArgument.HexColor())
                                .Executes(SetHexColor))))
                    .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("reset")
                        .Executes(ResetColor))
                    .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("style")
                        .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("reset")
                            .Executes(ResetStyle))
                        .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("set")
                            .Then(RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument(
                                    "style", IdentifierArgument.Id())
                                .Executes(SetStyle)))))));
    }

    private static int ListWaypoints(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetColor(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetHexColor(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int ResetColor(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int ResetStyle(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetStyle(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //Unavailable explains the missing waypoint manager
    private static int Unavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("waypoints are not implemented on this server; no waypoint was changed");
        return 0;
    }
}
