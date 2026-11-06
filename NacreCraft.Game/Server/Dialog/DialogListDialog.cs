using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Util;
using RegistryDialog = NacreCraft.Registry.Dialog;

namespace NacreCraft.Game.Server.Dialog;

//DialogListDialog a menu of other dialogs, maps to vanilla net.minecraft.server.dialog.DialogListDialog
public sealed record DialogListDialog(CommonDialogData Common, HolderSet<RegistryDialog> DialogList,
    Optional<ActionButton> ExitButton, int ColumnCount, int ButtonWidth) : ButtonListDialog
{
    public static readonly MapCodec<DialogListDialog> MAP_CODEC = RecordCodecBuilder.Of5(
        CommonDialogData.MAP_CODEC.ForGetter((DialogListDialog dialog) => dialog.Common),
        HolderSetCodecs.DialogSet.FieldOf("dialogs").ForGetter((DialogListDialog dialog) => dialog.DialogList),
        ActionButton.CODEC.OptionalFieldOf("exit_action").ForGetter((DialogListDialog dialog) => dialog.ExitButton),
        ExtraCodecs.PositiveInt.OptionalFieldOf("columns", 2).ForGetter((DialogListDialog dialog) => dialog.ColumnCount),
        DialogCodecs.WidthCodec.OptionalFieldOf("button_width", 150)
            .ForGetter((DialogListDialog dialog) => dialog.ButtonWidth),
        (common, dialogs, exitAction, columns, buttonWidth) =>
            new DialogListDialog(common, dialogs, exitAction, columns, buttonWidth));

    public static readonly MapCodec<RegistryDialog> ControlCodec =
        new XmapMapCodec<DialogListDialog, RegistryDialog>(MAP_CODEC, value => value,
            dialog => (DialogListDialog)dialog);

    public CommonDialogData GetCommon() => Common;

    public MapCodec<RegistryDialog> Codec() => ControlCodec;

    public int Columns() => ColumnCount;

    public Optional<ActionButton> ExitAction() => ExitButton;
}
