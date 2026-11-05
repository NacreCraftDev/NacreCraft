using NacreCraft.Commands;

namespace NacreCraft.Game.Commands;

//PardonIpCommand /pardon-ip command, maps to vanilla net.minecraft.server.commands.PardonIpCommand
//The command tree and handlers live in BanCommand together with the other ban commands; this class is the vanilla-named entry
public static class PardonIpCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
        => BanCommand.RegisterPardonIp(dispatcher);
}
