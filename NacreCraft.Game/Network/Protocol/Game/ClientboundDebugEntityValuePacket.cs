using NacreCraft.Game.Util.Debug;
using NacreCraft.Network;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugEntityValuePacket debug entity value packet, maps to vanilla ClientboundDebugEntityValuePacket
//Fields: EntityId(the tracked entity), Update(the subscription update payload)
public sealed record ClientboundDebugEntityValuePacket(int EntityId, Update Update) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugEntityValuePacket> StreamCodec { get; } = new DebugEntityValueCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugEntityValue;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugEntityValue(this);

    private sealed class DebugEntityValueCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugEntityValuePacket>
    {
        public ClientboundDebugEntityValuePacket Decode(RegistryFriendlyByteBuf buf)
            => new(buf.ReadVarInt(), Update.StreamCodec.Decode(buf));

        public void Encode(RegistryFriendlyByteBuf buf, ClientboundDebugEntityValuePacket value)
        {
            buf.WriteVarInt(value.EntityId);
            Update.StreamCodec.Encode(buf, value.Update);
        }
    }
}
