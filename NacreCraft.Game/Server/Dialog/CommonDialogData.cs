using NacreCraft.Codec;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Server.Dialog;

//CommonDialogData the fields every dialog carries, maps to vanilla net.minecraft.server.dialog.CommonDialogData
public sealed record CommonDialogData(Component Title, Optional<Component> ExternalTitle, bool CanCloseWithEscape,
    bool Pause, DialogAction AfterAction, IReadOnlyList<DialogBody> Body, IReadOnlyList<Input> Inputs)
{
    public static readonly MapCodec<CommonDialogData> MAP_CODEC = RecordCodecBuilder.Of7(
        ComponentSerialization.JsonCodec.FieldOf("title").ForGetter((CommonDialogData data) => data.Title),
        ComponentSerialization.JsonCodec.OptionalFieldOf("external_title")
            .ForGetter((CommonDialogData data) => data.ExternalTitle),
        Codecs.Bool.OptionalFieldOf("can_close_with_escape", true)
            .ForGetter((CommonDialogData data) => data.CanCloseWithEscape),
        Codecs.Bool.OptionalFieldOf("pause", true).ForGetter((CommonDialogData data) => data.Pause),
        DialogActions.Codec.OptionalFieldOf("after_action", DialogAction.CLOSE)
            .ForGetter((CommonDialogData data) => data.AfterAction),
        DialogBodyCodecs.CompactListCodec.OptionalFieldOf("body", Array.Empty<DialogBody>())
            .ForGetter((CommonDialogData data) => data.Body),
        Input.CODEC.ListOf().OptionalFieldOf("inputs", Array.Empty<Input>())
            .ForGetter((CommonDialogData data) => data.Inputs),
        (title, externalTitle, canCloseWithEscape, pause, afterAction, body, inputs) =>
            new CommonDialogData(title, externalTitle, canCloseWithEscape, pause, afterAction, body, inputs))
        .ComapFlatMap(Validate, data => data);

    private static DataResult<CommonDialogData> Validate(CommonDialogData data)
        => data.Pause && !data.AfterAction.WillUnpause()
            ? DataResult<CommonDialogData>.Error(
                () => "Dialogs that pause the game must use after_action values that unpause it after user action!")
            : DataResult<CommonDialogData>.Success(data);

    //ComputeExternalTitle the title shown outside the dialog, maps to vanilla computeExternalTitle
    public Component ComputeExternalTitle() => ExternalTitle.IsPresent ? ExternalTitle.Get() : Title;
}
