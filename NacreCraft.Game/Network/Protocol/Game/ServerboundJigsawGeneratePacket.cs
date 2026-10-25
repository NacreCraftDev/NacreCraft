using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundJigsawGeneratePacket jigsaw generate packet, maps to vanilla ServerboundJigsawGeneratePacket
//Fields: Pos(BlockPos), Levels(int), KeepJigsaws(boolean)
public sealed record ServerboundJigsawGeneratePacket(BlockPos Pos, int Levels, bool KeepJigsaws)
    : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundJigsawGeneratePacket> StreamCodec { get; } =
        new JigsawGenerateCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundJigsawGenerate;

    public void Handle(ServerGamePacketListener handler) => handler.HandleJigsawGenerate(this);

    private sealed class JigsawGenerateCodec : StreamCodec<FriendlyByteBuf, ServerboundJigsawGeneratePacket>
    {
        public ServerboundJigsawGeneratePacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadBlockPos(), buf.ReadVarInt(), buf.ReadBoolean());

        public void Encode(FriendlyByteBuf buf, ServerboundJigsawGeneratePacket value)
        {
            buf.WriteBlockPos(value.Pos);
            buf.WriteVarInt(value.Levels);
            buf.WriteBoolean(value.KeepJigsaws);
        }
    }
}
