using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Server;

namespace NacreCraft.Game.Commands;

//PublishCommand /publish command, maps to vanilla net.minecraft.server.commands.PublishCommand
//Vanilla opens the integrated server to the LAN; the dedicated server here always listens on server-port,
//so the tree is registered for parity and reports that publishing is unavailable
public static class PublishCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("publish")
            .Requires(s => s.HasPermission(4))
            .Executes(Unavailable)
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("allowCommands")
                .Executes(Unavailable)));
    }

    private static int Unavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("publishing to the LAN is not supported; this server always listens on its configured port");
        return 0;
    }
}
