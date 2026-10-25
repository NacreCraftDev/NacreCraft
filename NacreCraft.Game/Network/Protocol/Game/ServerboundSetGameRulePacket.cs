using NacreCraft.Registry;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundSetGameRulePacket set game rule packet, maps to vanilla ServerboundSetGameRulePacket
//Field: Entries(List<GameRuleEntryData>)
public sealed record ServerboundSetGameRulePacket(object Entries) : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundSetGameRulePacket> StreamCodec { get; } = new SetGameRuleCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundSetGameRule;

    public void Handle(ServerGamePacketListener handler) => handler.HandleSetGameRule(this);

    private sealed class SetGameRuleCodec : StreamCodec<FriendlyByteBuf, ServerboundSetGameRulePacket>
    {
        public ServerboundSetGameRulePacket Decode(FriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var entries = new List<GameRuleEntryData>(count);
            for (var i = 0; i < count; i++)
                entries.Add(new GameRuleEntryData(buf.ReadIdentifier(), buf.ReadString()));
            return new ServerboundSetGameRulePacket(entries);
        }

        public void Encode(FriendlyByteBuf buf, ServerboundSetGameRulePacket value)
        {
            var entries = value.Entries as IReadOnlyList<GameRuleEntryData> ?? Array.Empty<GameRuleEntryData>();
            buf.WriteVarInt(entries.Count);
            foreach (var entry in entries)
            {
                buf.WriteIdentifier(entry.Key);
                buf.WriteString(entry.Value);
            }
        }
    }
}

//GameRuleEntryData one game rule key with its serialized value
public sealed record GameRuleEntryData(Identifier Key, string Value);
