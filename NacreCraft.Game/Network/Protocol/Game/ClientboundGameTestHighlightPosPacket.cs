using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundGameTestHighlightPosPacket game test highlight pos packet, maps to vanilla ClientboundGameTestHighlightPosPacket
//Fields: AbsolutePos(BlockPos), RelativePos(BlockPos)
public sealed record ClientboundGameTestHighlightPosPacket(object AbsolutePos, object RelativePos) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundGameTestHighlightPosPacket> StreamCodec { get; } = new GameTestHighlightPosCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundGameTestHighlightPos;

    public void Handle(ClientGamePacketListener handler) => handler.HandleGameTestHighlightPos(this);

    private sealed class GameTestHighlightPosCodec : StreamCodec<FriendlyByteBuf, ClientboundGameTestHighlightPosPacket>
    {
        public ClientboundGameTestHighlightPosPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadBlockPos(), buf.ReadBlockPos());

        public void Encode(FriendlyByteBuf buf, ClientboundGameTestHighlightPosPacket value)
        {
            buf.WriteBlockPos(value.AbsolutePos is BlockPos absolute ? absolute : default);
            buf.WriteBlockPos(value.RelativePos is BlockPos relative ? relative : default);
        }
    }
}
