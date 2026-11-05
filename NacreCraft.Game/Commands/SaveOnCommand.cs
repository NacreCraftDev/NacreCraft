using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;

namespace NacreCraft.Game.Commands;

//SaveOnCommand save-on command, maps to vanilla net.minecraft.server.commands.SaveOnCommand
//Turns the periodic auto save back on
public static class SaveOnCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("save-on")
            .Requires(s => s.HasPermission(4))
            .Executes(context => SaveAllCommand.SetAuto(context, true)));
    }
}
