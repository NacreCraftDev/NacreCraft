using System.Buffers.Binary;
using NacreCraft.Codec;
using NacreCraft.Util;

namespace NacreCraft.Network.Chat;

//LastSeenMessages the signatures the client has acknowledged, maps to vanilla net.minecraft.network.chat.LastSeenMessages
public sealed record LastSeenMessages(IReadOnlyList<MessageSignature> Entries)
{
    public static readonly LastSeenMessages Empty = new(Array.Empty<MessageSignature>());

    public const int LastSeenMessagesMaxLength = 20;

    //CODEC base64 string list form, maps to the vanilla signature list codec
    public static readonly Codec<LastSeenMessages> CODEC = MessageSignature.CODEC.ListOf()
        .ComapFlatMap(
            list => DataResult<LastSeenMessages>.Success(new LastSeenMessages(list)),
            messages => messages.Entries);

    //UpdateSignature feeds the entry count and every signature into the signature bytes
    public void UpdateSignature(SignatureUpdater.Output output)
    {
        Span<byte> count = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(count, Entries.Count);
        output.Update(count.ToArray());
        foreach (var entry in Entries) output.Update(entry.Bytes);
    }

    public Packed Pack(MessageSignatureCache cache)
        => new(Entries.Select(entry => entry.Pack(cache)).ToList());

    //ComputeChecksum the rolling checksum of every signature, never zero
    public byte ComputeChecksum()
    {
        var checksum = 1;
        foreach (var entry in Entries) checksum = (31 * checksum) + entry.Checksum();
        var result = (byte)checksum;
        return result == 0 ? (byte)1 : result;
    }

    //Packed cache-indexed form, maps to vanilla LastSeenMessages.Packed
    public sealed record Packed(IReadOnlyList<MessageSignature.Packed> Entries)
    {
        public static readonly Packed Empty = new(Array.Empty<MessageSignature.Packed>());

        public Packed(FriendlyByteBuf input) : this(ReadEntries(input)) { }

        public void Write(FriendlyByteBuf output)
        {
            output.WriteVarInt(Entries.Count);
            foreach (var entry in Entries) MessageSignature.Packed.Write(output, entry);
        }

        public Optional<LastSeenMessages> Unpack(MessageSignatureCache cache)
        {
            var unpacked = new List<MessageSignature>(Entries.Count);
            foreach (var packed in Entries)
            {
                var entry = packed.Unpack(cache);
                if (entry is null) return Optional<LastSeenMessages>.Empty();
                unpacked.Add(entry);
            }
            return Optional<LastSeenMessages>.Of(new LastSeenMessages(unpacked));
        }

        private static IReadOnlyList<MessageSignature.Packed> ReadEntries(FriendlyByteBuf input)
        {
            var count = input.ReadVarInt();
            if (count > LastSeenMessagesMaxLength)
                throw new InvalidOperationException($"last seen packed count {count} exceeds {LastSeenMessagesMaxLength}");
            var entries = new List<MessageSignature.Packed>(count);
            for (var i = 0; i < count; i++) entries.Add(MessageSignature.Packed.Read(input));
            return entries;
        }
    }

    //Update the acknowledgement window sent by the client, maps to vanilla LastSeenMessages.Update
    public sealed record Update(int Offset, IReadOnlyList<bool> Acknowledged, byte Checksum)
    {
        public const byte IgnoreChecksum = 0;

        public Update(FriendlyByteBuf input) : this(input.ReadVarInt(), ReadAcknowledged(input), input.ReadByte()) { }

        public void Write(FriendlyByteBuf output)
        {
            output.WriteVarInt(Offset);
            WriteAcknowledged(output, Acknowledged);
            output.WriteByte(Checksum);
        }

        public bool VerifyChecksum(LastSeenMessages lastSeen)
            => Checksum == IgnoreChecksum || Checksum == lastSeen.ComputeChecksum();

        private static IReadOnlyList<bool> ReadAcknowledged(FriendlyByteBuf input)
        {
            var longs = input.ReadLongArray();
            var result = new List<bool>(LastSeenMessagesMaxLength);
            for (var i = 0; i < LastSeenMessagesMaxLength; i++)
            {
                var word = i / 64;
                result.Add(word < longs.Length && (longs[word] & (1L << (i % 64))) != 0);
            }
            return result;
        }

        private static void WriteAcknowledged(FriendlyByteBuf output, IReadOnlyList<bool> acknowledged)
        {
            var longs = new long[(LastSeenMessagesMaxLength + 63) / 64];
            for (var i = 0; i < acknowledged.Count && i < LastSeenMessagesMaxLength; i++)
                if (acknowledged[i]) longs[i / 64] |= 1L << (i % 64);
            output.WriteLongArray(longs);
        }
    }
}
