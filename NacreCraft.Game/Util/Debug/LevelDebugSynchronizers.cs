using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.Debug;

//LevelDebugSynchronizers per-level debug value synchronization, maps to vanilla net.minecraft.util.debug.LevelDebugSynchronizers
//Vanilla lives on ServerLevel; NC keeps it in the Game layer because the synchronizers need the player list and entity tracker
public sealed class LevelDebugSynchronizers
{
    private readonly MinecraftServer _server;
    private readonly PersistentServerLevel _level;
    private readonly List<TrackingDebugSynchronizer> _allSynchronizers = new();
    private readonly Dictionary<DebugSubscription, SourceSynchronizer> _sourceSynchronizers = new();
    private readonly PoiSynchronizer _poiSynchronizer;
    private readonly VillageSectionSynchronizer _villageSectionSynchronizer;
    private bool _sleeping = true;
    private IReadOnlySet<DebugSubscription> _enabledSubscriptions = new HashSet<DebugSubscription>();

    public LevelDebugSynchronizers(MinecraftServer server, PersistentServerLevel level)
    {
        _server = server;
        _level = level;
        //Every subscription that carries a value gets its own source synchronizer, maps to the vanilla constructor loop
        foreach (var element in BuiltInRegistries.DEBUG_SUBSCRIPTION)
        {
            if (element is not DebugSubscription subscription || subscription.ValueCodec is null) continue;
            var synchronizer = new SourceSynchronizer(this, subscription);
            _sourceSynchronizers[subscription] = synchronizer;
            _allSynchronizers.Add(synchronizer);
        }
        _poiSynchronizer = new PoiSynchronizer(this);
        _villageSectionSynchronizer = new VillageSectionSynchronizer(this);
        _allSynchronizers.Add(_poiSynchronizer);
        _allSynchronizers.Add(_villageSectionSynchronizer);
    }

    internal MinecraftServer Server => _server;

    internal PersistentServerLevel Level => _level;

    //Players the players standing in this level, maps to vanilla ServerLevel.players
    public IReadOnlyList<ServerPlayer> Players
    {
        get
        {
            var all = _server.PlayerList.Players;
            var result = new List<ServerPlayer>(all.Count);
            foreach (var player in all)
                if (ReferenceEquals(player.Level, _level)) result.Add(player);
            return result;
        }
    }

    internal ServerPlayer? GetPlayerByUuid(Guid uuid)
    {
        foreach (var player in _server.PlayerList.Players)
            if (player.Uuid == uuid) return player;
        return null;
    }

    //Tick updates the enabled set and drives every synchronizer, maps to the vanilla tick
    public void Tick(ServerDebugSubscribers serverSubscribers)
    {
        _enabledSubscriptions = serverSubscribers.EnabledSubscriptions;
        var shouldSleep = _enabledSubscriptions.Count == 0;
        if (_sleeping != shouldSleep)
        {
            _sleeping = shouldSleep;
            if (shouldSleep)
            {
                foreach (var synchronizer in _allSynchronizers) synchronizer.Clear();
            }
            else
            {
                WakeUp();
            }
        }
        if (_sleeping) return;
        foreach (var synchronizer in _allSynchronizers) synchronizer.Tick();
    }

    //WakeUp registers the values of everything already loaded, maps to the vanilla wakeUp
    //NC has no debug value sources on chunks yet, so only entities that implement the interface are registered
    private void WakeUp()
    {
        foreach (var entity in _level.Entities)
        {
            if (!_server.EntityTracker.IsTrackedByAnyPlayer(entity)) continue;
            RegisterEntity(entity);
        }
    }

    public void RegisterEntity(ITrackedEntity entity)
    {
        if (_sleeping || entity is not DebugValueSource source) return;
        source.RegisterDebugValues(_level, new Registration(this, entity));
    }

    public void DropEntity(ITrackedEntity entity)
    {
        if (_sleeping) return;
        foreach (var synchronizer in _sourceSynchronizers.Values) synchronizer.DropEntity(entity.Uuid);
    }

    public void DropChunk(ChunkPos chunkPos)
    {
        if (_sleeping) return;
        foreach (var synchronizer in _sourceSynchronizers.Values) synchronizer.DropChunk(chunkPos);
    }

    public void DropBlockEntity(BlockPos blockPos)
    {
        if (_sleeping) return;
        foreach (var synchronizer in _sourceSynchronizers.Values) synchronizer.DropBlockEntity(blockPos);
    }

    public void StartTrackingChunk(ServerPlayer player, ChunkPos chunkPos)
    {
        if (_sleeping) return;
        foreach (var synchronizer in _allSynchronizers) synchronizer.StartTrackingChunk(player, chunkPos);
    }

