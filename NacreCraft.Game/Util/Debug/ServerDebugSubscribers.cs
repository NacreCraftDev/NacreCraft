using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Network;

namespace NacreCraft.Game.Util.Debug;

//ServerDebugSubscribers server-side debug subscription bookkeeping, maps to vanilla net.minecraft.util.debug.ServerDebugSubscribers
//Rebuilt every tick from the subscriptions each player currently has enabled
public sealed class ServerDebugSubscribers
{
    private readonly MinecraftServer _server;
    private readonly Dictionary<DebugSubscription, List<ServerPlayer>> _enabledSubscriptions = new();

    public ServerDebugSubscribers(MinecraftServer server) => _server = server;

    private IReadOnlyList<ServerPlayer> GetSubscribersFor(DebugSubscription subscription)
        => _enabledSubscriptions.TryGetValue(subscription, out var list) ? list : Array.Empty<ServerPlayer>();

    //Tick rebuilds the player lists from the live subscriptions, maps to the vanilla tick
    public void Tick()
    {
        foreach (var list in _enabledSubscriptions.Values) list.Clear();
        foreach (var player in _server.PlayerList.Players)
        {
            foreach (var subscription in player.DebugSubscriptions())
            {
                if (!_enabledSubscriptions.TryGetValue(subscription, out var list))
                    _enabledSubscriptions[subscription] = list = new List<ServerPlayer>();
                list.Add(player);
            }
        }
        foreach (var subscription in _enabledSubscriptions.Where(e => e.Value.Count == 0).Select(e => e.Key).ToList())
            _enabledSubscriptions.Remove(subscription);
    }

    public void BroadcastToAll(DebugSubscription subscription, Packet<ClientGamePacketListener> packet)
    {
        foreach (var player in GetSubscribersFor(subscription)) player.Connection.Send(packet);
    }

    public IReadOnlySet<DebugSubscription> EnabledSubscriptions => _enabledSubscriptions.Keys.ToHashSet();

    public bool HasAnySubscriberFor(DebugSubscription subscription) => GetSubscribersFor(subscription).Count > 0;

    //HasRequiredPermissions whether the player may receive debug data
    //Vanilla also allows the singleplayer owner while running in an IDE; a dedicated server has no owner, so only ops qualify
    public bool HasRequiredPermissions(ServerPlayer player) => _server.OpList.IsOp(player.Profile);
}
