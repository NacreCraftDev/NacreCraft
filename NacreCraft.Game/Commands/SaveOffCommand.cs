using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;

namespace NacreCraft.Game.Commands;

//SaveOffCommand save-off command, maps to vanilla net.minecraft.server.commands.SaveOffCommand
//Turns off the periodic auto save; the in-memory setting is shared with save-on
public static class SaveOffCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("save-off")
            .Requires(s => s.HasPermission(4))
            .Executes(context => SaveAllCommand.SetAuto(context, false)));
    }
}
