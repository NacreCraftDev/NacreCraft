using NacreCraft.Game.Util.Debug;
using NacreCraft.Network;

namespace NacreCraft.Game.Network.Protocol.Game;

//ServerboundDebugSubscriptionRequestPacket debug subscription request, maps to vanilla ServerboundDebugSubscriptionRequestPacket
//Field: Subscriptions(the set of debug subscriptions the player enabled, encoded as registry ids)
public sealed record ServerboundDebugSubscriptionRequestPacket(HashSet<DebugSubscription> Subscriptions) : Packet<ServerGamePacketListener>
{
    //Vanilla caps the decoded set at 32 entries
    private const int MaxSubscriptions = 32;

    public static StreamCodec<RegistryFriendlyByteBuf, ServerboundDebugSubscriptionRequestPacket> StreamCodec { get; } = new DebugSubscriptionRequestCodec();

    public PacketType<ServerGamePacketListener> Type => GamePacketTypes.ServerboundDebugSubscriptionRequest;

    public void Handle(ServerGamePacketListener handler) => handler.HandleDebugSubscriptionRequest(this);

    private sealed class DebugSubscriptionRequestCodec : StreamCodec<RegistryFriendlyByteBuf, ServerboundDebugSubscriptionRequestPacket>
    {
        public ServerboundDebugSubscriptionRequestPacket Decode(RegistryFriendlyByteBuf buf)
        {
            var count = buf.ReadVarInt();
            if (count > MaxSubscriptions)
                throw new InvalidOperationException($"debug subscription count out of range {count} > {MaxSubscriptions}");
            var registry = buf.Lookup(Registries.DEBUG_SUBSCRIPTION);
            var subscriptions = new HashSet<DebugSubscription>();
            for (var i = 0; i < count; i++)
            {
                var id = buf.ReadVarInt();
                var subscription = registry.ById(id) as DebugSubscription
                    ?? throw new InvalidOperationException($"unknown debug subscription id {id}");
                subscriptions.Add(subscription);
            }
            return new ServerboundDebugSubscriptionRequestPacket(subscriptions);
        }

        public void Encode(RegistryFriendlyByteBuf buf, ServerboundDebugSubscriptionRequestPacket value)
        {
            var registry = buf.Lookup(Registries.DEBUG_SUBSCRIPTION);
            buf.WriteVarInt(value.Subscriptions.Count);
            foreach (var subscription in value.Subscriptions)
            {
                var id = registry.GetId(subscription);
                if (id == IdMap<object>.Default)
                    throw new InvalidOperationException($"debug subscription not registered: {subscription}");
                buf.WriteVarInt(id);
            }
        }
    }
}
