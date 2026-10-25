namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundSelectAdvancementsTabPacket select advancements tab packet, maps to vanilla ClientboundSelectAdvancementsTabPacket
//Field: Tab(Identifier)
public sealed record ClientboundSelectAdvancementsTabPacket(object Tab) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundSelectAdvancementsTabPacket> StreamCodec { get; } = new SelectAdvancementsTabCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundSelectAdvancementsTab;

    public void Handle(ClientGamePacketListener handler) => handler.HandleSelectAdvancementsTab(this);

    private sealed class SelectAdvancementsTabCodec : StreamCodec<FriendlyByteBuf, ClientboundSelectAdvancementsTabPacket>
    {
        //Vanilla order: nullable tab identifier
        public ClientboundSelectAdvancementsTabPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadBoolean() ? buf.ReadIdentifier() : null!);

        public void Encode(FriendlyByteBuf buf, ClientboundSelectAdvancementsTabPacket value)
        {
            var hasTab = value.Tab is NacreCraft.Registry.Identifier;
            buf.WriteBoolean(hasTab);
            if (hasTab) buf.WriteIdentifier((NacreCraft.Registry.Identifier)value.Tab!);
        }
    }
}
