using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Server;

namespace NacreCraft.Game.Commands;

//ReloadCommand /reload command, maps to vanilla net.minecraft.server.commands.ReloadCommand
//Vanilla reloads the pack repository and data-driven registries; the resource pack system is not ported,
//so the command reports that a reload is unsupported instead of silently doing nothing
public static class ReloadCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("reload")
            .Requires(s => s.HasPermission(2))
            .Executes(Reload));
    }

    private static int Reload(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("resource reload is not implemented; restart the server to apply data changes");
        return 0;
    }
}
