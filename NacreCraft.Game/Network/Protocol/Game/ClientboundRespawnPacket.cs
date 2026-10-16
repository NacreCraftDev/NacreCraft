namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundRespawnPacket respawn packet, maps to vanilla ClientboundRespawnPacket
//Fields: commonPlayerSpawnInfo CommonPlayerSpawnInfo, dataToKeep byte
//The client needs this to switch dimensions; without it a teleport into another level is undone on the next position packet
public sealed record ClientboundRespawnPacket(CommonPlayerSpawnInfo CommonPlayerSpawnInfo, byte DataToKeep) : Packet<ClientGamePacketListener>
{
    public const byte KeepAttributeModifiers = 1;
    public const byte KeepEntityData = 2;
    public const byte KeepAllData = 3;

    public static StreamCodec<FriendlyByteBuf, ClientboundRespawnPacket> StreamCodec { get; } = new RespawnCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundRespawn;

    public void Handle(ClientGamePacketListener handler) => handler.HandleRespawn(this);

    public bool ShouldKeep(byte mask) => (DataToKeep & mask) != 0;

    private sealed class RespawnCodec : StreamCodec<FriendlyByteBuf, ClientboundRespawnPacket>
    {
        public ClientboundRespawnPacket Decode(FriendlyByteBuf buf)
            => new(CommonPlayerSpawnInfo.StaticCodec.Decode(buf), buf.ReadByte());

        public void Encode(FriendlyByteBuf buf, ClientboundRespawnPacket value)
        {
            CommonPlayerSpawnInfo.StaticCodec.Encode(buf, value.CommonPlayerSpawnInfo);
            //dataToKeep is a plain byte in vanilla; VarInt would add a length byte the client does not expect
            buf.WriteByte(value.DataToKeep);
        }
    }
}
