using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands;

//DebugConfigCommand debug config command, maps to vanilla net.minecraft.server.commands.DebugConfigCommand
//Switches a player into the configuration protocol stage and shows dialogs there
//Degraded: the configuration packet listener state and dialog packets are not ported, so the tree mirrors
//vanilla and every branch reports that config mode is unavailable
public static class DebugConfigCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("debugconfig")
            .Requires(s => s.HasPermission(3))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("config")
                .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument(
                        "target", EntityArgument.Player())
                    .Executes(Config)))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("unconfig")
                .Then(RequiredArgumentBuilder<CommandSourceStack, Guid>.Argument(
                        "target", UuidArgument.Uuid())
                    .Executes(Unconfig)))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("dialog")
                .Then(RequiredArgumentBuilder<CommandSourceStack, Guid>.Argument(
                        "target", UuidArgument.Uuid())
                    .Then(RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument(
                            "dialog", IdentifierArgument.Id())
                        .Executes(ShowDialog)))));
    }

    private static int Config(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int Unconfig(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int ShowDialog(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //Unavailable explains the missing configuration stage and dialog packets
    private static int Unavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("configuration mode is not implemented on this server; the player was not switched");
        return 0;
    }
}
