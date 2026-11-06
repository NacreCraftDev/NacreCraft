using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using RegistryDialog = NacreCraft.Registry.Dialog;

namespace NacreCraft.Game.Server.Dialog;

//Dialogs the built-in dialog keys and their data, maps to vanilla net.minecraft.server.dialog.Dialogs
//Bootstrap is omitted: it needs data.worldgen's BootstrapContext and DialogTags, neither ported yet
public static class Dialogs
{
    public static readonly ResourceKey<RegistryDialog> SERVER_LINKS = Create("server_links");
    public static readonly ResourceKey<RegistryDialog> CUSTOM_OPTIONS = Create("custom_options");
    public static readonly ResourceKey<RegistryDialog> QUICK_ACTIONS = Create("quick_actions");

    public const int BIG_BUTTON_WIDTH = 310;

    public static readonly ActionButton DEFAULT_BACK_BUTTON =
        new(new CommonButtonData(CommonComponents.GUI_BACK, 200), Optional<Action>.Empty());

    private static ResourceKey<RegistryDialog> Create(string id)
        => ResourceKey<RegistryDialog>.Create(Registries.DIALOG, Identifier.WithDefaultNamespace(id));
}
