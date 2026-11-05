using NacreCraft.Commands;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;

namespace NacreCraft.Game.Commands;

//SaveAllCommand save-all command, maps to vanilla net.minecraft.server.commands.SaveAllCommand
//save-all flushes everything immediately; the flush subcommand is accepted for parity and does the same
public static class SaveAllCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("save-all")
            .Requires(s => s.HasPermission(4))
            .Executes(All)
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("flush")
                .Executes(All)));
    }

    //All flushes immediately; a full write blocks the main thread so the reply is sent first
    private static int All(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        source.SendSuccess("saving the world");
        source.Server.SaveAllNow();
        source.SendSuccess("the world has been saved");
        return 1;
    }

    //SetAuto toggles auto-flush for save-off/save-on; it reports no duplicate setting when already in the target state
    internal static int SetAuto(CommandContext<CommandSourceStack> context, bool enabled)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        if (source.Server.IsSavingEnabled == enabled)
        {
            source.SendFailure(enabled ? "auto-save is already on" : "auto-save is already off");
            return 0;
        }

        source.Server.SetSavingEnabled(enabled);
        source.SendSuccess(enabled ? "auto-save enabled" : "auto-save disabled; remember to flush manually with save-all");
        return 1;
    }
}
