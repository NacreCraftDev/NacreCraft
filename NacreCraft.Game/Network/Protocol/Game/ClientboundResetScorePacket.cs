namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundResetScorePacket reset score packet, maps to vanilla ClientboundResetScorePacket
//Fields: Owner(String), ObjectiveName(String)
public sealed record ClientboundResetScorePacket(string Owner, string ObjectiveName) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundResetScorePacket> StreamCodec { get; } = new ResetScoreCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundResetScore;

    public void Handle(ClientGamePacketListener handler) => handler.HandleResetScore(this);

    private sealed class ResetScoreCodec : StreamCodec<FriendlyByteBuf, ClientboundResetScorePacket>
    {
        //Vanilla order: owner string, then a nullable objective name
        public ClientboundResetScorePacket Decode(FriendlyByteBuf buf)
        {
            var owner = buf.ReadString();
            var objective = buf.ReadBoolean() ? buf.ReadString() : null;
            return new ClientboundResetScorePacket(owner, objective!);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundResetScorePacket value)
        {
            buf.WriteString(value.Owner);
            buf.WriteBoolean(value.ObjectiveName is not null);
            if (value.ObjectiveName is not null) buf.WriteString(value.ObjectiveName);
        }
    }
}
