using NacreCraft.Codec;
using NacreCraft.Util;
using RegistryDialog = NacreCraft.Registry.Dialog;

namespace NacreCraft.Game.Server.Dialog;

//MultiActionDialog a grid of action buttons, maps to vanilla net.minecraft.server.dialog.MultiActionDialog
public sealed record MultiActionDialog(CommonDialogData Common, IReadOnlyList<ActionButton> Actions,
    Optional<ActionButton> ExitButton, int ColumnCount) : ButtonListDialog
{
    public static readonly MapCodec<MultiActionDialog> MAP_CODEC = RecordCodecBuilder.Of4(
        CommonDialogData.MAP_CODEC.ForGetter((MultiActionDialog dialog) => dialog.Common),
        ExtraCodecs.NonEmptyList(ActionButton.CODEC.ListOf()).FieldOf("actions")
            .ForGetter((MultiActionDialog dialog) => dialog.Actions),
        ActionButton.CODEC.OptionalFieldOf("exit_action").ForGetter((MultiActionDialog dialog) => dialog.ExitButton),
        ExtraCodecs.PositiveInt.OptionalFieldOf("columns", 2).ForGetter((MultiActionDialog dialog) => dialog.ColumnCount),
        (common, actions, exitAction, columns) => new MultiActionDialog(common, actions, exitAction, columns));

    public static readonly MapCodec<RegistryDialog> ControlCodec =
        new XmapMapCodec<MultiActionDialog, RegistryDialog>(MAP_CODEC, value => value,
            dialog => (MultiActionDialog)dialog);

    public CommonDialogData GetCommon() => Common;

    public MapCodec<RegistryDialog> Codec() => ControlCodec;

    public int Columns() => ColumnCount;

    public Optional<ActionButton> ExitAction() => ExitButton;
}
