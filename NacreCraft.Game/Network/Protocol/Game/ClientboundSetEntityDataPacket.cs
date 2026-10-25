using NacreCraft.Game.World.Items;
using NacreCraft.Network;

namespace NacreCraft.Game.Network.Protocol.Game;

//EntityDataItem entity data item, maps to vanilla SynchedEntityData.DataValue
//Index data index (in the order of the entity class's defineId), SerializerId serializer id, Value interpreted by that id
public sealed record EntityDataItem(byte Index, int SerializerId, object Value)
{
    //Byte constructs a BYTE data item
    public static EntityDataItem Byte(byte index, byte value) => new(index, EntityDataSerializers.Byte, value);

    //Pose constructs a POSE data item
    public static EntityDataItem Pose(byte index, int poseId) => new(index, EntityDataSerializers.Pose, poseId);

    //ItemStackData constructs an ITEM_STACK data item; the item held by a dropped item uses this
    public static EntityDataItem ItemStackData(byte index, ItemStack value)
        => new(index, EntityDataSerializers.ItemStackId, value);
}

//ClientboundSetEntityDataPacket entity data packet, maps to vanilla ClientboundSetEntityDataPacket
//Fields: Id(int), PackedItems(List<SynchedEntityData.DataValue<?>>)
public sealed record ClientboundSetEntityDataPacket(int Id, IReadOnlyList<EntityDataItem> PackedItems)
    : Packet<ClientGamePacketListener>
{
    //EofMarker entry end marker, maps to vanilla EOF_MARKER
    public const byte EofMarker = 255;

    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundSetEntityDataPacket> StreamCodec { get; } = new SetEntityDataCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundSetEntityData;

    public void Handle(ClientGamePacketListener handler) => handler.HandleSetEntityData(this);

    private sealed class SetEntityDataCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundSetEntityDataPacket>
    {
        //Vanilla unpack reads index (single byte) + serializerId (VarInt) + value per entry, ending at 255
        public ClientboundSetEntityDataPacket Decode(RegistryFriendlyByteBuf buf)
        {
            var id = buf.ReadVarInt();
            var items = new List<EntityDataItem>();
            while (true)
            {
                var index = buf.ReadByte();
                if (index == EofMarker) break;
                var serializerId = buf.ReadVarInt();
                items.Add(new EntityDataItem(index, serializerId, ReadValue(buf, serializerId)));
            }
            return new ClientboundSetEntityDataPacket(id, items);
        }

        //Vanilla pack writes index + serializerId + value per entry and appends 255
        public void Encode(RegistryFriendlyByteBuf buf, ClientboundSetEntityDataPacket value)
        {
            buf.WriteVarInt(value.Id);
            foreach (var item in value.PackedItems)
            {
                buf.WriteByte(item.Index);
                buf.WriteVarInt(item.SerializerId);
                WriteValue(buf, item);
            }
            buf.WriteByte(EofMarker);
        }

        //ReadValue reads a value through the serializer registry; an unregistered id cannot locate the following bytes, so it must throw
        private static object ReadValue(RegistryFriendlyByteBuf buf, int serializerId)
            => EntityDataSerializers.Decode(serializerId, buf);

        private static void WriteValue(RegistryFriendlyByteBuf buf, EntityDataItem item)
            => EntityDataSerializers.Encode(item.SerializerId, buf, item.Value);
    }
}
