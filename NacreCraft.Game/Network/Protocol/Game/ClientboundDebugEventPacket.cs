using NacreCraft.Logging;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugEventPacket debug event packet, maps to vanilla ClientboundDebugEventPacket
//Field: Event(the raw subscription payload, see below)
public sealed record ClientboundDebugEventPacket(object Event) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundDebugEventPacket> StreamCodec { get; } = new DebugEventCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugEvent;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugEvent(this);

    private sealed class DebugEventCodec : StreamCodec<FriendlyByteBuf, ClientboundDebugEventPacket>
    {
        //The event is a DEBUG_SUBSCRIPTION registry id followed by that subscription's value codec
        //This client's subscription registry is empty and the client never subscribes, so the payload is kept verbatim
        public ClientboundDebugEventPacket Decode(FriendlyByteBuf buf)
        {
            var payload = buf.ReadBytes(buf.ReadableBytes);
            Log.Debug($"DebugEvent raw payload {payload.Length} bytes, debug subscriptions are not implemented");
            return new ClientboundDebugEventPacket(payload);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundDebugEventPacket value)
        {
            if (value.Event is byte[] payload) buf.WriteBytes(payload);
        }
    }
}
