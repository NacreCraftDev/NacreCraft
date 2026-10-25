namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundMapItemDataPacket map item data packet, maps to vanilla ClientboundMapItemDataPacket
//Fields: MapId(map id), Scale(byte), Locked(boolean), Decorations(List<MapDecorationData> or null), ColorPatch(MapPatchData or null)
public sealed record ClientboundMapItemDataPacket(object MapId, byte Scale, bool Locked, object Decorations, object ColorPatch) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundMapItemDataPacket> StreamCodec { get; } = new MapItemDataCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundMapItemData;

    public void Handle(ClientGamePacketListener handler) => handler.HandleMapItemData(this);

    private sealed class MapItemDataCodec : StreamCodec<FriendlyByteBuf, ClientboundMapItemDataPacket>
    {
        public ClientboundMapItemDataPacket Decode(FriendlyByteBuf buf)
        {
            var mapId = buf.ReadVarInt();
            var scale = buf.ReadByte();
            var locked = buf.ReadBoolean();
            var decorations = (List<MapDecorationData>?)null;
            if (buf.ReadBoolean())
            {
                var count = buf.ReadVarInt();
                decorations = new List<MapDecorationData>(count);
                for (var i = 0; i < count; i++)
                {
                    //MapDecorationType uses a plain registry id
                    var type = buf.ReadVarInt();
                    var x = buf.ReadByte();
                    var y = buf.ReadByte();
                    var rot = buf.ReadByte();
                    var name = buf.ReadBoolean() ? ComponentSerialization.StreamCodec.Decode(buf) : null;
                    decorations.Add(new MapDecorationData(type, x, y, (byte)(rot & 0xF), name));
                }
            }
            //The color patch starts with the byte width; zero means no patch follows
            var patchWidth = buf.ReadByte();
            var patch = (MapPatchData?)null;
            if (patchWidth > 0)
            {
                var patchHeight = buf.ReadByte();
                var startX = buf.ReadByte();
                var startY = buf.ReadByte();
                var colors = buf.ReadByteArray();
                patch = new MapPatchData(startX, startY, (byte)patchWidth, patchHeight, colors);
            }
            return new ClientboundMapItemDataPacket(mapId, scale, locked, decorations!, patch!);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundMapItemDataPacket value)
        {
            buf.WriteVarInt(value.MapId is int mapId ? mapId : 0);
            buf.WriteByte(value.Scale);
            buf.WriteBoolean(value.Locked);
            if (value.Decorations is List<MapDecorationData> decorations)
            {
                buf.WriteBoolean(true);
                buf.WriteVarInt(decorations.Count);
                foreach (var decoration in decorations)
                {
                    buf.WriteVarInt(decoration.Type);
                    buf.WriteByte(decoration.X);
                    buf.WriteByte(decoration.Y);
                    buf.WriteByte(decoration.Rot);
                    buf.WriteBoolean(decoration.Name is not null);
                    if (decoration.Name is not null) ComponentSerialization.StreamCodec.Encode(buf, decoration.Name);
                }
            }
            else
            {
                buf.WriteBoolean(false);
            }
            if (value.ColorPatch is MapPatchData patch)
            {
                buf.WriteByte(patch.Width);
                buf.WriteByte(patch.Height);
                buf.WriteByte(patch.StartX);
                buf.WriteByte(patch.StartY);
                buf.WriteByteArray(patch.Colors);
            }
            else
            {
                buf.WriteByte(0);
            }
        }
    }
}

//MapDecorationData one map decoration; rot is already masked to 4 bits as vanilla does
public sealed record MapDecorationData(int Type, byte X, byte Y, byte Rot, Component? Name);

//MapPatchData a rectangular block of map colors
public sealed record MapPatchData(byte StartX, byte StartY, byte Width, byte Height, byte[] Colors);