    public void StartTrackingEntity(ServerPlayer player, ITrackedEntity entity)
    {
        if (_sleeping) return;
        foreach (var synchronizer in _allSynchronizers) synchronizer.StartTrackingEntity(player, entity);
    }

    public void RegisterPoi(PoiRecord poi)
    {
        if (_sleeping) return;
        _poiSynchronizer.OnPoiAdded(poi);
        _villageSectionSynchronizer.OnPoiAdded(poi);
    }

    public void UpdatePoi(BlockPos pos)
    {
        if (_sleeping) return;
        _poiSynchronizer.OnPoiTicketCountChanged(pos);
    }

    public void DropPoi(BlockPos pos)
    {
        if (_sleeping) return;
        _poiSynchronizer.OnPoiRemoved(pos);
        _villageSectionSynchronizer.OnPoiRemoved(pos);
    }

    public bool HasAnySubscriberFor(DebugSubscription subscription) => _enabledSubscriptions.Contains(subscription);

    public void SendBlockValue<T>(BlockPos blockPos, DebugSubscription<T> subscription, T value)
    {
        if (!HasAnySubscriberFor(subscription)) return;
        BroadcastToTracking(ChunkOf(blockPos), subscription, new ClientboundDebugBlockValuePacket(blockPos, subscription.PackUpdate(value)));
    }

    public void ClearBlockValue(BlockPos blockPos, DebugSubscription subscription)
    {
        if (!HasAnySubscriberFor(subscription)) return;
        BroadcastToTracking(ChunkOf(blockPos), subscription, new ClientboundDebugBlockValuePacket(blockPos, subscription.EmptyUpdate()));
    }

    public void SendEntityValue<T>(ITrackedEntity entity, DebugSubscription<T> subscription, T value)
    {
        if (!HasAnySubscriberFor(subscription)) return;
        BroadcastToTracking(entity, subscription, new ClientboundDebugEntityValuePacket(entity.EntityId, subscription.PackUpdate(value)));
    }

    public void ClearEntityValue(ITrackedEntity entity, DebugSubscription subscription)
    {
        if (!HasAnySubscriberFor(subscription)) return;
        BroadcastToTracking(entity, subscription, new ClientboundDebugEntityValuePacket(entity.EntityId, subscription.EmptyUpdate()));
    }

    public void BroadcastEventToTracking<T>(BlockPos blockPos, DebugSubscription<T> subscription, T value)
    {
        if (!HasAnySubscriberFor(subscription)) return;
        BroadcastToTracking(ChunkOf(blockPos), subscription, new ClientboundDebugEventPacket(subscription.PackEvent(value!)));
    }

    internal void SendToPlayersTrackingChunk(ChunkPos chunkPos, Packet<ClientGamePacketListener> packet)
    {
        var packed = ChunkPos.Pack(chunkPos.X, chunkPos.Z);
        foreach (var player in Players)
        {
            if (player.ChunkSender is null || !player.ChunkSender.IsTracked(packed)) continue;
            player.Connection.Send(packet);
        }
    }

    internal void SendToPlayersTrackingEntity(ITrackedEntity entity, Packet<ClientGamePacketListener> packet, Func<ServerPlayer, bool> filter)
        => _server.EntityTracker.SendToTrackingPlayersFiltered(entity, Players, packet, filter);

    private void BroadcastToTracking(ChunkPos chunkPos, DebugSubscription subscription, Packet<ClientGamePacketListener> packet)
    {
        var packed = ChunkPos.Pack(chunkPos.X, chunkPos.Z);
        foreach (var player in Players)
        {
            if (!player.DebugSubscriptions().Contains(subscription)) continue;
            if (player.ChunkSender is null || !player.ChunkSender.IsTracked(packed)) continue;
            player.Connection.Send(packet);
        }
    }

    private void BroadcastToTracking(ITrackedEntity entity, DebugSubscription subscription, Packet<ClientGamePacketListener> packet)
        => _server.EntityTracker.SendToTrackingPlayersFiltered(entity, Players, packet,
            player => player.DebugSubscriptions().Contains(subscription));

    private static ChunkPos ChunkOf(BlockPos pos) => new(pos.X >> 4, pos.Z >> 4);

    //Registration forwards a source's values to the matching source synchronizer
    private sealed class Registration : DebugValueSource.Registration
    {
        private readonly LevelDebugSynchronizers _owner;
        private readonly ITrackedEntity _entity;

        public Registration(LevelDebugSynchronizers owner, ITrackedEntity entity)
        {
            _owner = owner;
            _entity = entity;
        }

        public void Register<T>(DebugSubscription<T> subscription, DebugValueSource.ValueGetter<T> getter)
        {
            if (!_owner._sourceSynchronizers.TryGetValue(subscription, out var synchronizer)) return;
            synchronizer.RegisterEntity(_entity.Uuid, new DebugValueGetter<T>(getter));
        }
    }
}
