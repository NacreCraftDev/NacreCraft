namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundAwardStatsPacket award stats packet, maps to vanilla ClientboundAwardStatsPacket
//Stats holds the stat map as (stat type registry id, stat value registry id) -> count
public sealed record ClientboundAwardStatsPacket(object Stats) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundAwardStatsPacket> StreamCodec { get; } = new AwardStatsCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundAwardStats;

    public void Handle(ClientGamePacketListener handler) => handler.HandleAwardStats(this);

    private sealed class AwardStatsCodec : StreamCodec<FriendlyByteBuf, ClientboundAwardStatsPacket>
    {
        //Vanilla STAT_VALUES_STREAM_CODEC: map size, then stat type holder and stat value holder per entry
        //Stat.STREAM_CODEC dispatches through the stat type registry, whose value codec is a plain registry id
        public ClientboundAwardStatsPacket Decode(FriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var stats = new Dictionary<(int Type, int Value), int>(count);
            for (var i = 0; i < count; i++)
            {
                var type = buf.ReadVarInt();
                var value = buf.ReadVarInt();
                stats[(type, value)] = buf.ReadVarInt();
            }
            return new ClientboundAwardStatsPacket(stats);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundAwardStatsPacket value)
        {
            var stats = value.Stats as IReadOnlyDictionary<(int Type, int Value), int>
                ?? new Dictionary<(int Type, int Value), int>();
            buf.WriteVarInt(stats.Count);
            foreach (var pair in stats)
            {
                buf.WriteVarInt(pair.Key.Type);
                buf.WriteVarInt(pair.Key.Value);
                buf.WriteVarInt(pair.Value);
            }
        }
    }
}
