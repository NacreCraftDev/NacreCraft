namespace NacreCraft.Game.Network.Protocol.Game;

using NacreCraft.Network.Chat.Numbers;

//ClientboundSetObjectivePacket objective packet, maps to vanilla ClientboundSetObjectivePacket
//Fields: ObjectiveName(String), DisplayName(Component), RenderType(ObjectiveCriteria.RenderType), NumberFormat(Optional<NumberFormat>), Method(int)
public sealed record ClientboundSetObjectivePacket(string ObjectiveName, Component DisplayName, object RenderType, NumberFormat? NumberFormat, int Method) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundSetObjectivePacket> StreamCodec { get; } = new SetObjectiveCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundSetObjective;

    public void Handle(ClientGamePacketListener handler) => handler.HandleAddObjective(this);

    private sealed class SetObjectiveCodec : StreamCodec<FriendlyByteBuf, ClientboundSetObjectivePacket>
    {
        //Vanilla order: name, method byte, then displayName/renderType/numberFormat only for add and change
        public ClientboundSetObjectivePacket Decode(FriendlyByteBuf buf)
        {
            var name = buf.ReadString();
            var method = buf.ReadByte();
            if (method is 0 or 2)
            {
                var displayName = ComponentSerialization.StreamCodec.Decode(buf);
                var renderType = buf.ReadVarInt();
                var numberFormat = NumberFormatTypes.OptionalStreamCodec.Decode(buf);
                return new ClientboundSetObjectivePacket(name, displayName, renderType, numberFormat, method);
            }
            return new ClientboundSetObjectivePacket(name, CommonComponents.Empty, 0, null, method);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundSetObjectivePacket value)
        {
            buf.WriteString(value.ObjectiveName);
            buf.WriteByte((byte)value.Method);
            if (value.Method is 0 or 2)
            {
                ComponentSerialization.StreamCodec.Encode(buf, value.DisplayName);
                buf.WriteVarInt(value.RenderType is int renderType ? renderType : 0);
                NumberFormatTypes.OptionalStreamCodec.Encode(buf, value.NumberFormat);
            }
        }
    }
}
