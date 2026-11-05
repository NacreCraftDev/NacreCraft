namespace NacreCraft.Network.Chat;

//ChatDecorator decorates an outgoing chat message, maps to vanilla net.minecraft.network.chat.ChatDecorator
//Vanilla takes a ServerPlayer; Network cannot reference the Game layer, so the player is passed as object
public interface ChatDecorator
{
    //Plain the decorator that leaves the message unchanged, maps to vanilla PLAIN
    static ChatDecorator Plain => PlainChatDecorator.Instance;

    Component Decorate(object? player, Component component);
}

//PlainChatDecorator returns the input component unchanged
internal sealed class PlainChatDecorator : ChatDecorator
{
    public static readonly PlainChatDecorator Instance = new();

    public Component Decorate(object? player, Component component) => component;
}
