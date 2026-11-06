using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using RegistryDialog = NacreCraft.Registry.Dialog;

namespace NacreCraft.Game.Server.Dialog;

//NoticeDialog a dialog with one acknowledgement button, maps to vanilla net.minecraft.server.dialog.NoticeDialog
public sealed record NoticeDialog(CommonDialogData Common, ActionButton Button) : SimpleDialog
{
    public static readonly ActionButton DEFAULT_ACTION =
        new(new CommonButtonData(CommonComponents.GUI_OK, 150), Optional<Action>.Empty());

    public static readonly MapCodec<NoticeDialog> MAP_CODEC = RecordCodecBuilder.Of2(
        CommonDialogData.MAP_CODEC.ForGetter((NoticeDialog dialog) => dialog.Common),
        ActionButton.CODEC.OptionalFieldOf("action", DEFAULT_ACTION)
            .ForGetter((NoticeDialog dialog) => dialog.Button),
        (common, action) => new NoticeDialog(common, action));

    public static readonly MapCodec<RegistryDialog> ControlCodec =
        new XmapMapCodec<NoticeDialog, RegistryDialog>(MAP_CODEC, value => value,
            dialog => (NoticeDialog)dialog);

    public CommonDialogData GetCommon() => Common;

    public MapCodec<RegistryDialog> Codec() => ControlCodec;

    public Optional<Action> OnCancel() => Button.ClickAction;

    public IReadOnlyList<ActionButton> MainActions() => new[] { Button };
}

