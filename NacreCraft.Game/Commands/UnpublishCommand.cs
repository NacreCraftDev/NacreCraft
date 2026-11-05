using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Server;

namespace NacreCraft.Game.Commands;

//UnpublishCommand /unpublish command, maps to vanilla net.minecraft.server.commands.UnpublishCommand
//Vanilla closes the LAN session opened by /publish; this server never publishes, so the command always reports not published
public static class UnpublishCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("unpublish")
            .Requires(s => s.HasPermission(4))
            .Executes(Unpublish));
    }

    private static int Unpublish(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("the server is not published");
        return 0;
    }
}
