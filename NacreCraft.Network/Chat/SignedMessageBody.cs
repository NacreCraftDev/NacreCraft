using System.Buffers.Binary;
using System.Text;
using NacreCraft.Codec;
using NacreCraft.Util;

namespace NacreCraft.Network.Chat;

//SignedMessageBody the signed payload of a chat message, maps to vanilla net.minecraft.network.chat.SignedMessageBody
public sealed record SignedMessageBody(string Content, DateTimeOffset TimeStamp, long Salt, LastSeenMessages LastSeen)
{
    //TimeStampCodec ISO-8601 round-trip form, stands in for vanilla INSTANT_ISO8601
    private static readonly Codec<DateTimeOffset> TimeStampCodec = Codecs.String.Xmap(
        text => DateTimeOffset.Parse(text, null, System.Globalization.DateTimeStyles.RoundtripKind),
        value => value.ToString("O"));

    public static readonly MapCodec<SignedMessageBody> MAP_CODEC =
        RecordCodecBuilder.Of4<SignedMessageBody, string, DateTimeOffset, long, LastSeenMessages>(
            Codecs.String.FieldOf("content").ForGetter((SignedMessageBody body) => body.Content),
            TimeStampCodec.FieldOf("time_stamp").ForGetter((SignedMessageBody body) => body.TimeStamp),
            Codecs.Long.FieldOf("salt").ForGetter((SignedMessageBody body) => body.Salt),
            LastSeenMessages.CODEC.OptionalFieldOf("last_seen", LastSeenMessages.Empty)
                .ForGetter((SignedMessageBody body) => body.LastSeen),
            (content, timeStamp, salt, lastSeen) => new SignedMessageBody(content, timeStamp, salt, lastSeen));

    public static SignedMessageBody Unsigned(string content)
        => new(content, DateTimeOffset.UtcNow, 0L, LastSeenMessages.Empty);

    public void UpdateSignature(SignatureUpdater.Output output)
    {
        Span<byte> scratch = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(scratch, Salt);
        output.Update(scratch.ToArray());
        BinaryPrimitives.WriteInt64BigEndian(scratch, TimeStamp.ToUnixTimeSeconds());
        output.Update(scratch.ToArray());
        var contentBytes = Encoding.UTF8.GetBytes(Content);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, contentBytes.Length);
        output.Update(length.ToArray());
        output.Update(contentBytes);
        LastSeen.UpdateSignature(output);
    }

    public Packed Pack(MessageSignatureCache cache)
        => new(Content, TimeStamp, Salt, LastSeen.Pack(cache));

    //Packed cache-indexed form, maps to vanilla SignedMessageBody.Packed
    public sealed record Packed(string Content, DateTimeOffset TimeStamp, long Salt, LastSeenMessages.Packed LastSeen)
    {
        public Packed(FriendlyByteBuf input)
            : this(input.ReadString(256), DateTimeOffset.FromUnixTimeMilliseconds(input.ReadLong()), input.ReadLong(),
                new LastSeenMessages.Packed(input))
        {
        }

        public void Write(FriendlyByteBuf output)
        {
            output.WriteString(Content);
            output.WriteLong(TimeStamp.ToUnixTimeMilliseconds());
            output.WriteLong(Salt);
            LastSeen.Write(output);
        }

        public Optional<SignedMessageBody> Unpack(MessageSignatureCache cache)
            => LastSeen.Unpack(cache).Map(lastSeen =>
                new SignedMessageBody(Content, TimeStamp, Salt, lastSeen));
    }
}
