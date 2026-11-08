using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.JsonRpc.Methods;
using NacreCraft.Game.World.Level;
using NacreCraft.Registry;
using RegistryMethod = NacreCraft.Registry.IncomingRpcMethod<object, object>;

namespace NacreCraft.Game.Server.JsonRpc;

//IncomingRpcMethods the client-to-server method table, maps to vanilla net.minecraft.server.jsonrpc.IncomingRpcMethods
//Every service family is registered here; async service functions are bridged with GetAwaiter so the RPC handler stays synchronous
public static class IncomingRpcMethods
{
    //Bootstrap registers every method and returns the discovery method, maps to vanilla bootstrap
    public static IncomingRpcMethod<object, DiscoveryService.DiscoverResponse> Bootstrap(Registry<RegistryMethod> methodRegistry)
    {
        RegisterAllowlist(methodRegistry);
        RegisterBanlist(methodRegistry);
        RegisterIpBanlist(methodRegistry);
        RegisterPlayer(methodRegistry);
        RegisterOperator(methodRegistry);
        RegisterServerState(methodRegistry);
        RegisterServerSettings(methodRegistry);
        RegisterGameRules(methodRegistry);
        return new IncomingRpcMethodBuilder<object, DiscoveryService.DiscoverResponse>(
                (api, clientInfo) => DiscoveryService.Discover(SchemaComponents.All()))
            .Description("Discover available RPC methods")
            .Undiscoverable().AllowPreServerInit().NotOnMainThread()
            .Response("result", Schema<DiscoveryService.DiscoverResponse>.Record(
                Codecs.AsCodec(DiscoveryService.DiscoverResponse.CODEC)))
            .Register(methodRegistry, "rpc.discover");
    }

    private static void RegisterAllowlist(Registry<RegistryMethod> registry)
    {
        Register(registry, "allowlist", "Get the allowlist",
            (api, _) => AllowlistService.Get(api), SchemaPresets.PLAYER_SCHEMA.AsArray());
        Register(registry, "allowlist/set", "Set the allowlist",
            (api, players, clientInfo) => AllowlistService.Set(api, players.ToList(), clientInfo).GetAwaiter().GetResult(),
            "players", SchemaPresets.PLAYER_SCHEMA.AsArray(), SchemaPresets.PLAYER_SCHEMA.AsArray());
        Register(registry, "allowlist/add", "Add players to allowlist",
            (api, players, clientInfo) => AllowlistService.Add(api, players.ToList(), clientInfo).GetAwaiter().GetResult(),
            "add", SchemaPresets.PLAYER_SCHEMA.AsArray(), SchemaPresets.PLAYER_SCHEMA.AsArray());
        Register(registry, "allowlist/remove", "Remove players from allowlist",
            (api, players, clientInfo) => AllowlistService.Remove(api, players.ToList(), clientInfo).GetAwaiter().GetResult(),
            "remove", SchemaPresets.PLAYER_SCHEMA.AsArray(), SchemaPresets.PLAYER_SCHEMA.AsArray());
        Register(registry, "allowlist/clear", "Clear all players in allowlist",
            (api, clientInfo) => AllowlistService.Clear(api, clientInfo), SchemaPresets.PLAYER_SCHEMA.AsArray());
    }

    private static void RegisterBanlist(Registry<RegistryMethod> registry)
    {
        Register(registry, "bans", "Get the ban list",
            (api, _) => BanlistService.Get(api), SchemaPresets.PLAYER_BAN_SCHEMA.AsArray());
        Register(registry, "bans/set", "Set the banlist",
            (api, bans, clientInfo) => BanlistService.Set(api, bans.ToList(), clientInfo).GetAwaiter().GetResult(),
            "bans", SchemaPresets.PLAYER_BAN_SCHEMA.AsArray(), SchemaPresets.PLAYER_BAN_SCHEMA.AsArray());
        Register(registry, "bans/add", "Add players to ban list",
            (api, bans, clientInfo) => BanlistService.Add(api, bans.ToList(), clientInfo).GetAwaiter().GetResult(),
            "add", SchemaPresets.PLAYER_BAN_SCHEMA.AsArray(), SchemaPresets.PLAYER_BAN_SCHEMA.AsArray());
        Register(registry, "bans/remove", "Remove players from ban list",
            (api, players, clientInfo) => BanlistService.Remove(api, players.ToList(), clientInfo).GetAwaiter().GetResult(),
            "remove", SchemaPresets.PLAYER_SCHEMA.AsArray(), SchemaPresets.PLAYER_BAN_SCHEMA.AsArray());
        Register(registry, "bans/clear", "Clear all players in ban list",
            (api, clientInfo) => BanlistService.Clear(api, clientInfo), SchemaPresets.PLAYER_BAN_SCHEMA.AsArray());
    }

