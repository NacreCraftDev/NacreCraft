using NacreCraft.Util;

namespace NacreCraft.Network.Chat;

//SignedMessageValidator validates an incoming chat message, maps to vanilla net.minecraft.network.chat.SignedMessageValidator
public interface SignedMessageValidator
{
    //AcceptUnsigned drops the signature and accepts the message
    static SignedMessageValidator AcceptUnsigned => AcceptUnsignedValidator.Instance;

    //RejectAll rejects every message because no chat session was initialized
    static SignedMessageValidator RejectAll => RejectAllValidator.Instance;

    PlayerChatMessage? UpdateAndValidate(PlayerChatMessage message);
}

//AcceptUnsignedValidator always accepts and strips the signature
internal sealed class AcceptUnsignedValidator : SignedMessageValidator
{
    public static readonly AcceptUnsignedValidator Instance = new();

    public PlayerChatMessage? UpdateAndValidate(PlayerChatMessage message) => message.RemoveSignature();
}

//RejectAllValidator always rejects
internal sealed class RejectAllValidator : SignedMessageValidator
{
    public static readonly RejectAllValidator Instance = new();

    public PlayerChatMessage? UpdateAndValidate(PlayerChatMessage message) => null;
}

//KeyBasedValidator validates the signature and the chain order, maps to vanilla SignedMessageValidator.KeyBased
public sealed class KeyBasedSignedMessageValidator(SignatureValidator validator, Func<bool> expired) : SignedMessageValidator
{
    private PlayerChatMessage? _lastMessage;
    private bool _isChainValid = true;

    private bool ValidateChain(PlayerChatMessage message)
    {
        if (message.Equals(_lastMessage)) return true;
        return _lastMessage is null || message.Link.IsDescendantOf(_lastMessage.Link);
    }

    private bool Validate(PlayerChatMessage message)
        => !expired() && message.Verify(validator) && ValidateChain(message);

    public PlayerChatMessage? UpdateAndValidate(PlayerChatMessage message)
    {
        _isChainValid = _isChainValid && Validate(message);
        if (!_isChainValid) return null;
        _lastMessage = message;
        return message;
    }
}
