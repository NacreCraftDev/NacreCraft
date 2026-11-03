using NacreCraft.Game.Util.Debug;
using NacreCraft.Network;

namespace NacreCraft.Game.Network.Protocol.Game;

//ClientboundDebugEventPacket debug event packet, maps to vanilla ClientboundDebugEventPacket
//Field: Event(the one-shot subscription event)
public sealed record ClientboundDebugEventPacket(Event Event) : Packet<ClientGamePacketListener>
{
    public static StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugEventPacket> StreamCodec { get; } = new DebugEventCodec();

    public PacketType<ClientGamePacketListener> Type => GamePacketTypes.ClientboundDebugEvent;

    public void Handle(ClientGamePacketListener handler) => handler.HandleDebugEvent(this);

    private sealed class DebugEventCodec : StreamCodec<RegistryFriendlyByteBuf, ClientboundDebugEventPacket>
    {
        public ClientboundDebugEventPacket Decode(RegistryFriendlyByteBuf buf)
            => new(Event.StreamCodec.Decode(buf));

        public void Encode(RegistryFriendlyByteBuf buf, ClientboundDebugEventPacket value)
            => Event.StreamCodec.Encode(buf, value.Event);
    }
}
