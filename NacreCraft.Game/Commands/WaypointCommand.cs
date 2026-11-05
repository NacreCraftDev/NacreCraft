using NacreCraft.Codec;
using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Server.Waypoints;
using NacreCraft.Game.World.Scores;
using NacreCraft.Game.World.Waypoints;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Commands;

//WaypointCommand waypoint command, maps to vanilla net.minecraft.server.commands.WaypointCommand
public static class WaypointCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("waypoint")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("list")
                .Executes(ListWaypoints))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("modify")
                .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument(
                        "waypoint", EntityArgument.Entity())
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

    //ListWaypoints reports the transmitters of the source level, maps to vanilla listWaypoints
    private static int ListWaypoints(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        if (source.Level is not PersistentServerLevel level) return 0;
        var waypoints = WaypointManagers.Get(level).Transmitters();
        var dimension = level.Dimension.ToString();
        if (waypoints.Count == 0)
        {
            source.SendSuccess($"No waypoints in {dimension}");
            return 0;
        }
        var names = string.Join(", ", waypoints.Select(waypoint =>
            waypoint is NacreCraft.Registry.Entity entity ? entity.Uuid.ToString() : waypoint.ToString()));
        source.SendSuccess($"{waypoints.Count} waypoint(s) in {dimension}: {names}");
        return waypoints.Count;
    }

    //SetColor applies a named team color, maps to vanilla setWaypointColor(TeamColor)
    private static int SetColor(CommandContext<CommandSourceStack> context)
    {
        var source = (ServerCommandSource)context.GetSource();
        var waypoint = WaypointArgument.GetWaypoint(context, "waypoint");
        var argument = context.GetArgument<TeamColorArgument.TeamColor>("color");
        var color = TeamColorExtensions.ByName(argument.Name);
        var rgb = color is { } named ? named.GetTextColor().Value : argument.Id;
        MutateIcon(source, waypoint, icon => icon.Color = Optional<int>.Of(rgb));
        source.SendSuccess($"Waypoint color set to {argument.Name}");
        return 0;
    }

    //SetHexColor applies an RGB hex color, maps to vanilla setWaypointColor(Integer)
    private static int SetHexColor(CommandContext<CommandSourceStack> context)
    {
        var source = (ServerCommandSource)context.GetSource();
        var waypoint = WaypointArgument.GetWaypoint(context, "waypoint");
        var color = context.GetArgument<int>("color") & 0xFFFFFF;
        MutateIcon(source, waypoint, icon => icon.Color = Optional<int>.Of(color));
        source.SendSuccess($"Waypoint color set to #{color:X6}");
        return 0;
    }

    //ResetColor clears the color override, maps to vanilla resetWaypointColor
    private static int ResetColor(CommandContext<CommandSourceStack> context)
    {
        var source = (ServerCommandSource)context.GetSource();
        var waypoint = WaypointArgument.GetWaypoint(context, "waypoint");
        MutateIcon(source, waypoint, icon => icon.Color = Optional<int>.Empty());
        source.SendSuccess("Waypoint color reset");
        return 0;
    }

    //SetStyle changes the icon style, maps to vanilla setWaypointStyle
    private static int SetStyle(CommandContext<CommandSourceStack> context)
    {
        var source = (ServerCommandSource)context.GetSource();
        var waypoint = WaypointArgument.GetWaypoint(context, "waypoint");
        var style = ResourceKey<WaypointStyleAsset>.Create(
            WaypointStyleAssets.ROOT_ID, context.GetArgument<Identifier>("style"));
        MutateIcon(source, waypoint, icon => icon.Style = style);
        source.SendSuccess("Waypoint style set");
        return 0;
    }

    //ResetStyle restores the default style, maps to vanilla setWaypointStyle(DEFAULT)
    private static int ResetStyle(CommandContext<CommandSourceStack> context)
    {
        var source = (ServerCommandSource)context.GetSource();
        var waypoint = WaypointArgument.GetWaypoint(context, "waypoint");
        MutateIcon(source, waypoint, icon => icon.Style = WaypointStyleAssets.DEFAULT);
        source.SendSuccess("Waypoint style reset");
        return 0;
    }

    //MutateIcon changes an icon and re-publishes the source, maps to vanilla mutateIcon
    //The manager has to forget the old icon before it mutates, because existing connections still hold the old one
    private static void MutateIcon(ServerCommandSource source, WaypointTransmitter waypoint, Action<Waypoint.Icon> iconConsumer)
    {
        if (source.Level is not PersistentServerLevel level) return;
        var manager = WaypointManagers.Get(level);
        manager.UntrackWaypoint(waypoint);
        iconConsumer(waypoint.WaypointIcon());
        manager.TrackWaypoint(waypoint);
    }
}
