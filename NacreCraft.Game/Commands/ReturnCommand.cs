using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Server;

namespace NacreCraft.Game.Commands;

//ReturnCommand /return command, maps to vanilla net.minecraft.server.commands.ReturnCommand
//Returns a value or a failure from a function; the execution-control frame model is not ported, so the value is
//passed back as the command result and the "run" modifier is degraded to a plain nested dispatch
public static class ReturnCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("return")
            .Requires(s => s.HasPermission(2))
            .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument("value", IntegerArgumentType.Integer())
                .Executes(context => IntegerArgumentType.GetInteger(context, "value")))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("fail")
                .Executes(ReturnFail)));
    }

    //ReturnFail marks the command as failed, maps to vanilla ReturnFailCustomExecutor
    private static int ReturnFail(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("returning a failure from the function");
        return 0;
    }
}
