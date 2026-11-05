using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands;

//DialogCommand dialog command, maps to vanilla net.minecraft.server.commands.DialogCommand
//Degraded: the dialog registry and the show/clear dialog packets are not ported, and there is no
//ResourceOrIdArgument<Dialog> because BuiltInRegistries has no dialog registry, so the dialog id is
//parsed with IdentifierArgument. The tree mirrors vanilla and execution reports that dialogs are unavailable
public static class DialogCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("dialog")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("show")
                .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument(
                        "targets", EntityArgument.Players())
                    .Then(RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument(
                            "dialog", IdentifierArgument.Id())
                        .Executes(ShowDialog))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("clear")
                .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument(
                        "targets", EntityArgument.Players())
                    .Executes(ClearDialog))));
    }

    private static int ShowDialog(CommandContext<CommandSourceStack> context) => Unavailable(context, "show");

    private static int ClearDialog(CommandContext<CommandSourceStack> context) => Unavailable(context, "clear");

    //Unavailable explains the missing dialog subsystem and packets
    private static int Unavailable(CommandContext<CommandSourceStack> context, string action)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure($"dialogs are not implemented on this server; cannot {action} a dialog");
        return 0;
    }
}
