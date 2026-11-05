using NacreCraft.Commands;

namespace NacreCraft.Game.Commands;

//EffectCommand effect command entry point, maps to vanilla net.minecraft.server.commands.EffectCommands
//Kept as the documented CommandManager call site for the mod API's command-register injection; the tree lives in EffectCommands
public static class EffectCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
        => EffectCommands.Register(dispatcher);
}
