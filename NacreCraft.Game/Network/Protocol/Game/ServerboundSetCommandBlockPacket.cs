using NacreCraft.Game.World.Level.Block;
using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundSetCommandBlockPacket set command block packet, maps to vanilla ServerboundSetCommandBlockPacket
//Fields: Pos(BlockPos), Command(String), TrackOutput(boolean), Conditional(boolean), Automatic(boolean), Mode(CommandBlockEntity.Mode)
public sealed record ServerboundSetCommandBlockPacket(BlockPos Pos, string Command, bool TrackOutput,
    bool Conditional, bool Automatic, CommandBlockEntity.Mode Mode) : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundSetCommandBlockPacket> StreamCodec { get; } = new SetCommandBlockCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundSetCommandBlock;

    public void Handle(ServerGamePacketListener handler) => handler.HandleSetCommandBlock(this);

    private sealed class SetCommandBlockCodec : StreamCodec<FriendlyByteBuf, ServerboundSetCommandBlockPacket>
    {
        //Vanilla order: block position (packed long), command string, three flags, mode VarInt
        public ServerboundSetCommandBlockPacket Decode(FriendlyByteBuf buf)
            => new(buf.ReadBlockPos(), buf.ReadString(), buf.ReadBoolean(), buf.ReadBoolean(),
                buf.ReadBoolean(), (CommandBlockEntity.Mode)buf.ReadVarInt());

        public void Encode(FriendlyByteBuf buf, ServerboundSetCommandBlockPacket value)
        {
            buf.WriteBlockPos(value.Pos);
            buf.WriteString(value.Command);
            buf.WriteBoolean(value.TrackOutput);
            buf.WriteBoolean(value.Conditional);
            buf.WriteBoolean(value.Automatic);
            buf.WriteVarInt((int)value.Mode);
        }
    }
}
