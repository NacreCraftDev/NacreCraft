namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundGameRuleValuesPacket game rule values packet, maps to vanilla ClientboundGameRuleValuesPacket
//Fields: values Map ResourceKey GameRule String business type placeholder
public sealed record ClientboundGameRuleValuesPacket(object Values) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundGameRuleValuesPacket> StreamCodec { get; } = new GameRuleValuesCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundGameRuleValues;

    public void Handle(ClientGamePacketListener handler) => handler.HandleGameRuleValues(this);

    private sealed class GameRuleValuesCodec : StreamCodec<FriendlyByteBuf, ClientboundGameRuleValuesPacket>
    {
        //Vanilla format: map of game rule key to its string value
        public ClientboundGameRuleValuesPacket Decode(FriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var values = new Dictionary<NacreCraft.Registry.Identifier, string>(count);
            for (var i = 0; i < count; i++)
                values[buf.ReadIdentifier()] = buf.ReadString();
            return new ClientboundGameRuleValuesPacket(values);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundGameRuleValuesPacket value)
        {
            var values = value.Values as IReadOnlyDictionary<NacreCraft.Registry.Identifier, string>
                ?? new Dictionary<NacreCraft.Registry.Identifier, string>();
            buf.WriteVarInt(values.Count);
            foreach (var pair in values)
            {
                buf.WriteIdentifier(pair.Key);
                buf.WriteString(pair.Value);
            }
        }
    }
}
