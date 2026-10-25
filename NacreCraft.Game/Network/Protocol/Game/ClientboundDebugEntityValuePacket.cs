using NacreCraft.Logging;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugEntityValuePacket debug entity value packet, maps to vanilla ClientboundDebugEntityValuePacket
//Fields: EntityId(int), Update(the raw subscription payload, see below)
public sealed record ClientboundDebugEntityValuePacket(int EntityId, object Update) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundDebugEntityValuePacket> StreamCodec { get; } = new DebugEntityValueCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugEntityValue;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugEntityValue(this);

    private sealed class DebugEntityValueCodec : StreamCodec<FriendlyByteBuf, ClientboundDebugEntityValuePacket>
    {
        //The update is a DEBUG_SUBSCRIPTION registry id followed by that subscription's value codec
        //This client's subscription registry is empty and the client never subscribes, so the payload is kept verbatim
        public ClientboundDebugEntityValuePacket Decode(FriendlyByteBuf buf)
        {
            var entityId = buf.ReadVarInt();
            var payload = buf.ReadBytes(buf.ReadableBytes);
            Log.Debug($"DebugEntityValue raw payload {payload.Length} bytes, debug subscriptions are not implemented");
            return new ClientboundDebugEntityValuePacket(entityId, payload);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundDebugEntityValuePacket value)
        {
            buf.WriteVarInt(value.EntityId);
            if (value.Update is byte[] payload) buf.WriteBytes(payload);
        }
    }
}
