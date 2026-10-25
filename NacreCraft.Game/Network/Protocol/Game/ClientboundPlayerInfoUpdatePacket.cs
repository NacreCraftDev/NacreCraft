using NacreCraft.Game.World.Level;
using NacreCraft.Network;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Network.Protocol.Game;

//PlayerInfoAction player info update action bit positions, maps to vanilla ClientboundPlayerInfoUpdatePacket.Action
//The bit index is the declaration order, which is the order the actions are written in
public enum PlayerInfoAction : byte
{
    AddPlayer = 0,
    InitializeChat = 1,
    UpdateGameMode = 2,
    UpdateListed = 3,
    UpdateLatency = 4,
    UpdateDisplayName = 5,
    UpdateListOrder = 6,
    UpdateHat = 7
}

//ChatSessionData the player's chat session public key, maps to vanilla RemoteChatSession.Data
//ExpiresAt is epoch milliseconds, Key is the encoded public key, Signature signs the profile id plus expiry
public readonly record struct ChatSessionData(Guid SessionId, long ExpiresAt, byte[] Key, byte[] Signature);

//PlayerInfoEntry player info entry, maps to vanilla ClientboundPlayerInfoUpdatePacket.Entry
//Only the actions present in the packet bitmask are written; the rest keep their previous or default value
public sealed record PlayerInfoEntry(
    Guid ProfileId,
    string Name,
    GameType GameMode,
    bool Listed,
    int Latency,
    Component? DisplayName)
{
    public bool ShowHat { get; init; } = true;
    public int ListOrder { get; init; }
    public ChatSessionData? ChatSession { get; init; }

    //EncodeEntry writes entry data in action declaration order
    public static void EncodeEntry(FriendlyByteBuf buf, PlayerInfoEntry entry, int actions)
    {
        buf.WriteUuid(entry.ProfileId);
        if ((actions & (1 << (int)PlayerInfoAction.AddPlayer)) != 0)
        {
            buf.WriteString(entry.Name);
            //Properties list of 0; the client does not consume texture/skin properties yet
            buf.WriteVarInt(0);
        }
        if ((actions & (1 << (int)PlayerInfoAction.InitializeChat)) != 0)
        {
            buf.WriteBoolean(entry.ChatSession is not null);
            if (entry.ChatSession is { } chat)
            {
                buf.WriteUuid(chat.SessionId);
                buf.WriteLong(chat.ExpiresAt);
                buf.WriteByteArray(chat.Key);
                buf.WriteByteArray(chat.Signature);
            }
        }
        if ((actions & (1 << (int)PlayerInfoAction.UpdateGameMode)) != 0)
            buf.WriteVarInt(entry.GameMode.Id);
        if ((actions & (1 << (int)PlayerInfoAction.UpdateListed)) != 0)
            buf.WriteBoolean(entry.Listed);
        if ((actions & (1 << (int)PlayerInfoAction.UpdateLatency)) != 0)
            buf.WriteVarInt(entry.Latency);
        if ((actions & (1 << (int)PlayerInfoAction.UpdateDisplayName)) != 0)
        {
            buf.WriteBoolean(entry.DisplayName is not null);
            if (entry.DisplayName is not null)
                ComponentSerialization.StreamCodec.Encode(buf, entry.DisplayName);
        }
        if ((actions & (1 << (int)PlayerInfoAction.UpdateListOrder)) != 0)
            buf.WriteVarInt(entry.ListOrder);
        if ((actions & (1 << (int)PlayerInfoAction.UpdateHat)) != 0)
            buf.WriteBoolean(entry.ShowHat);
    }

    //DecodeEntry reads entry data in action declaration order, mirroring EncodeEntry
    public static PlayerInfoEntry DecodeEntry(FriendlyByteBuf buf, int actions)
    {
        var profileId = buf.ReadUuid();
        var name = string.Empty;
        GameType gameMode = GameType.Survival;
        var listed = false;
        var latency = 0;
        Component? displayName = null;
        var listOrder = 0;
        var showHat = true;
        ChatSessionData? chatSession = null;
        if ((actions & (1 << (int)PlayerInfoAction.AddPlayer)) != 0)
        {
            name = buf.ReadString();
            var propertyCount = buf.ReadVarInt();
            for (var p = 0; p < propertyCount; p++)
            {
                buf.ReadString();
                buf.ReadString();
                if (buf.ReadBoolean()) buf.ReadByteArray();
            }
        }
        if ((actions & (1 << (int)PlayerInfoAction.InitializeChat)) != 0)
        {
            if (buf.ReadBoolean())
            {
                var sessionId = buf.ReadUuid();
                var expiresAt = buf.ReadLong();
                var key = buf.ReadByteArray();
                var signature = buf.ReadByteArray();
                chatSession = new ChatSessionData(sessionId, expiresAt, key, signature);
            }
        }
        if ((actions & (1 << (int)PlayerInfoAction.UpdateGameMode)) != 0)
            gameMode = GameType.ById(buf.ReadVarInt()) ?? GameType.Survival;
        if ((actions & (1 << (int)PlayerInfoAction.UpdateListed)) != 0)
            listed = buf.ReadBoolean();
        if ((actions & (1 << (int)PlayerInfoAction.UpdateLatency)) != 0)
            latency = buf.ReadVarInt();
        if ((actions & (1 << (int)PlayerInfoAction.UpdateDisplayName)) != 0)
        {
            if (buf.ReadBoolean()) displayName = ComponentSerialization.StreamCodec.Decode(buf);
        }
        if ((actions & (1 << (int)PlayerInfoAction.UpdateListOrder)) != 0)
            listOrder = buf.ReadVarInt();
        if ((actions & (1 << (int)PlayerInfoAction.UpdateHat)) != 0)
            showHat = buf.ReadBoolean();
        return new PlayerInfoEntry(profileId, name, gameMode, listed, latency, displayName)
        {
            ListOrder = listOrder,
            ShowHat = showHat,
            ChatSession = chatSession
        };
    }
}

//ClientboundPlayerInfoUpdatePacket player info update packet, maps to vanilla ClientboundPlayerInfoUpdatePacket
//The actions bitmask marks which actions are being updated; entries is the player list for the same action set
public sealed record ClientboundPlayerInfoUpdatePacket(int Actions, IReadOnlyList<PlayerInfoEntry> Entries) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundPlayerInfoUpdatePacket> StreamCodec { get; } = new PlayerInfoUpdateCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundPlayerInfoUpdate;

    public void Handle(ClientGamePacketListener handler) => handler.HandlePlayerInfoUpdate(this);

    private sealed class PlayerInfoUpdateCodec : StreamCodec<FriendlyByteBuf, ClientboundPlayerInfoUpdatePacket>
    {
        public ClientboundPlayerInfoUpdatePacket Decode(FriendlyByteBuf buf)
        {
            var actions = buf.ReadByte();
            var count = buf.ReadVarInt();
            var entries = new PlayerInfoEntry[count];
            for (var i = 0; i < count; i++)
                entries[i] = PlayerInfoEntry.DecodeEntry(buf, actions);
            return new ClientboundPlayerInfoUpdatePacket(actions, entries);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundPlayerInfoUpdatePacket value)
        {
            buf.WriteByte((byte)value.Actions);
            buf.WriteVarInt(value.Entries.Count);
            foreach (var entry in value.Entries)
                PlayerInfoEntry.EncodeEntry(buf, entry, value.Actions);
        }
    }
}
