namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundPlayerCombatEndPacket combat end packet, maps to vanilla ClientboundPlayerCombatEndPacket
//Field: Duration(int)
public sealed record ClientboundPlayerCombatEndPacket(int Duration) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundPlayerCombatEndPacket> StreamCodec { get; } = new PlayerCombatEndCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundPlayerCombatEnd;

    public void Handle(ClientGamePacketListener handler) => handler.HandlePlayerCombatEnd(this);

    private sealed class PlayerCombatEndCodec : StreamCodec<FriendlyByteBuf, ClientboundPlayerCombatEndPacket>
    {
        public ClientboundPlayerCombatEndPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadVarInt());

        public void Encode(FriendlyByteBuf buf, ClientboundPlayerCombatEndPacket value)
            => buf.WriteVarInt(value.Duration);
    }
}
