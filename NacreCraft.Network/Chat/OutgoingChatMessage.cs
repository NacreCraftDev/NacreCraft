namespace NacreCraft.Network.Chat;

//OutgoingChatMessage a chat message ready to be sent to players, maps to vanilla net.minecraft.network.chat.OutgoingChatMessage
//Vanilla writes straight to the player connection; Network cannot reference the Game layer, so the send entry points are hooks
//installed by the server
public interface OutgoingChatMessage
{
    //PlayerSendHook sends a signed player message
    static Action<object, PlayerChatMessage, ChatType.Bound>? PlayerSendHook { get; set; }

    //DisguisedSendHook sends an unsigned disguised message
    static Action<object, Component, ChatType.Bound>? DisguisedSendHook { get; set; }

    Component Content { get; }

    void SendToPlayer(object player, bool filtered, ChatType.Bound chatType);

    static OutgoingChatMessage Create(PlayerChatMessage message)
        => message.IsSystem ? new Disguised(message.DecoratedContent) : new Player(message);

    //Disguised a system message sent without a signature
    public sealed record Disguised(Component Content) : OutgoingChatMessage
    {
        public void SendToPlayer(object player, bool filtered, ChatType.Bound chatType)
            => DisguisedSendHook?.Invoke(player, Content, chatType);
    }

    //Player a signed player message
    public sealed record Player(PlayerChatMessage Message) : OutgoingChatMessage
    {
        public Component Content => Message.DecoratedContent;

        public void SendToPlayer(object player, bool filtered, ChatType.Bound chatType)
        {
            var filteredMessage = Message.Filter(filtered);
            if (!filteredMessage.IsFullyFiltered)
                PlayerSendHook?.Invoke(player, filteredMessage, chatType);
        }
    }
}
