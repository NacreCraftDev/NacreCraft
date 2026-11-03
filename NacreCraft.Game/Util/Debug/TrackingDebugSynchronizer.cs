using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.Debug;

//IDebugValueGetter typed value getter erased to object, so the synchronizers can hold sources of different value types
public interface IDebugValueGetter
{
    object? Get();
}

//DebugValueGetter<T> wraps a typed getter; maps to the generic ValueGetter<T> of vanilla DebugValueSource
internal sealed class DebugValueGetter<T> : IDebugValueGetter
{
    private readonly DebugValueSource.ValueGetter<T> _getter;

    public DebugValueGetter(DebugValueSource.ValueGetter<T> getter) => _getter = getter;

    public object? Get() => _getter.Get();
}

//TrackingDebugSynchronizer base class, maps to vanilla net.minecraft.util.debug.TrackingDebugSynchronizer
//Follows which players enabled the subscription and pushes their initial values and later changes
public abstract class TrackingDebugSynchronizer
{
    protected readonly LevelDebugSynchronizers Owner;
    //SubscribedPlayers players that enabled this subscription, by uuid
    protected readonly HashSet<Guid> SubscribedPlayers = new();

    protected TrackingDebugSynchronizer(LevelDebugSynchronizers owner) => Owner = owner;

    //Subscription the subscription this synchronizer serves
    public abstract DebugSubscription Subscription { get; }

    //Tick follows subscription changes and polls values when anyone is subscribed, maps to the vanilla tick
    public void Tick()
    {
        foreach (var player in Owner.Players)
        {
            var wasSubscribed = SubscribedPlayers.Contains(player.Uuid);
            var isSubscribed = player.DebugSubscriptions().Contains(Subscription);
            if (isSubscribed == wasSubscribed) continue;
            if (isSubscribed)
            {
                AddSubscriber(player);
                continue;
            }
            SubscribedPlayers.Remove(player.Uuid);
        }
        SubscribedPlayers.RemoveWhere(uuid => Owner.GetPlayerByUuid(uuid) is null);
        if (SubscribedPlayers.Count > 0) PollAndSendUpdates();
    }

    private void AddSubscriber(ServerPlayer player)
    {
        SubscribedPlayers.Add(player.Uuid);
        //Chunks already inside the view get their initial value right away, except ones still queued for sending
        if (player.ChunkSender is { } sender)
        {
            foreach (var packed in sender.TrackedChunks.ToList())
            {
                if (sender.IsPending(packed)) continue;
                StartTrackingChunk(player, ChunkPos.Unpack(packed));
            }
        }
        Owner.Server.EntityTracker.ForEachEntityTrackedBy(Owner.Level, player, entity => StartTrackingEntity(player, entity));
    }

    public void StartTrackingChunk(ServerPlayer player, ChunkPos chunkPos)
    {
        if (SubscribedPlayers.Contains(player.Uuid)) SendInitialChunk(player, chunkPos);
    }

    public void StartTrackingEntity(ServerPlayer player, ITrackedEntity entity)
    {
        if (SubscribedPlayers.Contains(player.Uuid)) SendInitialEntity(player, entity);
    }

    internal virtual void Clear() { }

    protected virtual void PollAndSendUpdates() { }

    protected virtual void SendInitialChunk(ServerPlayer player, ChunkPos chunkPos) { }

    protected virtual void SendInitialEntity(ServerPlayer player, ITrackedEntity entity) { }

    protected void SendToPlayersTrackingChunk(ChunkPos chunkPos, Packet<ClientGamePacketListener> packet)
        => Owner.SendToPlayersTrackingChunk(chunkPos, packet);

    protected void SendToPlayersTrackingEntity(ITrackedEntity entity, Packet<ClientGamePacketListener> packet)
        => Owner.SendToPlayersTrackingEntity(entity, packet, _ => true);

    //ChunkOf the chunk a block belongs to
    protected static ChunkPos ChunkOf(BlockPos pos) => new(pos.X >> 4, pos.Z >> 4);
}

//ValueSource caches the last value a source reported, maps to the vanilla TrackingDebugSynchronizer.ValueSource
internal sealed class ValueSource
{
    private readonly IDebugValueGetter _getter;
    private object? _lastSyncedValue;

    public ValueSource(IDebugValueGetter getter) => _getter = getter;

