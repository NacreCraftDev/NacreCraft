using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundSetTestBlockPacket set test block packet, maps to vanilla ServerboundSetTestBlockPacket
//Fields: Position(BlockPos), Mode(TestBlockMode id), Message(String)
public sealed record ServerboundSetTestBlockPacket(object Position, object Mode, string Message) : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundSetTestBlockPacket> StreamCodec { get; } = new SetTestBlockCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundSetTestBlock;

    public void Handle(ServerGamePacketListener handler) => handler.HandleSetTestBlock(this);

    private sealed class SetTestBlockCodec : StreamCodec<FriendlyByteBuf, ServerboundSetTestBlockPacket>
    {
        //TestBlockMode: 0 start, 1 log, 2 fail, 3 accept
        public ServerboundSetTestBlockPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadBlockPos(), buf.ReadVarInt(), buf.ReadString());

        public void Encode(FriendlyByteBuf buf, ServerboundSetTestBlockPacket value)
        {
            buf.WriteBlockPos(value.Position is BlockPos position ? position : default);
            buf.WriteVarInt(value.Mode is int mode ? mode : 0);
            buf.WriteString(value.Message);
        }
    }
}
