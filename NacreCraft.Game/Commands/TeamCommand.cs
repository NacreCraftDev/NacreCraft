using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Commands;

//TeamCommand team command, maps to vanilla net.minecraft.server.commands.TeamCommand
//The team model exists in World.Scores but MinecraftServer exposes no Scoreboard instance yet, so the full tree is
//registered and every executor only reports that the scoreboard is not wired to ServerCommandSource
public static class TeamCommand
{
    //ScoreboardUnavailable the single failure every branch reports while the scoreboard is not attached to the server
    private const string ScoreboardUnavailable = "scoreboard is not wired to ServerCommandSource yet";

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var list = LiteralArgumentBuilder<CommandSourceStack>.Literal("list")
            .Executes(Fail)
            .Then(Arg<string>("team", TeamArgument.Team()).Executes(Fail));

        var add = LiteralArgumentBuilder<CommandSourceStack>.Literal("add")
            .Then(Arg<string>("team", StringArgumentType.Word())
                .Executes(Fail)
                .Then(Arg<Component>("displayName", ComponentArgument.TextComponent()).Executes(Fail)));

        var remove = LiteralArgumentBuilder<CommandSourceStack>.Literal("remove")
            .Then(Arg<string>("team", TeamArgument.Team()).Executes(Fail));

        var empty = LiteralArgumentBuilder<CommandSourceStack>.Literal("empty")
            .Then(Arg<string>("team", TeamArgument.Team()).Executes(Fail));

        var join = LiteralArgumentBuilder<CommandSourceStack>.Literal("join")
            .Then(Arg<string>("team", TeamArgument.Team())
                .Executes(Fail)
                .Then(Arg<ScoreHolderArgument.Result>("members", ScoreHolderArgument.ScoreHolders()).Executes(Fail)));

        var leave = LiteralArgumentBuilder<CommandSourceStack>.Literal("leave")
            .Then(Arg<ScoreHolderArgument.Result>("members", ScoreHolderArgument.ScoreHolders()).Executes(Fail));

        var modify = LiteralArgumentBuilder<CommandSourceStack>.Literal("modify");
        RegisterModify(modify);

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("team")
            .Requires(s => s.HasPermission(2))
            .Then(list)
            .Then(add)
            .Then(remove)
            .Then(empty)
            .Then(join)
            .Then(leave)
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("modify")
                .Then(Arg<string>("team", TeamArgument.Team()).Then(modify))));
    }

    //RegisterModify the modify <team> option branches, maps to the vanilla modify subtree
    private static void RegisterModify(LiteralArgumentBuilder<CommandSourceStack> modify)
    {
        modify.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("displayName")
            .Then(Arg<Component>("displayName", ComponentArgument.TextComponent()).Executes(Fail)));

        var color = LiteralArgumentBuilder<CommandSourceStack>.Literal("reset").Executes(Fail);
        modify.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("color")
            .Then(color)
            .Then(Arg<TeamColorArgument.TeamColor>("value", TeamColorArgument.TeamColorArg()).Executes(Fail)));

        modify.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("friendlyFire")
            .Then(Arg<bool>("allowed", BoolArgumentType.Bool()).Executes(Fail)));

        modify.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("seeFriendlyInvisibles")
            .Then(Arg<bool>("allowed", BoolArgumentType.Bool()).Executes(Fail)));

        modify.Then(Visibility("nametagVisibility"));
        modify.Then(Visibility("deathMessageVisibility"));

        var collision = LiteralArgumentBuilder<CommandSourceStack>.Literal("collisionRule");
        collision.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("never").Executes(Fail));
        collision.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("pushOwnTeam").Executes(Fail));
        collision.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("pushOtherTeams").Executes(Fail));
        collision.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("always").Executes(Fail));
        modify.Then(collision);

        modify.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("prefix")
            .Then(Arg<Component>("prefix", ComponentArgument.TextComponent()).Executes(Fail)));

        modify.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("suffix")
            .Then(Arg<Component>("suffix", ComponentArgument.TextComponent()).Executes(Fail)));
    }

    //Visibility the four visibility literals shared by nametagVisibility and deathMessageVisibility
    private static ArgumentBuilder<CommandSourceStack> Visibility(string name)
    {
        return LiteralArgumentBuilder<CommandSourceStack>.Literal(name)
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("never").Executes(Fail))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("hideForOtherTeams").Executes(Fail))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("hideForOwnTeam").Executes(Fail))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("always").Executes(Fail));
    }

    private static RequiredArgumentBuilder<CommandSourceStack, T> Arg<T>(string name, ArgumentType<T> type)
        => RequiredArgumentBuilder<CommandSourceStack, T>.Argument(name, type);

    private static int Fail(CommandContext<CommandSourceStack> context)
    {
        context.GetSource().SendFailure(ScoreboardUnavailable);
        return 0;
    }
}
