using NacreCraft.Commands;

namespace NacreCraft.Game.Commands;

//PardonCommand /pardon command, maps to vanilla net.minecraft.server.commands.PardonCommand
//The command tree and handlers live in BanCommand together with the other ban commands; this class is the vanilla-named entry
public static class PardonCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
        => BanCommand.RegisterPardon(dispatcher);
}