    private static void RegisterIpBanlist(Registry<RegistryMethod> registry)
    {
        Register(registry, "ip_bans", "Get the ip ban list",
            (api, _) => IpBanlistService.Get(api), SchemaPresets.IP_BAN_SCHEMA.AsArray());
        Register(registry, "ip_bans/set", "Set the ip banlist",
            (api, bans, clientInfo) => IpBanlistService.Set(api, bans.ToList(), clientInfo),
            "banlist", SchemaPresets.IP_BAN_SCHEMA.AsArray(), SchemaPresets.IP_BAN_SCHEMA.AsArray());
        Register(registry, "ip_bans/add", "Add ip to ban list",
            (api, bans, clientInfo) => IpBanlistService.Add(api, bans.ToList(), clientInfo),
            "add", SchemaPresets.INCOMING_IP_BAN_SCHEMA.AsArray(), SchemaPresets.IP_BAN_SCHEMA.AsArray());
        Register(registry, "ip_bans/remove", "Remove ip from ban list",
            (api, ips, clientInfo) => IpBanlistService.Remove(api, ips.ToList(), clientInfo),
            "ip", SchemaPresets.STRING_SCHEMA.AsArray(), SchemaPresets.IP_BAN_SCHEMA.AsArray());
        Register(registry, "ip_bans/clear", "Clear all ips in ban list",
            (api, clientInfo) => IpBanlistService.Clear(api, clientInfo), SchemaPresets.IP_BAN_SCHEMA.AsArray());
    }

    private static void RegisterPlayer(Registry<RegistryMethod> registry)
    {
        Register(registry, "players", "Get all connected players",
            (api, _) => PlayerService.Get(api), SchemaPresets.PLAYER_SCHEMA.AsArray());
        Register(registry, "players/kick", "Kick players",
            (api, kicks, clientInfo) => PlayerService.Kick(api, kicks.ToList(), clientInfo),
            "kick", SchemaPresets.KICK_PLAYER_SCHEMA.AsArray(), SchemaPresets.PLAYER_SCHEMA.AsArray());
    }

    private static void RegisterOperator(Registry<RegistryMethod> registry)
    {
        Register(registry, "operators", "Get all oped players",
            (api, _) => OperatorService.Get(api), SchemaPresets.OPERATOR_SCHEMA.AsArray());
        Register(registry, "operators/set", "Set all oped players",
            (api, ops, clientInfo) => OperatorService.Set(api, ops.ToList(), clientInfo).GetAwaiter().GetResult(),
            "operators", SchemaPresets.OPERATOR_SCHEMA.AsArray(), SchemaPresets.OPERATOR_SCHEMA.AsArray());
        Register(registry, "operators/add", "Op players",
            (api, ops, clientInfo) => OperatorService.Add(api, ops.ToList(), clientInfo).GetAwaiter().GetResult(),
            "add", SchemaPresets.OPERATOR_SCHEMA.AsArray(), SchemaPresets.OPERATOR_SCHEMA.AsArray());
        Register(registry, "operators/remove", "Deop players",
            (api, players, clientInfo) => OperatorService.Remove(api, players.ToList(), clientInfo).GetAwaiter().GetResult(),
            "remove", SchemaPresets.PLAYER_SCHEMA.AsArray(), SchemaPresets.OPERATOR_SCHEMA.AsArray());
        Register(registry, "operators/clear", "Deop all players",
            (api, clientInfo) => OperatorService.Clear(api, clientInfo), SchemaPresets.OPERATOR_SCHEMA.AsArray());
    }

    private static void RegisterServerState(Registry<RegistryMethod> registry)
    {
        Register(registry, "server/status", "Get server status",
            (api, _) => ServerStateService.Status(api), SchemaPresets.SERVER_STATE_SCHEMA.AsRef());
        Register(registry, "server/save", "Save server state",
            (api, flush, clientInfo) => ServerStateService.Save(api, flush, clientInfo),
            "flush", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "server/stop", "Stop server",
            (api, clientInfo) => ServerStateService.Stop(api, clientInfo), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "server/system_message", "Send a system message",
            (api, message, clientInfo) => ServerStateService.SendSystemMessage(api, message, clientInfo),
            "message", SchemaPresets.SYSTEM_MESSAGE_SCHEMA.AsRef(), SchemaPresets.BOOL_SCHEMA);
    }

