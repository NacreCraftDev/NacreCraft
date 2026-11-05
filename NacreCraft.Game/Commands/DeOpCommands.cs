using NacreCraft.Commands;

namespace NacreCraft.Game.Commands;

//DeOpCommands /deop command, maps to vanilla net.minecraft.server.commands.DeOpCommands
//The handler is shared with /op because both edit the same list; this class owns the deop tree
public static class DeOpCommands
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
        => OpCommand.RegisterDeOp(dispatcher);
}
