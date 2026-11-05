using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Server;

namespace NacreCraft.Game.Commands;

//TeamMsgCommand /teammsg command, maps to vanilla net.minecraft.server.commands.TeamMsgCommand
//Sends a message to the members of the sender's scoreboard team; the scoreboard is not attached to the running server yet,
//so there is never a team to send to and the command reports that
public static class TeamMsgCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var msg = dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("teammsg")
            .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument("message", MessageArgument.Message())
                .Executes(NotOnTeam)));
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("tm")
            .Forward(msg, null, false));
    }

    private static int NotOnTeam(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("you are not on a team");
        return 0;
    }
}
