using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Commands;

//RaidCommand raid command, maps to vanilla net.minecraft.server.commands.RaidCommand
//The raid/raider entity model is not ported, so the vanilla tree is registered and every executor reports that raids are
//not wired to the level yet
public static class RaidCommand
{
    private const string Unavailable = "raids are not wired to the level yet";

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        var start = LiteralArgumentBuilder<CommandSourceStack>.Literal("start")
            .Then(Arg<int>("omenlvl", IntegerArgumentType.Integer(0)).Executes(Fail));
        var stop = LiteralArgumentBuilder<CommandSourceStack>.Literal("stop").Executes(Fail);
        var check = LiteralArgumentBuilder<CommandSourceStack>.Literal("check").Executes(Fail);
        var sound = LiteralArgumentBuilder<CommandSourceStack>.Literal("sound")
            .Then(Arg<Component>("type", ComponentArgument.TextComponent()).Executes(Fail));
        var spawnLeader = LiteralArgumentBuilder<CommandSourceStack>.Literal("spawnleader").Executes(Fail);
        var setOmen = LiteralArgumentBuilder<CommandSourceStack>.Literal("setomen")
            .Then(Arg<int>("level", IntegerArgumentType.Integer(0)).Executes(Fail));
        var glow = LiteralArgumentBuilder<CommandSourceStack>.Literal("glow").Executes(Fail);

        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("raid")
            .Requires(s => s.HasPermission(3))
            .Then(start)
            .Then(stop)
            .Then(check)
            .Then(sound)
            .Then(spawnLeader)
            .Then(setOmen)
            .Then(glow));
    }

    private static RequiredArgumentBuilder<CommandSourceStack, T> Arg<T>(string name, ArgumentType<T> type)
        => RequiredArgumentBuilder<CommandSourceStack, T>.Argument(name, type);

    private static int Fail(CommandContext<CommandSourceStack> context)
    {
        context.GetSource().SendFailure(Unavailable);
        return 0;
    }
}
