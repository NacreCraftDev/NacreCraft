using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundTestInstanceBlockActionPacket test instance block action packet, maps to vanilla ServerboundTestInstanceBlockActionPacket
//Fields: Pos(BlockPos), Action(0 init, 1 query, 2 set, 3 reset, 4 save, 5 export, 6 run), Data(TestInstanceBlockData)
public sealed record ServerboundTestInstanceBlockActionPacket(object Pos, object Action, object Data) : Packet<ServerGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ServerboundTestInstanceBlockActionPacket> StreamCodec { get; } = new TestInstanceBlockActionCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundTestInstanceBlockAction;

    public void Handle(ServerGamePacketListener handler) => handler.HandleTestInstanceBlockAction(this);

    private sealed class TestInstanceBlockActionCodec : StreamCodec<FriendlyByteBuf, ServerboundTestInstanceBlockActionPacket>
    {
        public ServerboundTestInstanceBlockActionPacket Decode(FriendlyByteBuf buf)
        {
            var pos = buf.ReadBlockPos();
            var action = buf.ReadVarInt();
            Identifier? test = buf.ReadBoolean() ? buf.ReadIdentifier() : null;
            var size = new Vec3i(buf.ReadVarInt(), buf.ReadVarInt(), buf.ReadVarInt());
            var rotation = buf.ReadVarInt();
            var ignoreEntities = buf.ReadBoolean();
            var status = buf.ReadVarInt();
            var errorMessage = buf.ReadBoolean() ? ComponentSerialization.StreamCodec.Decode(buf) : null;
            var data = new TestInstanceBlockData(test, size, rotation, ignoreEntities, status, errorMessage);
            return new ServerboundTestInstanceBlockActionPacket(pos, action, data);
        }

        public void Encode(FriendlyByteBuf buf, ServerboundTestInstanceBlockActionPacket value)
        {
            buf.WriteBlockPos(value.Pos is BlockPos pos ? pos : default);
            buf.WriteVarInt(value.Action is int action ? action : 0);
            if (value.Data is not TestInstanceBlockData data)
                throw new InvalidOperationException("Test instance block data payload is missing");
            buf.WriteBoolean(data.Test is not null);
            if (data.Test is Identifier test) buf.WriteIdentifier(test);
            buf.WriteVarInt(data.Size.X);
            buf.WriteVarInt(data.Size.Y);
            buf.WriteVarInt(data.Size.Z);
            buf.WriteVarInt(data.Rotation);
            buf.WriteBoolean(data.IgnoreEntities);
            buf.WriteVarInt(data.Status);
            buf.WriteBoolean(data.ErrorMessage is not null);
            if (data.ErrorMessage is not null) ComponentSerialization.StreamCodec.Encode(buf, data.ErrorMessage);
        }
    }
}

//TestInstanceBlockData test instance configuration; Rotation and Status are enum ids
public sealed record TestInstanceBlockData(
    Identifier? Test,
    Vec3i Size,
    int Rotation,
    bool IgnoreEntities,
    int Status,
    Component? ErrorMessage);
