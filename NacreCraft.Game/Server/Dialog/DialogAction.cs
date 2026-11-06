using NacreCraft.Codec;

namespace NacreCraft.Game.Server.Dialog;

//DialogAction what happens after the player acts on a dialog, maps to vanilla net.minecraft.server.dialog.DialogAction
public enum DialogAction
{
    CLOSE,
    NONE,
    WAIT_FOR_RESPONSE
}

public static class DialogActions
{
    public static string GetSerializedName(this DialogAction action) => action switch
    {
        DialogAction.CLOSE => "close",
        DialogAction.NONE => "none",
        _ => "wait_for_response"
    };

    //WillUnpause whether the action lets a paused game continue, maps to vanilla willUnpause
    public static bool WillUnpause(this DialogAction action)
        => action is DialogAction.CLOSE or DialogAction.WAIT_FOR_RESPONSE;

    public static readonly Codec<DialogAction> Codec = Codecs.String.ComapFlatMap(
        name => name switch
        {
            "close" => DataResult<DialogAction>.Success(DialogAction.CLOSE),
            "none" => DataResult<DialogAction>.Success(DialogAction.NONE),
            "wait_for_response" => DataResult<DialogAction>.Success(DialogAction.WAIT_FOR_RESPONSE),
            _ => DataResult<DialogAction>.Error(() => $"Unknown dialog action: {name}")
        },
        action => action.GetSerializedName());
}
