namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundBossEventPacket boss bar event packet, maps to vanilla ClientboundBossEventPacket
//Fields: Id UUID, Operation BossEventOperation carrying the decoded operation payload
public sealed record ClientboundBossEventPacket(Guid Id, object Operation) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundBossEventPacket> StreamCodec { get; } = new BossEventCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundBossEvent;

    public void Handle(ClientGamePacketListener handler) => handler.HandleBossUpdate(this);

    //Operation types in vanilla declaration order: add, remove, update_progress, update_name, update_style, update_properties
    public const int Add = 0;
    public const int Remove = 1;
    public const int UpdateProgress = 2;
    public const int UpdateName = 3;
    public const int UpdateStyle = 4;
    public const int UpdateProperties = 5;

    private sealed class BossEventCodec : StreamCodec<FriendlyByteBuf, ClientboundBossEventPacket>
    {
        public ClientboundBossEventPacket Decode(FriendlyByteBuf buf)
        {
            var id = buf.ReadUuid();
            var type = buf.ReadVarInt();
            var name = (Component?)null;
            var progress = 0f;
            var color = 0;
            var overlay = 0;
            var darkenScreen = false;
            var playMusic = false;
            var createWorldFog = false;
            switch (type)
            {
                case Add:
                    name = ComponentSerialization.StreamCodec.Decode(buf);
                    progress = buf.ReadFloat();
                    color = buf.ReadVarInt();
                    overlay = buf.ReadVarInt();
                    var addFlags = buf.ReadByte();
                    darkenScreen = (addFlags & 1) != 0;
                    playMusic = (addFlags & 2) != 0;
                    createWorldFog = (addFlags & 4) != 0;
                    break;
                case Remove:
                    break;
                case UpdateProgress:
                    progress = buf.ReadFloat();
                    break;
                case UpdateName:
                    name = ComponentSerialization.StreamCodec.Decode(buf);
                    break;
                case UpdateStyle:
                    color = buf.ReadVarInt();
                    overlay = buf.ReadVarInt();
                    break;
                case UpdateProperties:
                    var propertyFlags = buf.ReadByte();
                    darkenScreen = (propertyFlags & 1) != 0;
                    playMusic = (propertyFlags & 2) != 0;
                    createWorldFog = (propertyFlags & 4) != 0;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown boss event operation {type}");
            }
            return new ClientboundBossEventPacket(id,
                new BossEventOperation(type, name, progress, color, overlay, darkenScreen, playMusic, createWorldFog));
        }

        public void Encode(FriendlyByteBuf buf, ClientboundBossEventPacket value)
        {
            buf.WriteUuid(value.Id);
            var operation = value.Operation as BossEventOperation
                ?? throw new InvalidOperationException("Boss event operation payload is missing");
            buf.WriteVarInt(operation.Type);
            switch (operation.Type)
            {
                case Add:
                    ComponentSerialization.StreamCodec.Encode(buf, operation.Name ?? CommonComponents.Empty);
                    buf.WriteFloat(operation.Progress);
                    buf.WriteVarInt(operation.Color);
                    buf.WriteVarInt(operation.Overlay);
                    buf.WriteByte(EncodeFlags(operation));
                    break;
                case Remove:
                    break;
                case UpdateProgress:
                    buf.WriteFloat(operation.Progress);
                    break;
                case UpdateName:
                    ComponentSerialization.StreamCodec.Encode(buf, operation.Name ?? CommonComponents.Empty);
                    break;
                case UpdateStyle:
                    buf.WriteVarInt(operation.Color);
                    buf.WriteVarInt(operation.Overlay);
                    break;
                case UpdateProperties:
                    buf.WriteByte(EncodeFlags(operation));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown boss event operation {operation.Type}");
            }
        }

        //EncodeFlags packs darkenScreen=1, playMusic=2, createWorldFog=4
        private static byte EncodeFlags(BossEventOperation operation)
        {
            var flags = 0;
            if (operation.DarkenScreen) flags |= 1;
            if (operation.PlayMusic) flags |= 2;
            if (operation.CreateWorldFog) flags |= 4;
            return (byte)flags;
        }
    }
}

//BossEventOperation decoded boss event payload; unused fields keep their default for the operation type
public sealed record BossEventOperation(
    int Type,
    Component? Name,
    float Progress,
    int Color,
    int Overlay,
    bool DarkenScreen,
    bool PlayMusic,
    bool CreateWorldFog);
