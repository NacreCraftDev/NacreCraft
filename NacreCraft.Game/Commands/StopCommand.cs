using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Server;

namespace NacreCraft.Game.Commands;

//StopCommand /stop command, maps to vanilla net.minecraft.server.commands.StopCommand
//The report is sent before Stop because Stop disconnects players
public static class StopCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("stop")
            .Requires(s => s.HasPermission(4))
            .Executes(StopServer));
    }

    private static int StopServer(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendSuccess("stopping the server");
        source.Server.Stop();
        return 1;
    }
}
