using NacreCraft.Commands;

namespace NacreCraft.Game.Commands;

//BanPlayerCommands /ban command, maps to vanilla net.minecraft.server.commands.BanPlayerCommands
//The command tree and handlers live in BanCommand together with the other ban commands; this class is the vanilla-named entry
public static class BanPlayerCommands
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
        => BanCommand.RegisterPlayer(dispatcher);
}
