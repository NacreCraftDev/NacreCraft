using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Commands;

//SayCommand say command, maps to vanilla net.minecraft.server.commands.SayCommand
//Decorates the executor's words with chat.type.announcement and broadcasts to the whole server
public static class SayCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("say")
            .Requires(s => s.HasPermission(2))
            .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument("message", MessageArgument.Message())
                .Executes(context =>
                {
                    var source = (ServerCommandSource)context.GetSource();
                    //Vanilla goes through broadcastChatMessage with ChatType.SAY_COMMAND; the decoration is [sender] content
                    source.Server.PlayerList.BroadcastSystemMessage(
                        Component.Translatable("chat.type.announcement",
                            Component.Literal(source.SenderName),
                            Component.Literal(MessageArgument.GetMessage(context, "message"))),
                        false);
                    return 1;
                })));
    }
}
