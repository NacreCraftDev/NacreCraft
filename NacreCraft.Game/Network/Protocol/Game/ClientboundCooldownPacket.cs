namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundCooldownPacket item cooldown packet, maps to vanilla ClientboundCooldownPacket
//Fields: CooldownGroup(Identifier), Duration(int)
public sealed record ClientboundCooldownPacket(object CooldownGroup, int Duration) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundCooldownPacket> StreamCodec { get; } = new CooldownCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundCooldown;

    public void Handle(ClientGamePacketListener handler) => handler.HandleItemCooldown(this);

    private sealed class CooldownCodec : StreamCodec<FriendlyByteBuf, ClientboundCooldownPacket>
    {
        //Vanilla order: cooldown group identifier + VAR_INT duration in ticks
        public ClientboundCooldownPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadIdentifier(), buf.ReadVarInt());

        public void Encode(FriendlyByteBuf buf, ClientboundCooldownPacket value)
        {
            buf.WriteIdentifier(value.CooldownGroup is NacreCraft.Registry.Identifier id ? id : NacreCraft.Registry.Identifier.WithDefaultNamespace("empty"));
            buf.WriteVarInt(value.Duration);
        }
    }
}
