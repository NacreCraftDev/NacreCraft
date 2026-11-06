using System.Collections.Concurrent;
using NacreCraft.DataFixer.Util;
using NacreCraft.Game.World.Waypoints;
using NcEntity = NacreCraft.Registry.Entity;

namespace NacreCraft.Client.Waypoints;

//ClientWaypointManager tracks the waypoints the server sends, maps to vanilla net.minecraft.client.waypoints.ClientWaypointManager
//The key is the waypoint id: a UUID for entities or a string for other sources, the same Either the packet uses
public sealed class ClientWaypointManager : TrackedWaypointManager
{
    private readonly ConcurrentDictionary<Either<Guid, string>, TrackedWaypoint> _waypoints = new();

    public void TrackWaypoint(TrackedWaypoint waypoint)
        => _waypoints[waypoint.Id()] = waypoint;

    //UpdateWaypoint ignores an update whose track packet was missed instead of throwing on the missing key
    public void UpdateWaypoint(TrackedWaypoint waypoint)
    {
        if (_waypoints.TryGetValue(waypoint.Id(), out var existing))
            existing.Update(waypoint);
    }

    public void UntrackWaypoint(TrackedWaypoint waypoint)
        => _waypoints.TryRemove(waypoint.Id(), out _);

    public bool HasWaypoints() => !_waypoints.IsEmpty;

    //ForEachWaypoint visits waypoints farthest first, matching the vanilla render order
    public void ForEachWaypoint(NcEntity fromEntity, Action<TrackedWaypoint> consumer)
    {
        foreach (var waypoint in _waypoints.Values.OrderByDescending(w => w.DistanceSquared(fromEntity)))
            consumer(waypoint);
    }
}
