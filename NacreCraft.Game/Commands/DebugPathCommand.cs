using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands.Arguments;

namespace NacreCraft.Game.Commands;

//DebugPathCommand debug path command, maps to vanilla net.minecraft.server.commands.DebugPathCommand
//Draws the ground path from the executing mob to the target block
//Degraded: the mob navigation/pathfinder stack is not ported, so the tree mirrors vanilla and execution
//reports that path drawing is unavailable
public static class DebugPathCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("debugpath")
            .Requires(s => s.HasPermission(2))
            .Then(RequiredArgumentBuilder<CommandSourceStack, BlockCoordinates>.Argument(
                    "to", BlockPosArgument.BlockPos())
                .Executes(FillBlocks)));
    }

    //FillBlocks would build a GroundPathNavigation and draw the reached path, maps to vanilla fillBlocks
    private static int FillBlocks(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendFailure("debug path drawing is not implemented on this server; no path was made");
        return 0;
    }
}
