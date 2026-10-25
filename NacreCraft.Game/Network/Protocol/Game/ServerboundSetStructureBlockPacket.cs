using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Primitives;
using NacreCraft.Registry.Enums;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundSetStructureBlockPacket set structure block packet, maps to vanilla ServerboundSetStructureBlockPacket
//Fields: Pos(BlockPos), UpdateType(StructureBlockEntity.UpdateType), Mode(StructureMode), Name(String), Offset(BlockPos)
//Size(Vec3i), Mirror, Rotation, Data(String), IgnoreEntities/Strict/ShowAir/ShowBoundingBox(flag bits), Integrity(float), Seed(long)
public sealed record ServerboundSetStructureBlockPacket(BlockPos Pos, StructureBlockEntity.UpdateType UpdateType,
    StructureMode Mode, string Name, BlockPos Offset, Vec3i Size, Mirror Mirror, Rotation Rotation, string Data,
    bool IgnoreEntities, bool Strict, bool ShowAir, bool ShowBoundingBox, float Integrity, long Seed)
    : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundSetStructureBlockPacket> StreamCodec { get; } =
        new SetStructureBlockCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundSetStructureBlock;

    public void Handle(ServerGamePacketListener handler) => handler.HandleSetStructureBlock(this);

    private sealed class SetStructureBlockCodec : StreamCodec<FriendlyByteBuf, ServerboundSetStructureBlockPacket>
    {
        //Flag bits of the trailing byte, maps to the FLAG_* constants of vanilla
        private const int FlagIgnoreEntities = 1;
        private const int FlagShowAir = 2;
        private const int FlagShowBoundingBox = 4;
        private const int FlagStrict = 8;

        public ServerboundSetStructureBlockPacket Decode(FriendlyByteBuf buf)
        {
            var pos = buf.ReadBlockPos();
            var updateType = (StructureBlockEntity.UpdateType)buf.ReadVarInt();
            var mode = (StructureMode)buf.ReadVarInt();
            var name = buf.ReadString();
            //Offsets are signed bytes; the reader hands back an unsigned byte, so the cast restores the sign
            var offset = new BlockPos(
                Math.Clamp((int)(sbyte)buf.ReadByte(), -StructureBlockEntity.MaxOffsetPerAxis,
                    StructureBlockEntity.MaxOffsetPerAxis),
                Math.Clamp((int)(sbyte)buf.ReadByte(), -StructureBlockEntity.MaxOffsetPerAxis,
                    StructureBlockEntity.MaxOffsetPerAxis),
                Math.Clamp((int)(sbyte)buf.ReadByte(), -StructureBlockEntity.MaxOffsetPerAxis,
                    StructureBlockEntity.MaxOffsetPerAxis));
            var size = new Vec3i(
                Math.Clamp((int)buf.ReadByte(), 0, StructureBlockEntity.MaxSizePerAxis),
                Math.Clamp((int)buf.ReadByte(), 0, StructureBlockEntity.MaxSizePerAxis),
                Math.Clamp((int)buf.ReadByte(), 0, StructureBlockEntity.MaxSizePerAxis));
            var mirror = (Mirror)buf.ReadVarInt();
            var rotation = (Rotation)buf.ReadVarInt();
            var data = buf.ReadString(128);
            var integrity = Math.Clamp(buf.ReadFloat(), 0f, 1f);
            var seed = buf.ReadVarLong();
            var flags = buf.ReadByte();
            return new ServerboundSetStructureBlockPacket(pos, updateType, mode, name, offset, size, mirror, rotation,
                data, (flags & FlagIgnoreEntities) != 0, (flags & FlagStrict) != 0, (flags & FlagShowAir) != 0,
                (flags & FlagShowBoundingBox) != 0, integrity, seed);
        }

        public void Encode(FriendlyByteBuf buf, ServerboundSetStructureBlockPacket value)
        {
            buf.WriteBlockPos(value.Pos);
            buf.WriteVarInt((int)value.UpdateType);
            buf.WriteVarInt((int)value.Mode);
            buf.WriteString(value.Name);
            buf.WriteByte((byte)(sbyte)value.Offset.X);
            buf.WriteByte((byte)(sbyte)value.Offset.Y);
            buf.WriteByte((byte)(sbyte)value.Offset.Z);
            buf.WriteByte((byte)value.Size.X);
            buf.WriteByte((byte)value.Size.Y);
            buf.WriteByte((byte)value.Size.Z);
            buf.WriteVarInt((int)value.Mirror);
            buf.WriteVarInt((int)value.Rotation);
            buf.WriteString(value.Data, 128);
            buf.WriteFloat(value.Integrity);
            buf.WriteVarLong(value.Seed);
            var flags = 0;
            if (value.IgnoreEntities) flags |= FlagIgnoreEntities;
            if (value.ShowAir) flags |= FlagShowAir;
            if (value.ShowBoundingBox) flags |= FlagShowBoundingBox;
            if (value.Strict) flags |= FlagStrict;
            buf.WriteByte((byte)flags);
        }
    }
}
