using System.Buffers.Binary;
using NacreCraft.Codec;
using NacreCraft.Util;

namespace NacreCraft.Network.Chat;

//SignedMessageLink identifies one message in a chat session, maps to vanilla net.minecraft.network.chat.SignedMessageLink
public sealed record SignedMessageLink(int Index, Guid Sender, Guid SessionId)
{
    //GuidCodec canonical string form, stands in for vanilla UUIDUtil.CODEC
    private static readonly Codec<Guid> GuidCodec = Codecs.String.Xmap(Guid.Parse, value => value.ToString());

    public static readonly Codec<SignedMessageLink> CODEC = RecordCodecBuilder.Of3<SignedMessageLink, int, Guid, Guid>(
        Codecs.Int.FieldOf("index").ForGetter((SignedMessageLink link) => link.Index),
        GuidCodec.FieldOf("sender").ForGetter((SignedMessageLink link) => link.Sender),
        GuidCodec.FieldOf("session_id").ForGetter((SignedMessageLink link) => link.SessionId),
        (index, sender, sessionId) => new SignedMessageLink(index, sender, sessionId));

    public static SignedMessageLink Unsigned(Guid sender) => Root(sender, Guid.Empty);

    public static SignedMessageLink Root(Guid sender, Guid sessionId) => new(0, sender, sessionId);

    public void UpdateSignature(SignatureUpdater.Output output)
    {
        output.Update(Sender.ToByteArray());
        output.Update(SessionId.ToByteArray());
        Span<byte> index = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(index, Index);
        output.Update(index.ToArray());
    }

    public bool IsDescendantOf(SignedMessageLink link)
        => Index > link.Index && Sender == link.Sender && SessionId == link.SessionId;

    //Advance the next link in the chain, null once the index runs out
    public SignedMessageLink? Advance() => Index == int.MaxValue ? null : new SignedMessageLink(Index + 1, Sender, SessionId);
}
