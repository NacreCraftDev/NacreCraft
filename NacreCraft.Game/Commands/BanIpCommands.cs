using NacreCraft.Commands;

namespace NacreCraft.Game.Commands;

//BanIpCommands /ban-ip command, maps to vanilla net.minecraft.server.commands.BanIpCommands
//The command tree and handlers live in BanCommand together with the other ban commands; this class is the vanilla-named entry
public static class BanIpCommands
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
        => BanCommand.RegisterIp(dispatcher);
}
