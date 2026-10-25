namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundSetDisplayObjectivePacket display objective packet, maps to vanilla ClientboundSetDisplayObjectivePacket
//Fields: Slot(DisplaySlot), ObjectiveName(String)
public sealed record ClientboundSetDisplayObjectivePacket(object Slot, string ObjectiveName) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundSetDisplayObjectivePacket> StreamCodec { get; } = new SetDisplayObjectiveCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundSetDisplayObjective;

    public void Handle(ClientGamePacketListener handler) => handler.HandleSetDisplayObjective(this);

    private sealed class SetDisplayObjectiveCodec : StreamCodec<FriendlyByteBuf, ClientboundSetDisplayObjectivePacket>
    {
        //Vanilla order: display slot id (VAR_INT) + objective name; an empty name clears the slot
        public ClientboundSetDisplayObjectivePacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadVarInt(), buf.ReadString());

        public void Encode(FriendlyByteBuf buf, ClientboundSetDisplayObjectivePacket value)
        {
            buf.WriteVarInt(value.Slot is int slot ? slot : 0);
            buf.WriteString(value.ObjectiveName);
        }
    }
}