    private static void RegisterServerSettings(Registry<RegistryMethod> registry)
    {
        Register(registry, "serversettings/autosave", "Get whether automatic world saving is enabled",
            (api, _) => ServerSettingsService.Autosave(api), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/autosave/set", "Enable or disable automatic world saving",
            (api, enable, clientInfo) => ServerSettingsService.SetAutosave(api, enable, clientInfo),
            "enable", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/difficulty", "Get the current difficulty level",
            (api, _) => ServerSettingsService.Difficulty(api), SchemaPresets.DIFFICULTY_SCHEMA.AsRef());
        Register(registry, "serversettings/difficulty/set", "Set the difficulty level",
            (api, difficulty, clientInfo) => ServerSettingsService.SetDifficulty(api, difficulty, clientInfo),
            "difficulty", SchemaPresets.DIFFICULTY_SCHEMA.AsRef(), SchemaPresets.DIFFICULTY_SCHEMA.AsRef());
        Register(registry, "serversettings/enforce_allowlist", "Get whether allowlist enforcement is enabled",
            (api, _) => ServerSettingsService.EnforceAllowlist(api), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/enforce_allowlist/set", "Enable or disable allowlist enforcement",
            (api, enforce, clientInfo) => ServerSettingsService.SetEnforceAllowlist(api, enforce, clientInfo),
            "enforce", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/use_allowlist", "Get whether the allowlist is enabled",
            (api, _) => ServerSettingsService.UsingAllowlist(api), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/use_allowlist/set", "Enable or disable the allowlist",
            (api, use, clientInfo) => ServerSettingsService.SetUsingAllowlist(api, use, clientInfo),
            "use", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/max_players", "Get the maximum number of players",
            (api, _) => ServerSettingsService.MaxPlayers(api), SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/max_players/set", "Set the maximum number of players",
            (api, max, clientInfo) => ServerSettingsService.SetMaxPlayers(api, max, clientInfo),
            "max", SchemaPresets.INT_SCHEMA, SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/pause_when_empty_seconds", "Get the pause when empty delay",
            (api, _) => ServerSettingsService.PauseWhenEmpty(api), SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/pause_when_empty_seconds/set", "Set the pause when empty delay",
            (api, seconds, clientInfo) => ServerSettingsService.SetPauseWhenEmpty(api, seconds, clientInfo),
            "seconds", SchemaPresets.INT_SCHEMA, SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/player_idle_timeout", "Get the player idle timeout",
            (api, _) => ServerSettingsService.PlayerIdleTimeout(api), SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/player_idle_timeout/set", "Set the player idle timeout",
            (api, seconds, clientInfo) => ServerSettingsService.SetPlayerIdleTimeout(api, seconds, clientInfo),
            "seconds", SchemaPresets.INT_SCHEMA, SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/allow_flight", "Get whether flight is allowed",
            (api, _) => ServerSettingsService.AllowFlight(api), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/allow_flight/set", "Allow or disallow flight",
            (api, allow, clientInfo) => ServerSettingsService.SetAllowFlight(api, allow, clientInfo),
            "allow", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/motd", "Get the server MOTD",
            (api, _) => ServerSettingsService.Motd(api), SchemaPresets.STRING_SCHEMA);
        Register(registry, "serversettings/motd/set", "Set the server MOTD",
            (api, message, clientInfo) => ServerSettingsService.SetMotd(api, message, clientInfo),
            "message", SchemaPresets.STRING_SCHEMA, SchemaPresets.STRING_SCHEMA);
        Register(registry, "serversettings/spawn_protection_radius", "Get the spawn protection radius",
            (api, _) => ServerSettingsService.SpawnProtection(api), SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/spawn_protection_radius/set", "Set the spawn protection radius",
            (api, radius, clientInfo) => ServerSettingsService.SetSpawnProtection(api, radius, clientInfo),
            "radius", SchemaPresets.INT_SCHEMA, SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/force_game_mode", "Get whether the game mode is forced",
            (api, _) => ServerSettingsService.ForceGameMode(api), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/force_game_mode/set", "Enable or disable forcing the game mode",
            (api, force, clientInfo) => ServerSettingsService.SetForceGameMode(api, force, clientInfo),
            "force", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/game_mode", "Get the default game mode",
            (api, _) => ServerSettingsService.GameMode(api), SchemaPresets.GAME_TYPE_SCHEMA.AsRef());
        Register(registry, "serversettings/game_mode/set", "Set the default game mode",
            (api, mode, clientInfo) => ServerSettingsService.SetGameMode(api, mode, clientInfo),
            "mode", SchemaPresets.GAME_TYPE_SCHEMA.AsRef(), SchemaPresets.GAME_TYPE_SCHEMA.AsRef());
        Register(registry, "serversettings/view_distance", "Get the view distance",
            (api, _) => ServerSettingsService.ViewDistance(api), SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/view_distance/set", "Set the view distance",
            (api, distance, clientInfo) => ServerSettingsService.SetViewDistance(api, distance, clientInfo),
            "distance", SchemaPresets.INT_SCHEMA, SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/simulation_distance", "Get the simulation distance",
            (api, _) => ServerSettingsService.SimulationDistance(api), SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/simulation_distance/set", "Set the simulation distance",
            (api, distance, clientInfo) => ServerSettingsService.SetSimulationDistance(api, distance, clientInfo),
            "distance", SchemaPresets.INT_SCHEMA, SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/accept_transfers", "Get whether transfers are accepted",
            (api, _) => ServerSettingsService.AcceptTransfers(api), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/accept_transfers/set", "Enable or disable accepting transfers",
            (api, accept, clientInfo) => ServerSettingsService.SetAcceptTransfers(api, accept, clientInfo),
            "accept", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/status_heartbeat_interval", "Get the status heartbeat interval",
            (api, _) => ServerSettingsService.StatusHeartbeatInterval(api), SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/status_heartbeat_interval/set", "Set the status heartbeat interval",
            (api, seconds, clientInfo) => ServerSettingsService.SetStatusHeartbeatInterval(api, seconds, clientInfo),
            "seconds", SchemaPresets.INT_SCHEMA, SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/operator_user_permission_level", "Get the default operator permission level",
            (api, _) => ServerSettingsService.OperatorUserPermissionLevel(api), SchemaPresets.PERMISSION_LEVEL_SCHEMA);
        Register(registry, "serversettings/operator_user_permission_level/set", "Set the default operator permission level",
            (api, level, clientInfo) => ServerSettingsService.SetOperatorUserPermissionLevel(api, level, clientInfo),
            "level", SchemaPresets.PERMISSION_LEVEL_SCHEMA, SchemaPresets.PERMISSION_LEVEL_SCHEMA);
        Register(registry, "serversettings/hide_online_players", "Get whether online players are hidden",
            (api, _) => ServerSettingsService.HidesOnlinePlayers(api), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/hide_online_players/set", "Enable or disable hiding online players",
            (api, hide, clientInfo) => ServerSettingsService.SetHidesOnlinePlayers(api, hide, clientInfo),
            "hide", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/status_replies", "Get whether the server replies to status requests",
            (api, _) => ServerSettingsService.RepliesToStatus(api), SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/status_replies/set", "Enable or disable status replies",
            (api, enable, clientInfo) => ServerSettingsService.SetRepliesToStatus(api, enable, clientInfo),
            "enable", SchemaPresets.BOOL_SCHEMA, SchemaPresets.BOOL_SCHEMA);
        Register(registry, "serversettings/entity_broadcast_range", "Get the entity broadcast range",
            (api, _) => ServerSettingsService.EntityBroadcastRangePercentage(api), SchemaPresets.INT_SCHEMA);
        Register(registry, "serversettings/entity_broadcast_range/set", "Set the entity broadcast range",
            (api, percentage, clientInfo) => ServerSettingsService.SetEntityBroadcastRangePercentage(api, percentage, clientInfo),
            "percentage_points", SchemaPresets.INT_SCHEMA, SchemaPresets.INT_SCHEMA);
    }

