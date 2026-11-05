using NacreCraft.Game.Server;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Waypoints;

namespace NacreCraft.Game.Server.Waypoints;

//ServerWaypointManager tracks every transmitting entity against every receiving player, maps to vanilla net.minecraft.server.waypoints.ServerWaypointManager
//Connections live in a per-player map instead of a Guava table; both maps compare by reference because players and entities have no value equality
public sealed class ServerWaypointManager : WaypointManager<WaypointTransmitter>
{
    private readonly HashSet<WaypointTransmitter> _waypoints = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<ServerPlayer> _players = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ServerPlayer, Dictionary<WaypointTransmitter, WaypointTransmitter.Connection>> _connections =
        new(ReferenceEqualityComparer.Instance);

    public void TrackWaypoint(WaypointTransmitter waypoint)
    {
        _waypoints.Add(waypoint);
        foreach (var player in _players) CreateConnection(player, waypoint);
    }

    public void UpdateWaypoint(WaypointTransmitter waypoint)
    {
        if (!_waypoints.Contains(waypoint)) return;
        //Snapshot the affected entries because updating a connection can replace its entry
        var connections = new List<(ServerPlayer Player, WaypointTransmitter.Connection Connection)>();
        foreach (var (player, map) in _connections)
            if (map.TryGetValue(waypoint, out var connection)) connections.Add((player, connection));
        foreach (var (player, connection) in connections) UpdateConnection(player, waypoint, connection);
        var potentialPlayers = new List<ServerPlayer>();
        foreach (var player in _players)
            if (!_connections.TryGetValue(player, out var map) || !map.ContainsKey(waypoint)) potentialPlayers.Add(player);
        foreach (var player in potentialPlayers) CreateConnection(player, waypoint);
    }

    public void UntrackWaypoint(WaypointTransmitter waypoint)
    {
        foreach (var map in _connections.Values)
        {
            if (!map.Remove(waypoint, out var connection)) continue;
            connection.Disconnect();
        }
        _waypoints.Remove(waypoint);
    }

    //AddPlayer starts tracking a receiving player, maps to vanilla addPlayer
    public void AddPlayer(ServerPlayer player)
    {
        _players.Add(player);
        foreach (var waypoint in _waypoints) CreateConnection(player, waypoint);
        if (player.IsTransmittingWaypoint()) TrackWaypoint(player);
    }

    //UpdatePlayer refreshes a moved player's connections, maps to vanilla updatePlayer
    public void UpdatePlayer(ServerPlayer player)
    {
        var connections = new List<(WaypointTransmitter Waypoint, WaypointTransmitter.Connection Connection)>();
        if (_connections.TryGetValue(player, out var map))
            foreach (var (waypoint, connection) in map) connections.Add((waypoint, connection));
        foreach (var (waypoint, connection) in connections) UpdateConnection(player, waypoint, connection);
        var potentialWaypoints = new List<WaypointTransmitter>();
        foreach (var waypoint in _waypoints)
            if (!_connections.TryGetValue(player, out var current) || !current.ContainsKey(waypoint))
                potentialWaypoints.Add(waypoint);
        foreach (var waypoint in potentialWaypoints) CreateConnection(player, waypoint);
    }

    //RemovePlayer drops every connection of a leaving player, maps to vanilla removePlayer
    public void RemovePlayer(ServerPlayer player)
    {
        if (_connections.TryGetValue(player, out var map))
        {
            foreach (var connection in map.Values) connection.Disconnect();
            map.Clear();
        }
        UntrackWaypoint(player);
        _players.Remove(player);
    }

    //BreakAllConnections disconnects everyone without forgetting the tracked sets, maps to vanilla breakAllConnections
    public void BreakAllConnections()
    {
        foreach (var map in _connections.Values)
            foreach (var connection in map.Values)
                connection.Disconnect();
        _connections.Clear();
    }

    //RemakeConnections rebuilds the connections of one source, maps to vanilla remakeConnections
    public void RemakeConnections(WaypointTransmitter waypoint)
    {
        foreach (var player in _players) CreateConnection(player, waypoint);
    }

    //Transmitters the tracked sources, maps to vanilla transmitters
    public IReadOnlyCollection<WaypointTransmitter> Transmitters() => _waypoints;

    //IsLocatorBarEnabledFor checks the level's locator bar rule, maps to vanilla isLocatorBarEnabledFor
    private static bool IsLocatorBarEnabledFor(ServerPlayer player)
        => player.Level.GameRules?.GetBool(GameRules.LocatorBar) ?? true;

    private void CreateConnection(ServerPlayer player, WaypointTransmitter waypoint)
    {
        if (ReferenceEquals(player, waypoint)) return;
        if (!IsLocatorBarEnabledFor(player)) return;
        var connection = waypoint.MakeWaypointConnectionWith(player);
        if (connection.IsPresent)
        {
            if (!_connections.TryGetValue(player, out var map))
            {
                map = new Dictionary<WaypointTransmitter, WaypointTransmitter.Connection>(ReferenceEqualityComparer.Instance);
                _connections[player] = map;
            }
            map[waypoint] = connection.Get();
            connection.Get().Connect();
            return;
        }
        //An empty answer also means an old connection has to go
        if (_connections.TryGetValue(player, out var existing) && existing.Remove(waypoint, out var stale))
            stale.Disconnect();
    }

    private void UpdateConnection(ServerPlayer player, WaypointTransmitter waypoint, WaypointTransmitter.Connection connection)
    {
        if (ReferenceEquals(player, waypoint)) return;
        if (!IsLocatorBarEnabledFor(player)) return;
        if (!connection.IsBroken())
        {
            connection.Update();
            return;
        }
        var newConnection = waypoint.MakeWaypointConnectionWith(player);
        if (newConnection.IsPresent)
        {
            newConnection.Get().Connect();
            if (!_connections.TryGetValue(player, out var map))
            {
                map = new Dictionary<WaypointTransmitter, WaypointTransmitter.Connection>(ReferenceEqualityComparer.Instance);
                _connections[player] = map;
            }
            map[waypoint] = newConnection.Get();
            return;
        }
        connection.Disconnect();
        if (_connections.TryGetValue(player, out var connections)) connections.Remove(waypoint);
    }
}
