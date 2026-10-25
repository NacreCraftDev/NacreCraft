using NacreCraft.Logging;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugBlockValuePacket debug block value packet, maps to vanilla ClientboundDebugBlockValuePacket
//Fields: BlockPos(BlockPos), Update(the raw subscription payload, see below)
public sealed record ClientboundDebugBlockValuePacket(object BlockPos, object Update) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundDebugBlockValuePacket> StreamCodec { get; } = new DebugBlockValueCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugBlockValue;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugBlockValue(this);

    private sealed class DebugBlockValueCodec : StreamCodec<FriendlyByteBuf, ClientboundDebugBlockValuePacket>
    {
        //The update is a DEBUG_SUBSCRIPTION registry id followed by that subscription's value codec
        //This client's subscription registry is empty and the client never subscribes, so the payload is kept verbatim
        public ClientboundDebugBlockValuePacket Decode(FriendlyByteBuf buf)
        {
            var pos = buf.ReadBlockPos();
            var payload = buf.ReadBytes(buf.ReadableBytes);
            Log.Debug($"DebugBlockValue raw payload {payload.Length} bytes, debug subscriptions are not implemented");
            return new ClientboundDebugBlockValuePacket(pos, payload);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundDebugBlockValuePacket value)
        {
            buf.WriteBlockPos(value.BlockPos is BlockPos pos ? pos : default);
            if (value.Update is byte[] payload) buf.WriteBytes(payload);
        }
    }
}