    private static void RegisterGameRules(Registry<RegistryMethod> registry)
    {
        Register(registry, "gamerules", "Get the available game rule keys and their current values",
            (api, _) => GameRulesService.Get(api), SchemaPresets.TYPED_GAME_RULE_SCHEMA.AsArray());
        Register(registry, "gamerules/update", "Update game rule value",
            (api, update, clientInfo) => GameRulesService.Update(api, update, clientInfo),
            "gamerule", SchemaPresets.UNTYPED_GAME_RULE_SCHEMA.AsRef(), SchemaPresets.TYPED_GAME_RULE_SCHEMA.AsRef());
    }

    private static void Register<Result>(Registry<RegistryMethod> registry, string key, string description,
        IncomingRpcMethod<object, Result>.ParameterlessRpcMethodFunction function, Schema<Result> response)
        => new IncomingRpcMethodBuilder<object, Result>(function)
            .Description(description).Response("result", response).Register(registry, key);

    private static void Register<Params, Result>(Registry<RegistryMethod> registry, string key, string description,
        IncomingRpcMethod<Params, Result>.RpcMethodFunction function, string paramName, Schema<Params> parameters,
        Schema<Result> response)
        => new IncomingRpcMethodBuilder<Params, Result>(function)
            .Description(description).Param(paramName, parameters).Response("result", response)
            .Register(registry, key);
}
