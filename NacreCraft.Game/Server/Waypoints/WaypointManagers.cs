using System.Runtime.CompilerServices;
using NacreCraft.Game.World.Waypoints;
using NacreCraft.Storage;

namespace NacreCraft.Game.Server.Waypoints;

//WaypointManagers owns one ServerWaypointManager per level, the counterweight to vanilla ServerLevel.getWaypointManager
//Storage cannot reference Game, so the manager lives here and the level's entity events feed it; the table is weak so a
//discarded test level does not keep its manager alive
public static class WaypointManagers
{
    private static readonly ConditionalWeakTable<PersistentServerLevel, ServerWaypointManager> Managers = new();

    public static ServerWaypointManager Get(PersistentServerLevel level)
        => Managers.GetValue(level, Create);

    private static ServerWaypointManager Create(PersistentServerLevel level)
    {
        var manager = new ServerWaypointManager();
        var entityManager = level.EntityManager;
        //Storage raises plain entity events; the transmitter cast and the locator bar rule stay on the game side
        entityManager.EntityTrackingStarted += entity =>
        {
            if (entity is WaypointTransmitter waypoint && waypoint.IsTransmittingWaypoint())
                manager.TrackWaypoint(waypoint);
        };
        entityManager.EntityTrackingStopped += entity =>
        {
            if (entity is WaypointTransmitter waypoint) manager.UntrackWaypoint(waypoint);
        };
        //The manager may be requested after entities are already tracked, so seed from the current visible set
        foreach (var entity in entityManager.Visible)
            if (entity is WaypointTransmitter waypoint && waypoint.IsTransmittingWaypoint())
                manager.TrackWaypoint(waypoint);
        return manager;
    }
}
