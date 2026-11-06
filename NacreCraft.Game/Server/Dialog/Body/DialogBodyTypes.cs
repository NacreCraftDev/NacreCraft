using NacreCraft.Codec;
using NacreCraft.Registry;
using RegistryDialogBody = NacreCraft.Registry.DialogBody;

namespace NacreCraft.Game.Server.Dialog;

//DialogBodyTypes registers the built-in dialog body types, maps to vanilla net.minecraft.server.dialog.body.DialogBodyTypes
public static class DialogBodyTypes
{
    public static MapCodec<RegistryDialogBody> Bootstrap(Registry<MapCodec<RegistryDialogBody>> registry)
    {
        Registry<MapCodec<RegistryDialogBody>>.Register(registry,
            Identifier.WithDefaultNamespace("item"), ItemBody.ControlCodec);
        return Registry<MapCodec<RegistryDialogBody>>.Register(registry,
            Identifier.WithDefaultNamespace("plain_message"), PlainMessage.ControlCodec);
    }
}
