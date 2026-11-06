using NacreCraft.Codec;
using RegistryDialog = NacreCraft.Registry.Dialog;

namespace NacreCraft.Game.Server.Dialog;

//ConfirmationDialog a yes/no dialog, maps to vanilla net.minecraft.server.dialog.ConfirmationDialog
public sealed record ConfirmationDialog(CommonDialogData Common, ActionButton YesButton, ActionButton NoButton)
    : SimpleDialog
{
    public static readonly MapCodec<ConfirmationDialog> MAP_CODEC = RecordCodecBuilder.Of3(
        CommonDialogData.MAP_CODEC.ForGetter((ConfirmationDialog dialog) => dialog.Common),
        ActionButton.CODEC.FieldOf("yes").ForGetter((ConfirmationDialog dialog) => dialog.YesButton),
        ActionButton.CODEC.FieldOf("no").ForGetter((ConfirmationDialog dialog) => dialog.NoButton),
        (common, yes, no) => new ConfirmationDialog(common, yes, no));

    public static readonly MapCodec<RegistryDialog> ControlCodec =
        new XmapMapCodec<ConfirmationDialog, RegistryDialog>(MAP_CODEC, value => value,
            dialog => (ConfirmationDialog)dialog);

    public CommonDialogData GetCommon() => Common;

    public MapCodec<RegistryDialog> Codec() => ControlCodec;

    public Optional<Action> OnCancel() => NoButton.ClickAction;

    public IReadOnlyList<ActionButton> MainActions() => new[] { YesButton, NoButton };
}

