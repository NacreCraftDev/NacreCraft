using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;

namespace NacreCraft.Game.Commands;

//WardenSpawnTrackerCommand warden spawn tracker command, maps to vanilla net.minecraft.server.commands.WardenSpawnTrackerCommand
//warden_spawn_tracker clear|set <warning_level> resets or sets the executor's warden warning level
//Degraded: the player has no warden spawn tracker component, so the tree is registered and execution reports that the tracker is unavailable
public static class WardenSpawnTrackerCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("warden_spawn_tracker")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("clear")
                .Executes(ResetTracker))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("set")
                .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                        "warning_level", IntegerArgumentType.Integer(0, 4))
                    .Executes(SetWarningLevel))));
    }

    //ResetTracker would clear the warning level, maps to vanilla resetTracker
    private static int ResetTracker(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //SetWarningLevel would write the warning level, maps to vanilla setWarningLevel
    private static int SetWarningLevel(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //Unavailable resolves the executor so console use still reports the player requirement, then explains the missing subsystem
    private static int Unavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        _ = source.PlayerOrThrow;
        source.SendFailure("warden spawn trackers are not implemented on this server; nothing was changed");
        return 0;
    }
}
