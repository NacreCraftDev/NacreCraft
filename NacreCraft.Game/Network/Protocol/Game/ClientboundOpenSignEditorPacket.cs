using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundOpenSignEditorPacket open sign editor packet, maps to vanilla ClientboundOpenSignEditorPacket
//Fields: Pos(BlockPos), IsFrontText(boolean)
public sealed record ClientboundOpenSignEditorPacket(object Pos, bool IsFrontText) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundOpenSignEditorPacket> StreamCodec { get; } = new OpenSignEditorCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundOpenSignEditor;

    public void Handle(ClientGamePacketListener handler) => handler.HandleOpenSignEditor(this);

    private sealed class OpenSignEditorCodec : StreamCodec<FriendlyByteBuf, ClientboundOpenSignEditorPacket>
    {
        public ClientboundOpenSignEditorPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadBlockPos(), buf.ReadBoolean());

        public void Encode(FriendlyByteBuf buf, ClientboundOpenSignEditorPacket value)
        {
            buf.WriteBlockPos(value.Pos is BlockPos pos ? pos : default);
            buf.WriteBoolean(value.IsFrontText);
        }
    }
}
