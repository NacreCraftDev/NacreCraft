using NacreCraft.Util;

namespace NacreCraft.Network.Chat;

//SignedMessageChain produces and verifies the signature chain of a chat session
//maps to vanilla net.minecraft.network.chat.SignedMessageChain
//Vanilla builds the validators from a ProfilePublicKey; the profile key system is not ported, so the validator and the
//expiry check are passed in directly
public sealed class SignedMessageChain
{
    private SignedMessageLink? _nextLink;
    private DateTimeOffset _lastTimeStamp = DateTimeOffset.UnixEpoch;

    public SignedMessageChain(Guid profileId, Guid sessionId) => _nextLink = SignedMessageLink.Root(profileId, sessionId);

    //CreateEncoder builds the encoder that signs every message and advances the chain
    public Encoder CreateEncoder(Signer signer) => new ChainEncoder(this, signer);

    //ChainEncoder the encoder bound to one chain
    private sealed class ChainEncoder(SignedMessageChain chain, Signer signer) : Encoder
    {
        public MessageSignature? Pack(SignedMessageBody body)
        {
            var link = chain._nextLink;
            if (link is null) return null;
            chain._nextLink = link.Advance();
            return new MessageSignature(signer.Sign(
                SignatureUpdater.Of(output => PlayerChatMessage.UpdateSignature(output, link, body))));
        }
    }

    //Decoder builds the decoder that verifies incoming messages and advances the chain
    public Decoder CreateDecoder(SignatureValidator validator, Func<bool> expired)
        => new ChainDecoder(this, validator, expired);

    //ChainDecoder the decoder bound to one chain
    private sealed class ChainDecoder(SignedMessageChain chain, SignatureValidator validator, Func<bool> expired) : Decoder
    {
        public PlayerChatMessage Unpack(MessageSignature? signature, SignedMessageBody body)
        {
            if (signature is null) throw new DecodeException(DecodeException.MissingProfileKey);
            if (expired()) throw new DecodeException(DecodeException.ExpiredProfileKey);
            var link = chain._nextLink;
            if (link is null) throw new DecodeException(DecodeException.ChainBroken);
            if (body.TimeStamp < chain._lastTimeStamp)
            {
                SetChainBroken();
                throw new DecodeException(DecodeException.OutOfOrderChat);
            }
            chain._lastTimeStamp = body.TimeStamp;
            var unpacked = new PlayerChatMessage(link, signature, body, null, FilterMask.PassThrough);
            if (!unpacked.Verify(validator))
            {
                SetChainBroken();
                throw new DecodeException(DecodeException.InvalidSignature);
            }
            chain._nextLink = link.Advance();
            return unpacked;
        }

        public void SetChainBroken() => chain._nextLink = null;
    }

    //Encoder signs one message body, maps to vanilla Encoder
    public interface Encoder
    {
        static Encoder Unsigned => UnsignedEncoder.Instance;

        MessageSignature? Pack(SignedMessageBody body);
    }

    //UnsignedEncoder the encoder that signs nothing
    internal sealed class UnsignedEncoder : Encoder
    {
        public static readonly UnsignedEncoder Instance = new();

        public MessageSignature? Pack(SignedMessageBody body) => null;
    }

    //Decoder verifies one incoming message body, maps to vanilla Decoder
    public interface Decoder
    {
        static Decoder Unsigned(Guid profileId, Func<bool> enforcesSecureChat) => new UnsignedDecoder(profileId, enforcesSecureChat);

        PlayerChatMessage Unpack(MessageSignature? signature, SignedMessageBody body);

        void SetChainBroken() { }
    }

    //UnsignedDecoder the decoder used when the session has no key
    private sealed class UnsignedDecoder(Guid profileId, Func<bool> enforcesSecureChat) : Decoder
    {
        public PlayerChatMessage Unpack(MessageSignature? signature, SignedMessageBody body)
        {
            if (enforcesSecureChat()) throw new DecodeException(DecodeException.MissingProfileKey);
            return PlayerChatMessage.Unsigned(profileId, body.Content);
        }
    }

    //DecodeException a chat message that failed verification, maps to vanilla DecodeException
    public sealed class DecodeException : ThrowingComponent
    {
        public static readonly Component MissingProfileKey = Component.Translatable("chat.disabled.missingProfileKey");
        public static readonly Component ChainBroken = Component.Translatable("chat.disabled.chain_broken");
        public static readonly Component ExpiredProfileKey = Component.Translatable("chat.disabled.expiredProfileKey");
        public static readonly Component InvalidSignature = Component.Translatable("chat.disabled.invalid_signature");
        public static readonly Component OutOfOrderChat = Component.Translatable("chat.disabled.out_of_order_chat");

        public DecodeException(Component component) : base(component) { }
    }
}
