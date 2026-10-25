namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundPlayerChatPacket player chat packet, maps to vanilla ClientboundPlayerChatPacket
//Fields: GlobalIndex(int), Sender(UUID), Index(int), Signature(PackedMessageSignature or null)
//Body is a SignedMessageBodyData, UnsignedContent is the component shown without signature checks
//FilterMask is a FilterMaskData and ChatType is a ChatTypeBoundData
public sealed record ClientboundPlayerChatPacket(int GlobalIndex, Guid Sender, int Index, object Signature, object Body, Component? UnsignedContent, object FilterMask, object ChatType) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundPlayerChatPacket> StreamCodec { get; } = new PlayerChatCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundPlayerChat;

    public void Handle(ClientGamePacketListener handler) => handler.HandlePlayerChat(this);

    private sealed class PlayerChatCodec : StreamCodec<FriendlyByteBuf, ClientboundPlayerChatPacket>
    {
        public ClientboundPlayerChatPacket Decode(FriendlyByteBuf buf)
        {
            var globalIndex = buf.ReadVarInt();
            var sender = buf.ReadUuid();
            var index = buf.ReadVarInt();
            var signature = (PackedMessageSignature?)null;
            if (buf.ReadBoolean()) signature = ReadSignature(buf);
            var body = new SignedMessageBodyData(
                buf.ReadString(256),
                buf.ReadLong(),
                buf.ReadLong(),
                ReadLastSeen(buf));
            var unsignedContent = buf.ReadBoolean() ? ComponentSerialization.StreamCodec.Decode(buf) : null;
            var filterMask = ReadFilterMask(buf);
            var chatType = ReadChatTypeBound(buf);
            return new ClientboundPlayerChatPacket(globalIndex, sender, index, signature!, body, unsignedContent, filterMask, chatType);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundPlayerChatPacket value)
        {
            buf.WriteVarInt(value.GlobalIndex);
            buf.WriteUuid(value.Sender);
            buf.WriteVarInt(value.Index);
            buf.WriteBoolean(value.Signature is PackedMessageSignature);
            if (value.Signature is PackedMessageSignature signature) WriteSignature(buf, signature);
            var body = value.Body as SignedMessageBodyData
                ?? throw new InvalidOperationException("Chat body payload is missing");
            buf.WriteString(body.Content, 256);
            buf.WriteLong(body.TimeStamp);
            buf.WriteLong(body.Salt);
            WriteLastSeen(buf, body.LastSeen);
            buf.WriteBoolean(value.UnsignedContent is not null);
            if (value.UnsignedContent is not null) ComponentSerialization.StreamCodec.Encode(buf, value.UnsignedContent);
            WriteFilterMask(buf, value.FilterMask as FilterMaskData ?? FilterMaskData.PassThrough);
            WriteChatTypeBound(buf, value.ChatType as ChatTypeBoundData
                ?? throw new InvalidOperationException("Chat type payload is missing"));
        }

        //Vanilla MessageSignature: a VAR_INT id + 1 where 0 means the full 256-byte signature follows
        private static PackedMessageSignature ReadSignature(FriendlyByteBuf buf)
        {
            var id = buf.ReadVarInt() - 1;
            return id == -1
                ? new PackedMessageSignature(-1, buf.ReadBytes(256))
                : new PackedMessageSignature(id, null);
        }

        private static void WriteSignature(FriendlyByteBuf buf, PackedMessageSignature signature)
        {
            buf.WriteVarInt(signature.Id + 1);
            if (signature.Id == -1) buf.WriteBytes(signature.FullSignature!);
        }

        private static List<PackedMessageSignature> ReadLastSeen(FriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var entries = new List<PackedMessageSignature>(count);
            for (var i = 0; i < count; i++) entries.Add(ReadSignature(buf));
            return entries;
        }

        private static void WriteLastSeen(FriendlyByteBuf buf, IReadOnlyList<PackedMessageSignature> entries)
        {
            buf.WriteVarInt(entries.Count);
            foreach (var entry in entries) WriteSignature(buf, entry);
        }

        //FilterMask type: 0 pass-through, 1 fully filtered, 2 partially filtered with a bitset payload
        private static FilterMaskData ReadFilterMask(FriendlyByteBuf buf)
        {
            var type = buf.ReadVarInt();
            if (type != 2) return new FilterMaskData(type, Array.Empty<long>());
            var wordCount = buf.ReadVarInt();
            var words = new long[wordCount];
            for (var i = 0; i < wordCount; i++) words[i] = buf.ReadLong();
            return new FilterMaskData(type, words);
        }

        private static void WriteFilterMask(FriendlyByteBuf buf, FilterMaskData mask)
        {
            buf.WriteVarInt(mask.Type);
            if (mask.Type != 2) return;
            buf.WriteVarInt(mask.Words.Length);
            buf.WriteLongsBigEndian(mask.Words);
        }

        //ChatType.Bound: holder id + 1, sender name, then an optional target name; the inline holder form is not decoded
        private static ChatTypeBoundData ReadChatTypeBound(FriendlyByteBuf buf)
        {
            var holderId = buf.ReadVarInt();
            if (holderId == 0)
                throw new NotSupportedException("Inline chat type is not implemented");
            var name = ComponentSerialization.StreamCodec.Decode(buf);
            var targetName = buf.ReadBoolean() ? ComponentSerialization.StreamCodec.Decode(buf) : null;
            return new ChatTypeBoundData(holderId - 1, name, targetName);
        }

        private static void WriteChatTypeBound(FriendlyByteBuf buf, ChatTypeBoundData chatType)
        {
            buf.WriteVarInt(chatType.ChatTypeId + 1);
            ComponentSerialization.StreamCodec.Encode(buf, chatType.Name);
            buf.WriteBoolean(chatType.TargetName is not null);
            if (chatType.TargetName is not null) ComponentSerialization.StreamCodec.Encode(buf, chatType.TargetName);
        }
    }
}

//SignedMessageBodyData the signed chat body; lastSeen references previously received signatures
public sealed record SignedMessageBodyData(string Content, long TimeStamp, long Salt, IReadOnlyList<PackedMessageSignature> LastSeen);

//FilterMaskData chat filter mask; Words is only meaningful for the partially filtered type
public sealed record FilterMaskData(int Type, long[] Words)
{
    public static readonly FilterMaskData PassThrough = new(0, Array.Empty<long>());
}

//ChatTypeBoundData resolved chat type with the sender name and an optional target name
public sealed record ChatTypeBoundData(int ChatTypeId, Component Name, Component? TargetName);
