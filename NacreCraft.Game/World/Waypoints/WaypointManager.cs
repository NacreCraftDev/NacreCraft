namespace NacreCraft.Game.World.Waypoints;

//WaypointManager tracks waypoint sources, maps to vanilla net.minecraft.world.waypoints.WaypointManager
public interface WaypointManager<T> where T : Waypoint
{
    void TrackWaypoint(T waypoint);

    void UpdateWaypoint(T waypoint);

    void UntrackWaypoint(T waypoint);
}

//TrackedWaypointManager the client-side tracked waypoint manager, maps to vanilla net.minecraft.world.waypoints.TrackedWaypointManager
public interface TrackedWaypointManager : WaypointManager<TrackedWaypoint>
{
}
