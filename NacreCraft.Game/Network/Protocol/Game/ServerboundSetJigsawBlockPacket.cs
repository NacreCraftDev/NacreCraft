using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundSetJigsawBlockPacket set jigsaw block packet, maps to vanilla ServerboundSetJigsawBlockPacket
//Fields: Pos(BlockPos), Name/Target/Pool(Identifier), FinalState(String), Joint, SelectionPriority(int), PlacementPriority(int)
public sealed record ServerboundSetJigsawBlockPacket(BlockPos Pos, Identifier Name, Identifier Target, Identifier Pool,
    string FinalState, JointType Joint, int SelectionPriority, int PlacementPriority)
    : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundSetJigsawBlockPacket> StreamCodec { get; } =
        new SetJigsawBlockCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundSetJigsawBlock;

    public void Handle(ServerGamePacketListener handler) => handler.HandleSetJigsawBlock(this);

    private sealed class SetJigsawBlockCodec : StreamCodec<FriendlyByteBuf, ServerboundSetJigsawBlockPacket>
    {
        public ServerboundSetJigsawBlockPacket Decode(FriendlyByteBuf buf)
        {
            var pos = buf.ReadBlockPos();
            var name = buf.ReadIdentifier();
            var target = buf.ReadIdentifier();
            var pool = buf.ReadIdentifier();
            var finalState = buf.ReadString();
            //An unknown joint name falls back to aligned, maps to the byName default in vanilla
            var joint = JointTypes.TryParse(buf.ReadString()) ?? JointType.Aligned;
            var selectionPriority = buf.ReadVarInt();
            var placementPriority = buf.ReadVarInt();
            return new ServerboundSetJigsawBlockPacket(pos, name, target, pool, finalState, joint, selectionPriority,
                placementPriority);
        }

        public void Encode(FriendlyByteBuf buf, ServerboundSetJigsawBlockPacket value)
        {
            buf.WriteBlockPos(value.Pos);
            buf.WriteIdentifier(value.Name);
            buf.WriteIdentifier(value.Target);
            buf.WriteIdentifier(value.Pool);
            buf.WriteString(value.FinalState);
            buf.WriteString(JointTypes.Name(value.Joint));
            buf.WriteVarInt(value.SelectionPriority);
            buf.WriteVarInt(value.PlacementPriority);
        }
    }
}
