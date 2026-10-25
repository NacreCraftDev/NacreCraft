namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundSetPassengersPacket set passengers packet, maps to vanilla ClientboundSetPassengersPacket
//Fields: Vehicle(int), Passengers(int[]); an empty array detaches every passenger
public sealed record ClientboundSetPassengersPacket(int Vehicle, int[] Passengers) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundSetPassengersPacket> StreamCodec { get; } = new SetPassengersCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundSetPassengers;

    public void Handle(ClientGamePacketListener handler) => handler.HandleSetEntityPassengersPacket(this);

    private sealed class SetPassengersCodec : StreamCodec<FriendlyByteBuf, ClientboundSetPassengersPacket>
    {
        //Vanilla order: VAR_INT vehicle, VAR_INT count, count VAR_INT passenger ids
        public ClientboundSetPassengersPacket Decode(FriendlyByteBuf buf)
        {
            var vehicle = buf.ReadVarInt();
            var count = buf.ReadVarInt();
            var passengers = new int[count];
            for (var i = 0; i < count; i++) passengers[i] = buf.ReadVarInt();
            return new ClientboundSetPassengersPacket(vehicle, passengers);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundSetPassengersPacket value)
        {
            buf.WriteVarInt(value.Vehicle);
            buf.WriteVarInt(value.Passengers.Length);
            foreach (var id in value.Passengers) buf.WriteVarInt(id);
        }
    }
}
