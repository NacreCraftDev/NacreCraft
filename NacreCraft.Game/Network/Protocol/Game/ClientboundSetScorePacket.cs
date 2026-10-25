namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundSetScorePacket set score packet, maps to vanilla ClientboundSetScorePacket
//Fields: Owner(String), ObjectiveName(String), Score(int), Display(Optional<Component>), NumberFormat(Optional<NumberFormat>)
public sealed record ClientboundSetScorePacket(string Owner, string ObjectiveName, int Score, object Display, object NumberFormat) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundSetScorePacket> StreamCodec { get; } = new SetScoreCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundSetScore;

    public void Handle(ClientGamePacketListener handler) => handler.HandleSetScore(this);

    private sealed class SetScoreCodec : StreamCodec<FriendlyByteBuf, ClientboundSetScorePacket>
    {
        //Vanilla order: owner, objective, score, optional display component, optional number format
        public ClientboundSetScorePacket Decode(FriendlyByteBuf buf)
        {
            var owner = buf.ReadString();
            var objective = buf.ReadString();
            var score = buf.ReadVarInt();
            var display = buf.ReadBoolean() ? ComponentSerialization.StreamCodec.Decode(buf) : null;
            object? numberFormat = null;
            if (buf.ReadBoolean())
            {
                var id = buf.ReadVarInt();
                if (id == 1) buf.ReadNbt();
                else if (id == 2) ComponentSerialization.StreamCodec.Decode(buf);
                numberFormat = id;
            }
            return new ClientboundSetScorePacket(owner, objective, score, display!, numberFormat!);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundSetScorePacket value)
        {
            buf.WriteString(value.Owner);
            buf.WriteString(value.ObjectiveName);
            buf.WriteVarInt(value.Score);
            buf.WriteBoolean(value.Display is Component);
            if (value.Display is Component display)
                ComponentSerialization.StreamCodec.Encode(buf, display);
            //The number format payload is kept as its registry id only, so it is not written back
            buf.WriteBoolean(false);
        }
    }
}
