using NacreCraft.Game.Util.DebugChart;
using NacreCraft.Network;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugSamplePacket debug sample packet, maps to vanilla ClientboundDebugSamplePacket
//Fields: Sample(long[] debug chart samples), DebugSampleType(the sample type, written as its enum ordinal)
public sealed record ClientboundDebugSamplePacket(long[] Sample, RemoteDebugSampleType DebugSampleType) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugSamplePacket> StreamCodec { get; } = new DebugSampleCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugSample;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugSample(this);

    private sealed class DebugSampleCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugSamplePacket>
    {
        //Vanilla writes the long array first, then the enum ordinal
        public ClientboundDebugSamplePacket Decode(RegistryFriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            var samples = new long[count];
            for (var i = 0; i < count; i++) samples[i] = buf.ReadLong();
            return new ClientboundDebugSamplePacket(samples, (RemoteDebugSampleType)buf.ReadVarInt());
        }

        public void Encode(RegistryFriendlyByteBuf buf, ClientboundDebugSamplePacket value)
        {
            buf.WriteVarInt(value.Sample.Length);
            buf.WriteLongsBigEndian(value.Sample);
            buf.WriteVarInt((int)value.DebugSampleType);
        }
    }
}
