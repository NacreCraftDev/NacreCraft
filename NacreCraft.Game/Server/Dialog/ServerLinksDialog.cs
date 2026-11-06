using NacreCraft.Codec;
using NacreCraft.Util;
using RegistryDialog = NacreCraft.Registry.Dialog;

namespace NacreCraft.Game.Server.Dialog;

//ServerLinksDialog the server links screen, maps to vanilla net.minecraft.server.dialog.ServerLinksDialog
public sealed record ServerLinksDialog(CommonDialogData Common, Optional<ActionButton> ExitButton, int ColumnCount,
    int ButtonWidth) : ButtonListDialog
{
    public static readonly MapCodec<ServerLinksDialog> MAP_CODEC = RecordCodecBuilder.Of4(
        CommonDialogData.MAP_CODEC.ForGetter((ServerLinksDialog dialog) => dialog.Common),
        ActionButton.CODEC.OptionalFieldOf("exit_action").ForGetter((ServerLinksDialog dialog) => dialog.ExitButton),
        ExtraCodecs.PositiveInt.OptionalFieldOf("columns", 2).ForGetter((ServerLinksDialog dialog) => dialog.ColumnCount),
        DialogCodecs.WidthCodec.OptionalFieldOf("button_width", 150)
            .ForGetter((ServerLinksDialog dialog) => dialog.ButtonWidth),
        (common, exitAction, columns, buttonWidth) =>
            new ServerLinksDialog(common, exitAction, columns, buttonWidth));

    public static readonly MapCodec<RegistryDialog> ControlCodec =
        new XmapMapCodec<ServerLinksDialog, RegistryDialog>(MAP_CODEC, value => value,
            dialog => (ServerLinksDialog)dialog);

    public CommonDialogData GetCommon() => Common;

    public MapCodec<RegistryDialog> Codec() => ControlCodec;

    public int Columns() => ColumnCount;

    public Optional<ActionButton> ExitAction() => ExitButton;
}
