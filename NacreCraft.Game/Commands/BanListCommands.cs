using NacreCraft.Commands;

namespace NacreCraft.Game.Commands;

//BanListCommands /banlist command, maps to vanilla net.minecraft.server.commands.BanListCommands
//The command tree and handlers live in BanCommand together with the other ban commands; this class is the vanilla-named entry
public static class BanListCommands
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
        => BanCommand.RegisterList(dispatcher);
}
