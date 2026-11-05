using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands;

//BossBarCommands custom boss bar command, maps to vanilla net.minecraft.server.commands.BossBarCommands
//Degraded: ServerBossEvent exists, but the server has no CustomBossEvents store and no boss event packet
//wrapper, so bars could be kept in memory without ever reaching clients. The tree mirrors vanilla and
//every branch reports that custom boss bars are unavailable
public static class BossBarCommands
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("bossbar")
            .Requires(s => s.HasPermission(2))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("add")
                .Then(Id()
                    .Then(RequiredArgumentBuilder<CommandSourceStack, Component>.Argument(
                            "name", ComponentArgument.TextComponent())
                        .Executes(CreateBar))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("remove")
                .Then(Id().Executes(RemoveBar)))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("list")
                .Executes(ListBars))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("set")
                .Then(SetTree()))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("get")
                .Then(GetTree())));
    }

    //Id the id argument shared by the branches; each caller builds its own node
    private static RequiredArgumentBuilder<CommandSourceStack, Identifier> Id()
        => RequiredArgumentBuilder<CommandSourceStack, Identifier>.Argument("id", IdentifierArgument.Id());

    //SetTree builds the set subtree, maps to vanilla set branch
    private static RequiredArgumentBuilder<CommandSourceStack, Identifier> SetTree()
    {
        var node = Id()
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("name")
                .Then(RequiredArgumentBuilder<CommandSourceStack, Component>.Argument(
                        "name", ComponentArgument.TextComponent())
                    .Executes(SetName)));

        var color = LiteralArgumentBuilder<CommandSourceStack>.Literal("color");
        foreach (var name in new[] { "pink", "blue", "red", "green", "yellow", "purple", "white" })
            color.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal(name).Executes(SetColor));
        node.Then(color);

        var style = LiteralArgumentBuilder<CommandSourceStack>.Literal("style");
        foreach (var name in new[] { "progress", "notched_6", "notched_10", "notched_12", "notched_20" })
            style.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal(name).Executes(SetStyle));
        node.Then(style);

        node.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("value")
            .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                    "value", IntegerArgumentType.Integer(0))
                .Executes(SetValue)));
        node.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("max")
            .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                    "max", IntegerArgumentType.Integer(1))
                .Executes(SetMax)));
        node.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("visible")
            .Then(RequiredArgumentBuilder<CommandSourceStack, bool>.Argument("visible", BoolArgumentType.Bool())
                .Executes(SetVisible)));
        node.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("players")
            .Executes(SetPlayers)
            .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument(
                    "targets", EntityArgument.Players())
                .Executes(SetPlayers)));
        return node;
    }

    //GetTree builds the get subtree, maps to vanilla get branch
    private static RequiredArgumentBuilder<CommandSourceStack, Identifier> GetTree()
    {
        var node = Id();
        foreach (var name in new[] { "value", "max", "visible", "players" })
            node.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal(name).Executes(GetValue));
        return node;
    }

    private static int CreateBar(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int RemoveBar(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int ListBars(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetName(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetColor(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetStyle(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetValue(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetMax(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetVisible(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int SetPlayers(CommandContext<CommandSourceStack> context) => Unavailable(context);

    private static int GetValue(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //Unavailable explains the missing custom boss bar store and network sync
    private static int Unavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("custom boss bars are not implemented on this server; no boss bar was changed");
        return 0;
    }
}
