using NacreCraft.Codec;
using NacreCraft.Util;

namespace NacreCraft.Network.Chat;

//PlayerChatMessage one player chat message with its signature and filter state
//maps to vanilla net.minecraft.network.chat.PlayerChatMessage
public sealed record PlayerChatMessage(SignedMessageLink Link, MessageSignature? Signature, SignedMessageBody SignedBody,
    Component? UnsignedContent, FilterMask FilterMask)
{
    public static readonly MapCodec<PlayerChatMessage> MAP_CODEC =
        RecordCodecBuilder.Of5<PlayerChatMessage, SignedMessageLink, Optional<MessageSignature>, SignedMessageBody,
            Optional<Component>, FilterMask>(
            SignedMessageLink.CODEC.FieldOf("link").ForGetter((PlayerChatMessage message) => message.Link),
            MessageSignature.CODEC.OptionalFieldOf("signature")
                .ForGetter((PlayerChatMessage message) => Optional<MessageSignature>.OfNullable(message.Signature)),
            SignedMessageBody.MAP_CODEC.ForGetter((PlayerChatMessage message) => message.SignedBody),
            ComponentSerialization.Codec.OptionalFieldOf("unsigned_content")
                .ForGetter((PlayerChatMessage message) => Optional<Component>.OfNullable(message.UnsignedContent)),
            FilterMask.CODEC.OptionalFieldOf("filter_mask", FilterMask.PassThrough)
                .ForGetter((PlayerChatMessage message) => message.FilterMask),
            (link, signature, signedBody, unsignedContent, filterMask) => new PlayerChatMessage(link,
                signature.IsPresent ? signature.Get() : null, signedBody,
                unsignedContent.IsPresent ? unsignedContent.Get() : null, filterMask));

    //SystemSender the sender id used for system messages
    public static readonly Guid SystemSender = Guid.Empty;

    public static readonly TimeSpan MessageExpiresAfterServer = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan MessageExpiresAfterClient = MessageExpiresAfterServer + TimeSpan.FromMinutes(2);

    public static PlayerChatMessage System(string content) => Unsigned(SystemSender, content);

    public static PlayerChatMessage Unsigned(Guid sender, string content)
        => new(SignedMessageLink.Unsigned(sender), null, SignedMessageBody.Unsigned(content), null,
            FilterMask.PassThrough);

    public PlayerChatMessage WithUnsignedContent(Component content)
    {
        var unsignedContent = !content.Equals(Component.Literal(SignedContent)) ? content : null;
        return new PlayerChatMessage(Link, Signature, SignedBody, unsignedContent, FilterMask);
    }

    public PlayerChatMessage RemoveUnsignedContent()
        => UnsignedContent is null ? this : new PlayerChatMessage(Link, Signature, SignedBody, null, FilterMask);

    public PlayerChatMessage Filter(FilterMask filterMask)
        => FilterMask.Equals(filterMask) ? this : new PlayerChatMessage(Link, Signature, SignedBody, UnsignedContent, filterMask);

    public PlayerChatMessage Filter(bool filtered) => Filter(filtered ? FilterMask : FilterMask.PassThrough);

    public PlayerChatMessage RemoveSignature()
        => new(SignedMessageLink.Unsigned(Sender), null, SignedMessageBody.Unsigned(SignedContent), UnsignedContent,
            FilterMask);

    public static void UpdateSignature(SignatureUpdater.Output output, SignedMessageLink link, SignedMessageBody body)
    {
        output.Update(BitConverter.GetBytes(1));
        link.UpdateSignature(output);
        body.UpdateSignature(output);
    }

    public bool Verify(SignatureValidator signatureValidator)
        => Signature is not null
           && Signature.Verify(signatureValidator, SignatureUpdater.Of(output => UpdateSignature(output, Link, SignedBody)));

    public string SignedContent => SignedBody.Content;

    public Component DecoratedContent => UnsignedContent ?? Component.Literal(SignedContent);

    public DateTimeOffset TimeStamp => SignedBody.TimeStamp;

    public long Salt => SignedBody.Salt;

    public bool HasExpiredServer(DateTimeOffset now) => now > TimeStamp + MessageExpiresAfterServer;

    public bool HasExpiredClient(DateTimeOffset now) => now > TimeStamp + MessageExpiresAfterClient;

    public Guid Sender => Link.Sender;

    public bool IsSystem => Sender == SystemSender;

    public bool HasSignature => Signature is not null;

    public bool HasSignatureFrom(Guid profileId) => HasSignature && Link.Sender == profileId;

    public bool IsFullyFiltered => FilterMask.IsFullyFiltered;

    //DescribeSigned a human readable dump of the message, maps to vanilla describeSigned
    public static string DescribeSigned(PlayerChatMessage message)
        => $"'{message.SignedBody.Content}' @ {message.SignedBody.TimeStamp}\n - From: {message.Link.Sender}/{message.Link.SessionId}, message #{message.Link.Index}\n - Salt: {message.SignedBody.Salt}\n - Signature: {MessageSignature.Describe(message.Signature)}\n - Last Seen: [\n{string.Concat(message.SignedBody.LastSeen.Entries.Select(signature => $"     {MessageSignature.Describe(signature)}\n"))} ]\n";
}
