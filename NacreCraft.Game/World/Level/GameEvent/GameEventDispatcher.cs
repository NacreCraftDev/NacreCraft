using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.GameEvents;

//GameEventDispatcher hands a game event to every listener close enough to care, maps to vanilla GameEventDispatcher
//Listeners that ask for BY_DISTANCE delivery are collected first and served nearest-first, so a closer listener wins
public sealed class GameEventDispatcher(ServerLevel level)
{
    //Post dispatches one event from a world position, maps to vanilla post
    public void Post(GameEvent gameEvent, Vec3 position, GameEvent.Context context)
    {
        var radius = gameEvent.NotificationRadius;
        var centerX = (int)Math.Floor(position.X);
        var centerY = (int)Math.Floor(position.Y);
        var centerZ = (int)Math.Floor(position.Z);

        var minSectionX = SectionPos.BlockToSectionCoord(centerX - radius);
        var minSectionY = SectionPos.BlockToSectionCoord(centerY - radius);
        var minSectionZ = SectionPos.BlockToSectionCoord(centerZ - radius);
        var maxSectionX = SectionPos.BlockToSectionCoord(centerX + radius);
        var maxSectionY = SectionPos.BlockToSectionCoord(centerY + radius);
        var maxSectionZ = SectionPos.BlockToSectionCoord(centerZ + radius);

        List<ListenerInfo>? byDistance = null;
        ListenerVisitor visitor = (listener, listenerPosition) =>
        {
            if (listener.Mode == DeliveryMode.ByDistance)
            {
                byDistance ??= new List<ListenerInfo>();
                byDistance.Add(new ListenerInfo(gameEvent, position, context, listener, listenerPosition));
            }
            else
            {
                listener.HandleGameEvent(level, gameEvent, context, position);
            }
        };

        for (var chunkX = minSectionX; chunkX <= maxSectionX; chunkX++)
        {
            for (var chunkZ = minSectionZ; chunkZ <= maxSectionZ; chunkZ++)
            {
                //GetChunk returns null when the chunk is not loaded, which is the vanilla getChunkNow behaviour
                var chunk = level.GetChunk(new ChunkPos(chunkX, chunkZ));
                if (chunk is null) continue;
                for (var sectionY = minSectionY; sectionY <= maxSectionY; sectionY++)
                {
                    if (chunk.GetListenerRegistry(sectionY) is not GameEventListenerRegistry registry) continue;
                    registry.VisitInRangeListeners(gameEvent, position, context, visitor);
                }
            }
        }

        if (byDistance is null) return;
        byDistance.Sort();
        foreach (var info in byDistance)
            info.Recipient.HandleGameEvent(level, info.GameEvent, info.Context, info.Source);
    }
}

//ListenerInfo one deferred delivery together with how far the listener sits from the source
//maps to vanilla GameEvent.ListenerInfo; it lives here rather than next to GameEvent because it names a listener
//and GameEvent has to stay below the game layer
public sealed record ListenerInfo(
    GameEvent GameEvent,
    Vec3 Source,
    GameEvent.Context Context,
    GameEventListener Recipient,
    Vec3 RecipientPosition) : IComparable<ListenerInfo>
{
    //DistanceSquared how far the source is from the listener, the ordering key
    public double DistanceSquared => Source.DistanceToSqr(RecipientPosition);

    //CompareTo orders listeners by distance so the nearest one is served first
    public int CompareTo(ListenerInfo? other) => other is null ? 1 : DistanceSquared.CompareTo(other.DistanceSquared);
}
