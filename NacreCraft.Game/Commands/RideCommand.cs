using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;

namespace NacreCraft.Game.Commands;

//RideCommand ride command, maps to vanilla net.minecraft.server.commands.RideCommand
//ride <target> mount <vehicle> and ride <target> dismount; the command tree matches vanilla
//Degraded: Registry.Entity carries no passenger/vehicle state and no startRiding/stopRiding, so execution reports that riding is unavailable
public static class RideCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("ride")
            .Requires(s => s.HasPermission(2))
            .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument("target", EntityArgument.Entity())
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("mount")
                    .Then(RequiredArgumentBuilder<CommandSourceStack, EntitySelector>.Argument("vehicle", EntityArgument.Entity())
                        .Executes(Mount)))
                .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("dismount")
                    .Executes(Dismount))));
    }

    //Mount would put target on vehicle, maps to vanilla mount
    private static int Mount(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        EntityArgument.GetSingleTarget(context, "target");
        EntityArgument.GetSingleTarget(context, "vehicle");
        source.SendFailure("riding is not implemented on this server; the target was not mounted");
        return 0;
    }

    //Dismount would take target off its vehicle, maps to vanilla dismount
    private static int Dismount(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        EntityArgument.GetSingleTarget(context, "target");
        source.SendFailure("riding is not implemented on this server; the target was not dismounted");
        return 0;
    }
}
