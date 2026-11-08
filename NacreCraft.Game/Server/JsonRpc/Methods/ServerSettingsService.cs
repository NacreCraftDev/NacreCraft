using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.World.Level;
using NacreCraft.Network.Protocol.Status;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc.Methods;

//ServerSettingsService settings RPC methods, maps to vanilla net.minecraft.server.jsonrpc.methods.ServerSettingsService
//Every method is a thin pass-through to the internal settings service
public static class ServerSettingsService
{
    public static bool Autosave(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.IsAutoSave();
    public static bool SetAutosave(MinecraftApi minecraftApi, bool enabled, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetAutoSave(enabled, clientInfo);

    public static Difficulty Difficulty(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.GetDifficulty();
    public static Difficulty SetDifficulty(MinecraftApi minecraftApi, Difficulty difficulty, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetDifficulty(difficulty, clientInfo);

    public static bool EnforceAllowlist(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.IsEnforceWhitelist();
    public static bool SetEnforceAllowlist(MinecraftApi minecraftApi, bool enforce, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetEnforceWhitelist(enforce, clientInfo);

    public static bool UsingAllowlist(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.IsUsingWhitelist();
    public static bool SetUsingAllowlist(MinecraftApi minecraftApi, bool use, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetUsingWhitelist(use, clientInfo);

    public static int MaxPlayers(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.GetMaxPlayers();
    public static int SetMaxPlayers(MinecraftApi minecraftApi, int maxPlayers, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetMaxPlayers(maxPlayers, clientInfo);

    public static int PauseWhenEmpty(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.GetPauseWhenEmptySeconds();
    public static int SetPauseWhenEmpty(MinecraftApi minecraftApi, int emptySeconds, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetPauseWhenEmptySeconds(emptySeconds, clientInfo);

    public static int PlayerIdleTimeout(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.GetPlayerIdleTimeout();
    public static int SetPlayerIdleTimeout(MinecraftApi minecraftApi, int idleTime, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetPlayerIdleTimeout(idleTime, clientInfo);

    public static bool AllowFlight(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.AllowFlight();
    public static bool SetAllowFlight(MinecraftApi minecraftApi, bool allow, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetAllowFlight(allow, clientInfo);

    public static int SpawnProtection(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.GetSpawnProtectionRadius();
    public static int SetSpawnProtection(MinecraftApi minecraftApi, int spawnProtection, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetSpawnProtectionRadius(spawnProtection, clientInfo);

    public static string Motd(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.GetMotd();
    public static string SetMotd(MinecraftApi minecraftApi, string motd, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetMotd(motd, clientInfo);

    public static bool ForceGameMode(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.ForceGameMode();
    public static bool SetForceGameMode(MinecraftApi minecraftApi, bool force, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetForceGameMode(force, clientInfo);

    public static GameType GameMode(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.GetGameMode();
    public static GameType SetGameMode(MinecraftApi minecraftApi, GameType gameMode, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetGameMode(gameMode, clientInfo);

    public static int ViewDistance(MinecraftApi minecraftApi) => minecraftApi.ServerSettingsService.GetViewDistance();
    public static int SetViewDistance(MinecraftApi minecraftApi, int viewDistance, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetViewDistance(viewDistance, clientInfo);

    public static int SimulationDistance(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.GetSimulationDistance();
    public static int SetSimulationDistance(MinecraftApi minecraftApi, int simulationDistance, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetSimulationDistance(simulationDistance, clientInfo);

    public static bool AcceptTransfers(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.AcceptsTransfers();
    public static bool SetAcceptTransfers(MinecraftApi minecraftApi, bool accept, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetAcceptsTransfers(accept, clientInfo);

    public static int StatusHeartbeatInterval(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.GetStatusHeartbeatInterval();
    public static int SetStatusHeartbeatInterval(MinecraftApi minecraftApi, int statusHeartbeatInterval,
        ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetStatusHeartbeatInterval(statusHeartbeatInterval, clientInfo);

    public static PermissionLevel OperatorUserPermissionLevel(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.GetOperatorUserPermissions().Level;
    public static PermissionLevel SetOperatorUserPermissionLevel(MinecraftApi minecraftApi, PermissionLevel level,
        ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetOperatorUserPermissions(
            LevelBasedPermissionSet.ForLevel(level), clientInfo).Level;

    public static bool HidesOnlinePlayers(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.HidesOnlinePlayers();
    public static bool SetHidesOnlinePlayers(MinecraftApi minecraftApi, bool hide, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetHidesOnlinePlayers(hide, clientInfo);

    public static bool RepliesToStatus(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.RepliesToStatus();
    public static bool SetRepliesToStatus(MinecraftApi minecraftApi, bool enable, ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetRepliesToStatus(enable, clientInfo);

    public static int EntityBroadcastRangePercentage(MinecraftApi minecraftApi)
        => minecraftApi.ServerSettingsService.GetEntityBroadcastRangePercentage();
    public static int SetEntityBroadcastRangePercentage(MinecraftApi minecraftApi, int percentage,
        ClientInfo clientInfo)
        => minecraftApi.ServerSettingsService.SetEntityBroadcastRangePercentage(percentage, clientInfo);
}