    public object? LastSyncedValue => _lastSyncedValue;

    public Update? PollUpdate(DebugSubscription subscription)
    {
        var newValue = _getter.Get();
        if (Equals(newValue, _lastSyncedValue)) return null;
        _lastSyncedValue = newValue;
        return subscription.PackUpdate(newValue);
    }
}

//SourceSynchronizer drives value sources registered by chunks, block entities and entities
public sealed class SourceSynchronizer : TrackingDebugSynchronizer
{
    private readonly DebugSubscription _subscription;
    private readonly Dictionary<ChunkPos, ValueSource> _chunkSources = new();
    private readonly Dictionary<BlockPos, ValueSource> _blockEntitySources = new();
    private readonly Dictionary<Guid, ValueSource> _entitySources = new();

    public SourceSynchronizer(LevelDebugSynchronizers owner, DebugSubscription subscription) : base(owner)
        => _subscription = subscription;

    public override DebugSubscription Subscription => _subscription;

    public void RegisterChunk(ChunkPos chunkPos, IDebugValueGetter getter)
        => _chunkSources[chunkPos] = new ValueSource(getter);

    public void RegisterBlockEntity(BlockPos blockPos, IDebugValueGetter getter)
        => _blockEntitySources[blockPos] = new ValueSource(getter);

    public void RegisterEntity(Guid entityId, IDebugValueGetter getter)
        => _entitySources[entityId] = new ValueSource(getter);

    public void DropChunk(ChunkPos chunkPos)
    {
        _chunkSources.Remove(chunkPos);
        foreach (var pos in _blockEntitySources.Keys.Where(pos => ChunkOf(pos) == chunkPos).ToList())
            _blockEntitySources.Remove(pos);
    }

    public void DropBlockEntity(BlockPos blockPos)
    {
        if (_blockEntitySources.Remove(blockPos, out _))
            SendToPlayersTrackingChunk(ChunkOf(blockPos), new ClientboundDebugBlockValuePacket(blockPos, _subscription.EmptyUpdate()));
    }

    public void DropEntity(Guid entityId) => _entitySources.Remove(entityId);

    internal override void Clear()
    {
        _chunkSources.Clear();
        _blockEntitySources.Clear();
        _entitySources.Clear();
    }

    protected override void PollAndSendUpdates()
    {
        foreach (var (chunkPos, source) in _chunkSources)
        {
            var update = source.PollUpdate(_subscription);
            if (update is null) continue;
            SendToPlayersTrackingChunk(chunkPos, new ClientboundDebugChunkValuePacket(chunkPos, update));
        }
        foreach (var (blockPos, source) in _blockEntitySources)
        {
            var update = source.PollUpdate(_subscription);
            if (update is null) continue;
            SendToPlayersTrackingChunk(ChunkOf(blockPos), new ClientboundDebugBlockValuePacket(blockPos, update));
        }
        foreach (var (entityId, source) in _entitySources)
        {
            var update = source.PollUpdate(_subscription);
            if (update is null) continue;
            var entity = Owner.Level.EntityManager.GetByUuid(entityId);
            if (entity is null) continue;
            SendToPlayersTrackingEntity(entity, new ClientboundDebugEntityValuePacket(entity.EntityId, update));
        }
    }

    protected override void SendInitialChunk(ServerPlayer player, ChunkPos chunkPos)
    {
        if (_chunkSources.TryGetValue(chunkPos, out var chunkSource) && chunkSource.LastSyncedValue is not null)
            player.Connection.Send(new ClientboundDebugChunkValuePacket(chunkPos, _subscription.PackUpdate(chunkSource.LastSyncedValue)));
        foreach (var (blockPos, source) in _blockEntitySources)
        {
            if (source.LastSyncedValue is null || ChunkOf(blockPos) != chunkPos) continue;
            player.Connection.Send(new ClientboundDebugBlockValuePacket(blockPos, _subscription.PackUpdate(source.LastSyncedValue)));
        }
    }

    protected override void SendInitialEntity(ServerPlayer player, ITrackedEntity entity)
    {
        if (_entitySources.TryGetValue(entity.Uuid, out var source) && source.LastSyncedValue is not null)
            player.Connection.Send(new ClientboundDebugEntityValuePacket(entity.EntityId, _subscription.PackUpdate(source.LastSyncedValue)));
    }
}

