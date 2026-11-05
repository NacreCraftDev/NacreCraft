using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using RegistryAction = NacreCraft.Registry.Action;

namespace NacreCraft.Game.Server.Dialog;

//ActionTypes registers the dialog action codecs, maps to vanilla net.minecraft.server.dialog.action.ActionTypes
public static class ActionTypes
{
    //Bootstrap registers every static action plus the two dynamic forms, maps to vanilla bootstrap
    public static MapCodec<RegistryAction> Bootstrap(Registry<MapCodec<RegistryAction>> registry)
    {
        foreach (var (action, codec) in StaticAction.WRAPPED_CODECS)
            Registry<MapCodec<RegistryAction>>.Register(registry,
                Identifier.WithDefaultNamespace(action.GetSerializedName()), codec);
        Registry<MapCodec<RegistryAction>>.Register(registry,
            Identifier.WithDefaultNamespace("dynamic/run_command"), CommandTemplate.ActionCodec);
        return Registry<MapCodec<RegistryAction>>.Register(registry,
            Identifier.WithDefaultNamespace("dynamic/custom"), CustomAll.ActionCodec);
    }
}

