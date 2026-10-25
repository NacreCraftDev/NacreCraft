namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundPlayerLookAtPacket player look at packet, maps to vanilla ClientboundPlayerLookAtPacket
//Fields: fromAnchor enum placeholder, x/y/z 3 doubles, atEntity bool, entity VarInt, toAnchor enum placeholder
//When atEntity is false, entity=0 and toAnchor=null
public sealed record ClientboundPlayerLookAtPacket(object FromAnchor, double X, double Y, double Z, bool AtEntity, int Entity, object? ToAnchor) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundPlayerLookAtPacket> StreamCodec { get; } = new PlayerLookAtCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundPlayerLookAt;

    public void Handle(ClientGamePacketListener handler) => handler.HandleLookAt(this);

    private sealed class PlayerLookAtCodec : StreamCodec<FriendlyByteBuf, ClientboundPlayerLookAtPacket>
    {
        //Vanilla order: from anchor enum, target position, at-entity flag, then entity id + to anchor when at an entity
        public ClientboundPlayerLookAtPacket Decode(FriendlyByteBuf buf)
        {
            var fromAnchor = buf.ReadVarInt();
            var x = buf.ReadDouble();
            var y = buf.ReadDouble();
            var z = buf.ReadDouble();
            var atEntity = buf.ReadBoolean();
            var entity = atEntity ? buf.ReadVarInt() : 0;
            var toAnchor = atEntity ? buf.ReadVarInt() : 0;
            return new ClientboundPlayerLookAtPacket(fromAnchor, x, y, z, atEntity, entity, toAnchor);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundPlayerLookAtPacket value)
        {
            buf.WriteVarInt(value.FromAnchor is int from ? from : 0);
            buf.WriteDouble(value.X);
            buf.WriteDouble(value.Y);
            buf.WriteDouble(value.Z);
            buf.WriteBoolean(value.AtEntity);
            if (value.AtEntity)
            {
                buf.WriteVarInt(value.Entity);
                buf.WriteVarInt(value.ToAnchor is int to ? to : 0);
            }
        }
    }
}