//PoiSynchronizer pushes point of interest records, maps to vanilla TrackingDebugSynchronizer.PoiSynchronizer
public sealed class PoiSynchronizer : TrackingDebugSynchronizer
{
    public PoiSynchronizer(LevelDebugSynchronizers owner) : base(owner) { }

    public override DebugSubscription Subscription => DebugSubscriptions.Pois;

    public void OnPoiAdded(PoiRecord record)
        => SendToPlayersTrackingChunk(ChunkOf(record.Pos),
            new ClientboundDebugBlockValuePacket(record.Pos, Subscription.PackUpdate(new DebugPoiInfo(record))));

    public void OnPoiRemoved(BlockPos poiPos)
        => SendToPlayersTrackingChunk(ChunkOf(poiPos),
            new ClientboundDebugBlockValuePacket(poiPos, Subscription.EmptyUpdate()));

    public void OnPoiTicketCountChanged(BlockPos poiPos)
    {
        var type = Owner.Level.PoiManager.GetType(poiPos);
        var update = type is null
            ? Subscription.EmptyUpdate()
            : Subscription.PackUpdate(new DebugPoiInfo(poiPos, BuiltInRegistries.POINT_OF_INTEREST_TYPE.WrapAsHolder(type), 0));
        SendToPlayersTrackingChunk(ChunkOf(poiPos), new ClientboundDebugBlockValuePacket(poiPos, update));
    }

    protected override void SendInitialChunk(ServerPlayer player, ChunkPos chunkPos)
    {
        foreach (var record in Owner.Level.PoiManager.GetInChunk(chunkPos))
            player.Connection.Send(new ClientboundDebugBlockValuePacket(record.Pos, Subscription.PackUpdate(new DebugPoiInfo(record))));
    }
}

//VillageSectionSynchronizer pushes which sections belong to a village, maps to vanilla TrackingDebugSynchronizer.VillageSectionSynchronizer
public sealed class VillageSectionSynchronizer : TrackingDebugSynchronizer
{
    public VillageSectionSynchronizer(LevelDebugSynchronizers owner) : base(owner) { }

    public override DebugSubscription Subscription => DebugSubscriptions.VillageSections;

    public void OnPoiAdded(PoiRecord record) => SendVillageSectionsPacket(record.Pos);

    public void OnPoiRemoved(BlockPos poiPos) => SendVillageSectionsPacket(poiPos);

    protected override void SendInitialChunk(ServerPlayer player, ChunkPos chunkPos)
    {
        foreach (var record in Owner.Level.PoiManager.GetInChunk(chunkPos))
            ForEachVillageSectionUpdate(SectionOf(record.Pos), (sectionCenter, isVillage) =>
                player.Connection.Send(new ClientboundDebugBlockValuePacket(
                    sectionCenter, isVillage ? Subscription.PackUpdate(Unit.Instance) : Subscription.EmptyUpdate())));
    }

    private void SendVillageSectionsPacket(BlockPos poiPos)
    {
        ForEachVillageSectionUpdate(SectionOf(poiPos), (sectionCenter, isVillage) =>
            SendToPlayersTrackingChunk(ChunkOf(sectionCenter), new ClientboundDebugBlockValuePacket(
                sectionCenter, isVillage ? Subscription.PackUpdate(Unit.Instance) : Subscription.EmptyUpdate())));
    }

    //ForEachVillageSectionUpdate tests the 3x3x3 sections around the centre, maps to the vanilla loop
    private void ForEachVillageSectionUpdate((int X, int Y, int Z) center, Action<BlockPos, bool> consumer)
    {
        for (var offsetZ = -1; offsetZ <= 1; offsetZ++)
        for (var offsetX = -1; offsetX <= 1; offsetX++)
        for (var offsetY = -1; offsetY <= 1; offsetY++)
        {
            var section = (center.X + offsetX, center.Y + offsetY, center.Z + offsetZ);
            var sectionCenter = SectionCenter(section);
            consumer(sectionCenter, Owner.Level.IsVillage(sectionCenter));
        }
    }

    private static (int X, int Y, int Z) SectionOf(BlockPos pos) => (pos.X >> 4, pos.Y >> 4, pos.Z >> 4);

    private static BlockPos SectionCenter((int X, int Y, int Z) section)
        => new((section.X << 4) + 8, (section.Y << 4) + 8, (section.Z << 4) + 8);
}
