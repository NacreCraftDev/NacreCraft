using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Commands;

//ScoreboardCommand scoreboard command, maps to vanilla net.minecraft.server.commands.ScoreboardCommand
//The scoreboard model exists in World.Scores but MinecraftServer exposes no Scoreboard instance yet, so the full
//objectives/players tree is registered and every executor only reports that the scoreboard is not wired to ServerCommandSource
public static class ScoreboardCommand
{
    //ScoreboardUnavailable the single failure every branch reports while the scoreboard is not attached to the server
    private const string ScoreboardUnavailable = "scoreboard is not wired to ServerCommandSource yet";

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var objectives = LiteralArgumentBuilder<CommandSourceStack>.Literal("objectives");
        RegisterObjectives(objectives);

        var players = LiteralArgumentBuilder<CommandSourceStack>.Literal("players");
        RegisterPlayers(players);

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("scoreboard")
            .Requires(s => s.HasPermission(2))
            .Then(objectives)
            .Then(players));
    }

    //RegisterObjectives objectives list/add/modify/remove/setdisplay, maps to the vanilla objectives branch
    private static void RegisterObjectives(LiteralArgumentBuilder<CommandSourceStack> objectives)
    {
        objectives.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("list")
            .Executes(Fail));

        //add <objective> <criteria> [<displayName>]
        var addDisplayName = Arg<Component>("displayName", ComponentArgument.TextComponent()).Executes(Fail);
        var addCriteria = Arg<string>("criteria", ObjectiveCriteriaArgument.Criteria())
            .Executes(Fail)
            .Then(addDisplayName);
        var addObjective = Arg<string>("objective", StringArgumentType.Word())
            .Then(addCriteria);
        objectives.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("add").Then(addObjective));

        //modify <objective> [displayname|rendertype|displayautoupdate|numberformat]
        var modify = LiteralArgumentBuilder<CommandSourceStack>.Literal("modify");

        modify.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("displayname")
            .Then(Arg<Component>("displayName", ComponentArgument.TextComponent()).Executes(Fail)));

        var renderType = LiteralArgumentBuilder<CommandSourceStack>.Literal("rendertype");
        renderType.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("integer").Executes(Fail));
        renderType.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("hearts").Executes(Fail));
        modify.Then(renderType);

        modify.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("displayautoupdate")
            .Then(Arg<bool>("value", BoolArgumentType.Bool()).Executes(Fail)));

        var modifyNumberFormat = LiteralArgumentBuilder<CommandSourceStack>.Literal("numberformat").Executes(Fail);
        AddNumberFormatOptions(modifyNumberFormat);
        modify.Then(modifyNumberFormat);
        objectives.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("modify")
            .Then(Arg<string>("objective", ObjectiveArgument.Objective()).Then(modify)));

        objectives.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("remove")
            .Then(Arg<string>("objective", ObjectiveArgument.Objective()).Executes(Fail)));

        //setdisplay <slot> [<objective>]
        var setDisplay = Arg<ScoreboardSlotArgument.DisplaySlot>("slot", ScoreboardSlotArgument.DisplaySlotArgument())
            .Executes(Fail)
            .Then(Arg<string>("objective", ObjectiveArgument.Objective()).Executes(Fail));
        objectives.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("setdisplay").Then(setDisplay));
    }

    //RegisterPlayers players list/set/get/add/remove/reset/enable/display/operation, maps to the vanilla players branch
    private static void RegisterPlayers(LiteralArgumentBuilder<CommandSourceStack> players)
    {
        players.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("list")
            .Executes(Fail)
            .Then(Arg<ScoreHolderArgument.Result>("target", ScoreHolderArgument.ScoreHolder()).Executes(Fail)));

        players.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("set")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Then(Arg<string>("objective", ObjectiveArgument.Objective())
                    .Then(Arg<int>("score", IntegerArgumentType.Integer()).Executes(Fail)))));

        players.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("get")
            .Then(Arg<ScoreHolderArgument.Result>("target", ScoreHolderArgument.ScoreHolder())
                .Then(Arg<string>("objective", ObjectiveArgument.Objective()).Executes(Fail))));

        players.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("add")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Then(Arg<string>("objective", ObjectiveArgument.Objective())
                    .Then(Arg<int>("score", IntegerArgumentType.Integer(0)).Executes(Fail)))));

        players.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("remove")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Then(Arg<string>("objective", ObjectiveArgument.Objective())
                    .Then(Arg<int>("score", IntegerArgumentType.Integer(0)).Executes(Fail)))));

        players.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("reset")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Executes(Fail)
                .Then(Arg<string>("objective", ObjectiveArgument.Objective()).Executes(Fail))));

        players.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("enable")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Then(Arg<string>("objective", ObjectiveArgument.Objective()).Executes(Fail))));

        var display = LiteralArgumentBuilder<CommandSourceStack>.Literal("display");
        display.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("name")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Then(Arg<string>("objective", ObjectiveArgument.Objective())
                    .Executes(Fail)
                    .Then(Arg<Component>("name", ComponentArgument.TextComponent()).Executes(Fail)))));
        var displayNumberFormat = Arg<string>("objective", ObjectiveArgument.Objective());
        AddNumberFormatOptions(displayNumberFormat);
        display.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("numberformat")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Then(displayNumberFormat)));
        players.Then(display);

        players.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("operation")
            .Then(Arg<ScoreHolderArgument.Result>("targets", ScoreHolderArgument.ScoreHolders())
                .Then(Arg<string>("targetObjective", ObjectiveArgument.Objective())
                    .Then(Arg<OperationArgument.Operation>("operation", OperationArgument.ScoreboardOperation())
                        .Then(Arg<ScoreHolderArgument.Result>("source", ScoreHolderArgument.ScoreHolders())
                            .Then(Arg<string>("sourceObjective", ObjectiveArgument.Objective()).Executes(Fail)))))));
    }

    //AddNumberFormatOptions attaches the blank/fixed/styled subtrees, maps to vanilla addNumberFormats
    //The number format models are not ported, so the executors only report the scoreboard is unavailable
    private static void AddNumberFormatOptions<TParent>(TParent parent) where TParent : ArgumentBuilder<CommandSourceStack, TParent>
    {
        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("blank").Executes(Fail));
        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("fixed")
            .Then(Arg<Component>("contents", ComponentArgument.TextComponent()).Executes(Fail)));
        parent.Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("styled")
            .Then(Arg<Style>("style", StyleArgument.StyleArg()).Executes(Fail)));
    }

    private static RequiredArgumentBuilder<CommandSourceStack, T> Arg<T>(string name, ArgumentType<T> type)
        => RequiredArgumentBuilder<CommandSourceStack, T>.Argument(name, type);

    private static int Fail(CommandContext<CommandSourceStack> context)
    {
        context.GetSource().SendFailure(ScoreboardUnavailable);
        return 0;
    }
}
