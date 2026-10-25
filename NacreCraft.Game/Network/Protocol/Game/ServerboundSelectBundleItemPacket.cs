namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundSelectBundleItemPacket select bundle item packet, maps to vanilla ServerboundSelectBundleItemPacket
//Fields: SlotId(int), SelectedItemIndex(int); the index is -1 when no item is selected
public sealed record ServerboundSelectBundleItemPacket(int SlotId, int SelectedItemIndex) : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundSelectBundleItemPacket> StreamCodec { get; } = new SelectBundleItemCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundBundleItemSelected;

    public void Handle(ServerGamePacketListener handler) => handler.HandleBundleItemSelectedPacket(this);

    private sealed class SelectBundleItemCodec : StreamCodec<FriendlyByteBuf, ServerboundSelectBundleItemPacket>
    {
        public ServerboundSelectBundleItemPacket Decode(FriendlyByteBuf buf)
        {
            var slotId = buf.ReadVarInt();
            var index = buf.ReadVarInt();
            if (index < 0 && index != -1)
                throw new InvalidOperationException($"Invalid selected item index {index}");
            return new ServerboundSelectBundleItemPacket(slotId, index);
        }

        public void Encode(FriendlyByteBuf buf, ServerboundSelectBundleItemPacket value)
        {
            buf.WriteVarInt(value.SlotId);
            buf.WriteVarInt(value.SelectedItemIndex);
        }
    }
}
