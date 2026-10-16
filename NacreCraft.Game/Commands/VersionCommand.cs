using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;

namespace NacreCraft.Game.Commands;

//VersionCommand version command, maps to vanilla net.minecraft.server.commands.VersionCommand
//Reports the server assembly version
public static class VersionCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("version")
            .Executes(context =>
            {
                var source = (ServerCommandSource)context.GetSource();
                var version = typeof(VersionCommand).Assembly.GetName().Version?.ToString() ?? "unknown";
                source.SendSuccess($"this server is running NacreCraft {version}");
                return 1;
            }));
    }
}
