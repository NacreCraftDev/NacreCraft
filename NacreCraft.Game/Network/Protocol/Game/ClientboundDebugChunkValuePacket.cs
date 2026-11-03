using NacreCraft.Game.Util.Debug;
using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugChunkValuePacket debug chunk value packet, maps to vanilla ClientboundDebugChunkValuePacket
//Fields: ChunkPos(the tracked chunk), Update(the subscription update payload)
public sealed record ClientboundDebugChunkValuePacket(ChunkPos ChunkPos, Update Update) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugChunkValuePacket> StreamCodec { get; } = new DebugChunkValueCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugChunkValue;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugChunkValue(this);

    private sealed class DebugChunkValueCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugChunkValuePacket>
    {
        public ClientboundDebugChunkValuePacket Decode(RegistryFriendlyByteBuf buf)
            => new(ChunkPos.Unpack(buf.ReadLong()), Update.StreamCodec.Decode(buf));

        public void Encode(RegistryFriendlyByteBuf buf, ClientboundDebugChunkValuePacket value)
        {
            buf.WriteLong(value.ChunkPos.Pack());
            Update.StreamCodec.Encode(buf, value.Update);
        }
    }
}
