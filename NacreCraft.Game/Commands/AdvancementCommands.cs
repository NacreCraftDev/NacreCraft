using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands;

//AdvancementCommands advancement command, maps to vanilla net.minecraft.server.commands.AdvancementCommands
//The advancement subsystem is not ported: there is no advancement tree and no per-player progress store,
//so the tree mirrors vanilla and every branch reports that progress cannot be changed
public static class AdvancementCommands
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("advancement")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("grant")
                .Then(Targets("grant")))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("revoke")
                .Then(Targets("revoke"))));
    }

    //Targets builds the targets subtree shared by grant and revoke, maps to vanilla targets branch
    private static RequiredArgumentBuilder<CommandSourceStack, EntitySelector> Targets(string action)
    {
        var only = RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument(
                "advancement", new ResourceKeyArgument(Registries.ADVANCEMENT.Identifier))
            .Executes(context => Unavailable(context, action))
            .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument(
                    "criterion", StringArgumentType.GreedyString())
                .Executes(context => Unavailable(context, action)));

        return RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument(
                "targets", EntityArgument.Players())
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("only").Then(only))
            .Then(SingleMode("from", action))
            .Then(SingleMode("until", action))
            .Then(SingleMode("through", action))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("everything")
                .Executes(context => Unavailable(context, action)));
    }

    //SingleMode builds a from/until/through branch that takes one advancement, maps to vanilla mode branches
    private static LiteralArgumentBuilder<CommandSourceStack> SingleMode(string mode, string action)
        => LiteralArgumentBuilder<CommandSourceStack>.Literal(mode)
            .Then(RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument(
                    "advancement", new ResourceKeyArgument(Registries.ADVANCEMENT.Identifier))
                .Executes(context => Unavailable(context, action)));

    //Unavailable explains the missing advancement subsystem; the selector and id are already parsed by the tree
    private static int Unavailable(CommandContext<CommandSourceStack> context, string action)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure($"advancement progress is not implemented on this server; nothing was {action}ed");
        return 0;
    }
}
