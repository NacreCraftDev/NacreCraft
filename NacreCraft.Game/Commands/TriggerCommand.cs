using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;

namespace NacreCraft.Game.Commands;

//TriggerCommand trigger command, maps to vanilla net.minecraft.server.commands.TriggerCommand
//trigger <objective> [add|set <value>] bumps a trigger objective of the executing player
//Degraded: MinecraftServer holds no Scoreboard instance and objectives are not tracked yet, so the tree is registered and execution reports that trigger objectives are unavailable
public static class TriggerCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("trigger")
            .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument("objective", ObjectiveArgument.Objective())
                .Executes(SimpleTrigger)
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("add")
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument("value", IntegerArgumentType.Integer())
                        .Executes(AddValue)))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("set")
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument("value", IntegerArgumentType.Integer())
                        .Executes(SetValue)))));
    }

    //SimpleTrigger would add one to the score, maps to vanilla simpleTrigger
    private static int SimpleTrigger(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //AddValue would add the amount to the score, maps to vanilla addValue
    private static int AddValue(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //SetValue would set the score, maps to vanilla setValue
    private static int SetValue(CommandContext<CommandSourceStack> context) => Unavailable(context);

    //Unavailable resolves the executor so console use still reports the player requirement, then explains the missing subsystem
    private static int Unavailable(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        _ = source.PlayerOrThrow;
        source.SendFailure("trigger objectives are not implemented on this server; no score was changed");
        return 0;
    }
}
