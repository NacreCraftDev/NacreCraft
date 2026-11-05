using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;

namespace NacreCraft.Game.Commands;

//ChaseCommand chase command, maps to vanilla net.minecraft.server.commands.ChaseCommand
//Chase is a debug tool that mirrors the lead server's player positions to following servers over TCP
//Degraded: ChaseClient/ChaseServer are not ported, so the tree mirrors vanilla and every branch reports that chase is unavailable
public static class ChaseCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("chase")
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("follow")
                .Executes(context => Follow(context, "localhost", 10000))
                .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument(
                        "host", StringArgumentType.String())
                    .Executes(context => Follow(context,
                        StringArgumentType.GetString(context, "host"), 10000))
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                            "port", IntegerArgumentType.Integer(1, 65535))
                        .Executes(context => Follow(context,
                            StringArgumentType.GetString(context, "host"),
                            IntegerArgumentType.GetInteger(context, "port"))))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("lead")
                .Executes(context => Lead(context, "0.0.0.0", 10000))
                .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument(
                        "bind_address", StringArgumentType.String())
                    .Executes(context => Lead(context,
                        StringArgumentType.GetString(context, "bind_address"), 10000))
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                            "port", IntegerArgumentType.Integer(1024, 65535))
                        .Executes(context => Lead(context,
                            StringArgumentType.GetString(context, "bind_address"),
                            IntegerArgumentType.GetInteger(context, "port"))))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("stop")
                .Executes(Stop)));
    }

    private static int Follow(CommandContext<CommandSourceStack> context, string host, int port)
        => Unavailable(context, $"follow {host}:{port}");

    private static int Lead(CommandContext<CommandSourceStack> context, string bindAddress, int port)
        => Unavailable(context, $"lead {bindAddress}:{port}");

    private static int Stop(CommandContext<CommandSourceStack> context) => Unavailable(context, "stop");

    //Unavailable explains the missing chase transport; the host/port are already parsed by the tree
    private static int Unavailable(CommandContext<CommandSourceStack> context, string action)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure($"chase is not implemented on this server; cannot {action}");
        return 0;
    }
}
