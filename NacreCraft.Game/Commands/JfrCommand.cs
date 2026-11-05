using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Server;

namespace NacreCraft.Game.Commands;

//JfrCommand /jfr command, maps to vanilla net.minecraft.server.commands.JfrCommand
//Vanilla starts/stops a Java Flight Recorder profiling session; the .NET runtime has no JFR equivalent,
//so the tree is registered for parity and both branches report that profiling is unavailable
public static class JfrCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("jfr")
            .Requires(s => s.HasPermission(4))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("start")
                .Executes(context => Unavailable(context, "start")))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("stop")
                .Executes(context => Unavailable(context, "stop"))));
    }

    private static int Unavailable(CommandContext<CommandSourceStack> context, string action)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure($"JFR profiling is not available: cannot {action} a recording on this runtime");
        return 0;
    }
}
