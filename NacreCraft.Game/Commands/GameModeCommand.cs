using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Game.World.Level;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Commands;

//GameModeCommand /gamemode command, maps to vanilla net.minecraft.server.commands.GameModeCommand
//The executor gets only its own report; other targets also receive a private notice
public static class GameModeCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("gamemode")
            .Requires(s => s.HasPermission(2))
            .Then(RequiredArgumentBuilder<CommandSourceStack, GameType>.Argument("gamemode", GameModeArgument.GameMode())
                .Executes(context => SetGameMode(context, new[] { ((ServerCommandSource)context.GetSource()).Player }))
                .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument("targets", EntityArgument.Players())
                    .Executes(context => SetGameMode(context, EntityArgument.GetPlayers(context, "targets"))))));
    }

    //SetGameMode switches the target player's game mode and reports, maps to vanilla setGameMode
    private static int SetGameMode(CommandContext<CommandSourceStack> context, IReadOnlyList<ServerPlayer> targets)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        var gameType = GameModeArgument.GetGameMode(context, "gamemode");
        var changed = 0;
        foreach (var target in targets)
        {
            //No packet or report when the mode is unchanged, consistent with vanilla setGameMode returning false
            if (target.GameType == gameType) continue;
            source.Server.PlayerList.ChangeGameMode(target, gameType);
            changed++;
            if (ReferenceEquals(target, source.Player))
            {
                source.SendSuccess($"set your game mode to {gameType.Name}");
            }
            else
            {
                source.SendSuccess($"set {target.Profile.Name}'s game mode to {gameType.Name}");
                target.Connection.Send(new ClientboundSystemChatPacket(
                    Component.Literal($"your game mode has been set to {gameType.Name}"), false));
            }
        }
        if (changed == 0)
        {
            source.SendFailure("the target is already in that game mode");
            return 0;
        }
        return changed;
    }
}
