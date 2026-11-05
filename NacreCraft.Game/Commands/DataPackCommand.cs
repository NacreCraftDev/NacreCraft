using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Commands;

//DataPackCommand datapack command, maps to vanilla net.minecraft.server.commands.DataPackCommand
//The server has no PackRepository and no reload-packs entry point yet, so the vanilla tree is registered and every executor
//reports that pack management is unavailable; create is not written to disk either
public static class DataPackCommand
{
    private const string Unavailable = "datapack pack repository is not wired to the server yet";

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var enable = LiteralArgumentBuilder<CommandSourceStack>.Literal("enable")
            .Then(Arg<string>("name", StringArgumentType.String())
                .Executes(Fail)
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("after")
                    .Then(Arg<string>("existing", StringArgumentType.String()).Executes(Fail)))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("before")
                    .Then(Arg<string>("existing", StringArgumentType.String()).Executes(Fail)))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("last").Executes(Fail))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("first").Executes(Fail)));

        var disable = LiteralArgumentBuilder<CommandSourceStack>.Literal("disable")
            .Then(Arg<string>("name", StringArgumentType.String()).Executes(Fail));

        var list = LiteralArgumentBuilder<CommandSourceStack>.Literal("list")
            .Executes(Fail)
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("available").Executes(Fail))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("enabled").Executes(Fail));

        var create = LiteralArgumentBuilder<CommandSourceStack>.Literal("create")
            .Requires(s => s.HasPermission(4))
            .Then(Arg<string>("id", StringArgumentType.String())
                .Then(Arg<Component>("description", ComponentArgument.TextComponent()).Executes(Fail)));

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("datapack")
            .Requires(s => s.HasPermission(2))
            .Then(enable)
            .Then(disable)
            .Then(list)
            .Then(create));
    }

    private static RequiredArgumentBuilder<CommandSourceStack, T> Arg<T>(string name, ArgumentType<T> type)
        => RequiredArgumentBuilder<CommandSourceStack, T>.Argument(name, type);

    private static int Fail(CommandContext<CommandSourceStack> context)
    {
        context.GetSource().SendFailure(Unavailable);
        return 0;
    }
}
