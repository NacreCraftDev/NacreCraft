using NacreCraft.Codec;
using NacreCraft.Registry;
using RegistryDialog = NacreCraft.Registry.Dialog;

namespace NacreCraft.Game.Server.Dialog;

//DialogTypes registers the built-in dialog types, maps to vanilla net.minecraft.server.dialog.DialogTypes
public static class DialogTypes
{
    public static MapCodec<RegistryDialog> Bootstrap(Registry<MapCodec<RegistryDialog>> registry)
    {
        Registry<MapCodec<RegistryDialog>>.Register(registry,
            Identifier.WithDefaultNamespace("notice"), NoticeDialog.ControlCodec);
        Registry<MapCodec<RegistryDialog>>.Register(registry,
            Identifier.WithDefaultNamespace("server_links"), ServerLinksDialog.ControlCodec);
        Registry<MapCodec<RegistryDialog>>.Register(registry,
            Identifier.WithDefaultNamespace("dialog_list"), DialogListDialog.ControlCodec);
        Registry<MapCodec<RegistryDialog>>.Register(registry,
            Identifier.WithDefaultNamespace("multi_action"), MultiActionDialog.ControlCodec);
        return Registry<MapCodec<RegistryDialog>>.Register(registry,
            Identifier.WithDefaultNamespace("confirmation"), ConfirmationDialog.ControlCodec);
    }
}
