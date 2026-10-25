namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugSamplePacket debug sample packet, maps to vanilla ClientboundDebugSamplePacket
//Fields: DebugSampleType(the sample type id), Sample(long[] debug chart samples)
public sealed record ClientboundDebugSamplePacket(object DebugSampleType, long[] Sample) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<FriendlyByteBuf, ClientboundDebugSamplePacket> StreamCodec { get; } = new DebugSampleCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugSample;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugSample(this);

    private sealed class DebugSampleCodec : StreamCodec<FriendlyByteBuf, ClientboundDebugSamplePacket>
    {
        //Vanilla reads the sample array before the enum, and writeLongArray is a VAR_INT count followed by fixed longs
        public ClientboundDebugSamplePacket Decode(FriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var samples = new long[count];
            for (var i = 0; i < count; i++) samples[i] = buf.ReadLong();
            var type = buf.ReadVarInt();
            return new ClientboundDebugSamplePacket(type, samples);
        }

        public void Encode(FriendlyByteBuf buf, ClientboundDebugSamplePacket value)
        {
            buf.WriteVarInt(value.Sample.Length);
            buf.WriteLongsBigEndian(value.Sample);
            buf.WriteVarInt(value.DebugSampleType is int type ? type : 0);
        }
    }
}
