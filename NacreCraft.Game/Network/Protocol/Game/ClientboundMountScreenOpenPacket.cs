namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundMountScreenOpenPacket mount screen open packet, maps to vanilla ClientboundMountScreenOpenPacket
//Fields: ContainerId(int), InventoryColumns(int), EntityId(int)
public sealed record ClientboundMountScreenOpenPacket(int ContainerId, int InventoryColumns, int EntityId) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundMountScreenOpenPacket> StreamCodec { get; } = new MountScreenOpenCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundMountScreenOpen;

    public void Handle(ClientGamePacketListener handler) => handler.HandleMountScreenOpen(this);

    private sealed class MountScreenOpenCodec : StreamCodec<FriendlyByteBuf, ClientboundMountScreenOpenPacket>
    {
        public ClientboundMountScreenOpenPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadVarInt());

        public void Encode(FriendlyByteBuf buf, ClientboundMountScreenOpenPacket value)
        {
            buf.WriteVarInt(value.ContainerId);
            buf.WriteVarInt(value.InventoryColumns);
            buf.WriteVarInt(value.EntityId);
        }
    }
}
