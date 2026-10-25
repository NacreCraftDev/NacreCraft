using NacreCraft.Logging;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugChunkValuePacket debug chunk value packet, maps to vanilla ClientboundDebugChunkValuePacket
//Fields: ChunkPos(ChunkPos), Update(the raw subscription payload, see below)
public sealed record ClientboundDebugChunkValuePacket(object ChunkPos, object Update) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundDebugChunkValuePacket> StreamCodec { get; } = new DebugChunkValueCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugChunkValue;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugChunkValue(this);

    private sealed class DebugChunkValueCodec : StreamCodec<FriendlyByteBuf, ClientboundDebugChunkValuePacket>
    {
        //The update is a DEBUG_SUBSCRIPTION registry id followed by that subscription's value codec
        //This client's subscription registry is empty and the client never subscribes, so the payload is kept verbatim
        public ClientboundDebugChunkValuePacket Decode(FriendlyByteBuf buf)
        {
            var chunkPos = NacreCraft.Primitives.ChunkPos.Unpack(buf.ReadLong());
            var payload = buf.ReadBytes(buf.ReadableBytes);
            Log.Debug($"DebugChunkValue raw payload {payload.Length} bytes, debug subscriptions are not implemented");
            return new ClientboundDebugChunkValuePacket(chunkPos, payload);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundDebugChunkValuePacket value)
        {
            buf.WriteLong(value.ChunkPos is ChunkPos pos ? pos.Pack() : 0L);
            if (value.Update is byte[] payload) buf.WriteBytes(payload);
        }
    }
}
