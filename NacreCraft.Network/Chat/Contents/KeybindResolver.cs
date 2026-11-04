namespace NacreCraft.Network.Chat.Contents;

//KeybindResolver resolves a keybind name into the component shown to the user
//maps to vanilla net.minecraft.network.chat.contents.KeybindResolver
//The client installs a resolver that maps key names to their localized names; the default echoes the key name
public static class KeybindResolver
{
    private static Func<string, Func<Component>> _keyResolver = name => () => Component.Literal(name);

    public static void SetKeyResolver(Func<string, Func<Component>> resolver) => _keyResolver = resolver;

    //Resolve returns the supplier of the display component for one keybind name
    public static Func<Component> Resolve(string name) => _keyResolver(name);
}
