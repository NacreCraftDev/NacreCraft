using NacreCraft.Game.Util.Debug;
using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugBlockValuePacket debug block value packet, maps to vanilla ClientboundDebugBlockValuePacket
//Fields: BlockPos(the tracked block), Update(the subscription update payload)
public sealed record ClientboundDebugBlockValuePacket(BlockPos BlockPos, Update Update) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugBlockValuePacket> StreamCodec { get; } = new DebugBlockValueCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugBlockValue;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugBlockValue(this);

    private sealed class DebugBlockValueCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugBlockValuePacket>
    {
        public ClientboundDebugBlockValuePacket Decode(RegistryFriendlyByteBuf buf)
            => new(buf.ReadBlockPos(), Update.StreamCodec.Decode(buf));

        public void Encode(RegistryFriendlyByteBuf buf, ClientboundDebugBlockValuePacket value)
        {
            buf.WriteBlockPos(value.BlockPos);
            Update.StreamCodec.Encode(buf, value.Update);
        }
    }
}
