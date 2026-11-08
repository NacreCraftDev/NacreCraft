using NacreCraft.Codec;
using NacreCraft.Game.Server.Notifications;
using NacreCraft.Game.World.Level;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftServerSettingsService server settings behind the RPC, maps to vanilla MinecraftServerSettingsService
//NC applies most of these by writing server.properties; the live effect of some fields still depends on the startup snapshot
public interface MinecraftServerSettingsService
{
    bool IsAutoSave();
    bool SetAutoSave(bool enabled, ClientInfo clientInfo);
    Difficulty GetDifficulty();
    Difficulty SetDifficulty(Difficulty difficulty, ClientInfo clientInfo);
    bool IsEnforceWhitelist();
    bool SetEnforceWhitelist(bool enforce, ClientInfo clientInfo);
    bool IsUsingWhitelist();
    bool SetUsingWhitelist(bool use, ClientInfo clientInfo);
    int GetMaxPlayers();
    int SetMaxPlayers(int maxPlayers, ClientInfo clientInfo);
    int GetPauseWhenEmptySeconds();
    int SetPauseWhenEmptySeconds(int emptySeconds, ClientInfo clientInfo);
    int GetPlayerIdleTimeout();
    int SetPlayerIdleTimeout(int idleTime, ClientInfo clientInfo);
    bool AllowFlight();
    bool SetAllowFlight(bool allow, ClientInfo clientInfo);
    int GetSpawnProtectionRadius();
    int SetSpawnProtectionRadius(int spawnProtection, ClientInfo clientInfo);
    string GetMotd();
    string SetMotd(string motd, ClientInfo clientInfo);
    bool ForceGameMode();
    bool SetForceGameMode(bool force, ClientInfo clientInfo);
    GameType GetGameMode();
    GameType SetGameMode(GameType gameMode, ClientInfo clientInfo);
    int GetViewDistance();
    int SetViewDistance(int viewDistance, ClientInfo clientInfo);
    int GetSimulationDistance();
    int SetSimulationDistance(int simulationDistance, ClientInfo clientInfo);
    bool AcceptsTransfers();
    bool SetAcceptsTransfers(bool accept, ClientInfo clientInfo);
    int GetStatusHeartbeatInterval();
    int SetStatusHeartbeatInterval(int statusHeartbeatInterval, ClientInfo clientInfo);
    LevelBasedPermissionSet GetOperatorUserPermissions();
    LevelBasedPermissionSet SetOperatorUserPermissions(LevelBasedPermissionSet level, ClientInfo clientInfo);
    bool HidesOnlinePlayers();
    bool SetHidesOnlinePlayers(bool hide, ClientInfo clientInfo);
    bool RepliesToStatus();
    bool SetRepliesToStatus(bool enable, ClientInfo clientInfo);
    int GetEntityBroadcastRangePercentage();
    int SetEntityBroadcastRangePercentage(int percentage, ClientInfo clientInfo);
}

//MinecraftServerSettingsServiceImpl settings service over ServerSettings and the level data difficulty
public sealed class MinecraftServerSettingsServiceImpl(NotificationManager notificationManager,
    JsonRpcLogger jsonRpcLogger) : MinecraftServerSettingsService
{
    private NacreCraft.Game.Server.MinecraftServer Server()
        => notificationManager.Server() ?? throw new InvalidOperationException("Server is not initialized");

    private NacreCraft.ServerSettings Settings => Server().Settings;

    public bool IsAutoSave() => Settings.AutoSave;
    public bool SetAutoSave(bool enabled, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update autosave from {} to {}", IsAutoSave(), enabled);
        Settings.SetAutoSave(enabled);
        Settings.SaveCurrent();
        return IsAutoSave();
    }

    public Difficulty GetDifficulty()
        => Difficulty.ByName(Server().LevelData.DifficultyName) ?? Difficulty.Normal;
    public Difficulty SetDifficulty(Difficulty difficulty, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update difficulty from '{}' to '{}'", GetDifficulty(), difficulty);
        Server().LevelData.DifficultyName = difficulty.Name;
        Settings.SetDifficulty(difficulty.Name);
        Settings.SaveCurrent();
        return GetDifficulty();
    }

    public bool IsEnforceWhitelist() => Settings.EnforceWhiteList;
    public bool SetEnforceWhitelist(bool enforce, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update enforce allowlist from {} to {}", IsEnforceWhitelist(), enforce);
        Settings.SetEnforceWhiteList(enforce);
        Settings.SaveCurrent();
        return IsEnforceWhitelist();
    }

    public bool IsUsingWhitelist() => Server().IsWhiteListEnabled;
    public bool SetUsingWhitelist(bool use, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update using allowlist from {} to {}", IsUsingWhitelist(), use);
        Server().IsWhiteListEnabled = use;
        Settings.SetWhiteList(use);
        Settings.SaveCurrent();
        return IsUsingWhitelist();
    }

    public int GetMaxPlayers() => Settings.MaxPlayers;
    public int SetMaxPlayers(int maxPlayers, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update max players from {} to {}", GetMaxPlayers(), maxPlayers);
        Settings.SetMaxPlayers(maxPlayers);
        Settings.SaveCurrent();
        //The new limit applies to the player list immediately, maps to the mutable maxPlayers value in vanilla
        Server().ApplySettings();
        return GetMaxPlayers();
    }

    public int GetPauseWhenEmptySeconds() => Settings.PauseWhenEmptySeconds;
    public int SetPauseWhenEmptySeconds(int emptySeconds, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update pause when empty from {} seconds to {} seconds",
            GetPauseWhenEmptySeconds(), emptySeconds);
        Settings.SetPauseWhenEmptySeconds(emptySeconds);
        Settings.SaveCurrent();
        return GetPauseWhenEmptySeconds();
    }

    public int GetPlayerIdleTimeout() => Settings.PlayerIdleTimeout;
    public int SetPlayerIdleTimeout(int idleTime, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update player idle timeout from {} minutes to {} minutes",
            GetPlayerIdleTimeout(), idleTime);
        Settings.SetPlayerIdleTimeout(idleTime);
        Settings.SaveCurrent();
        return GetPlayerIdleTimeout();
    }

    public bool AllowFlight() => Settings.AllowFlight;
    public bool SetAllowFlight(bool allow, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update allow flight from {} to {}", AllowFlight(), allow);
        Settings.SetAllowFlight(allow);
        Settings.SaveCurrent();
        return AllowFlight();
    }

    public int GetSpawnProtectionRadius() => Settings.SpawnProtection;
    public int SetSpawnProtectionRadius(int spawnProtection, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update spawn protection radius from {} to {}", GetSpawnProtectionRadius(),
            spawnProtection);
        Settings.SetSpawnProtection(spawnProtection);
        Settings.SaveCurrent();
        return GetSpawnProtectionRadius();
    }

    public string GetMotd() => Settings.Motd;
    public string SetMotd(string motd, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update MOTD from '{}' to '{}'", GetMotd(), motd);
        Settings.SetMotd(motd);
        Settings.SaveCurrent();
        return GetMotd();
    }

    public bool ForceGameMode() => Settings.ForceGameMode;
    public bool SetForceGameMode(bool force, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update force game mode from {} to {}", ForceGameMode(), force);
        Settings.SetForceGameMode(force);
        Settings.SaveCurrent();
        return ForceGameMode();
    }

    public GameType GetGameMode() => GameType.ByName(Settings.Gamemode) ?? GameType.Survival;
    public GameType SetGameMode(GameType gameMode, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update game mode from '{}' to '{}'", GetGameMode(), gameMode);
        Settings.SetGamemode(gameMode.Name);
        Settings.SaveCurrent();
        return GetGameMode();
    }

    public int GetViewDistance() => Settings.ViewDistance;
    public int SetViewDistance(int viewDistance, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update view distance from {} to {}", GetViewDistance(), viewDistance);
        Settings.SetViewDistance(viewDistance);
        Settings.SaveCurrent();
        //Online players switch radius now, maps to the setViewDistance listener of the vanilla mutable value
        Server().ApplySettings();
        return GetViewDistance();
    }

    public int GetSimulationDistance() => Settings.SimulationDistance;
    public int SetSimulationDistance(int simulationDistance, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update simulation distance from {} to {}", GetSimulationDistance(),
            simulationDistance);
        Settings.SetSimulationDistance(simulationDistance);
        Settings.SaveCurrent();
        //Both the client packet and the ticking radius follow, maps to the setSimulationDistance listener in vanilla
        Server().ApplySettings();
        return GetSimulationDistance();
    }

    public bool AcceptsTransfers() => Settings.AcceptsTransfers;
    public bool SetAcceptsTransfers(bool accept, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update accepts transfers from {} to {}", AcceptsTransfers(), accept);
        Settings.SetAcceptsTransfers(accept);
        Settings.SaveCurrent();
        return AcceptsTransfers();
    }

    public int GetStatusHeartbeatInterval() => Settings.StatusHeartbeatInterval;
    public int SetStatusHeartbeatInterval(int statusHeartbeatInterval, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update status heartbeat interval from {} to {}", GetStatusHeartbeatInterval(),
            statusHeartbeatInterval);
        Settings.SetStatusHeartbeatInterval(statusHeartbeatInterval);
        Settings.SaveCurrent();
        return GetStatusHeartbeatInterval();
    }

    public LevelBasedPermissionSet GetOperatorUserPermissions()
        => LevelBasedPermissionSet.ForLevel((PermissionLevel)Settings.OpPermissionLevel);
    public LevelBasedPermissionSet SetOperatorUserPermissions(LevelBasedPermissionSet level, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update operator user permissions from '{}' to '{}'",
            GetOperatorUserPermissions().Level, level.Level);
        Settings.SetOpPermissionLevel((int)level.Level);
        Settings.SaveCurrent();
        return GetOperatorUserPermissions();
    }

    public bool HidesOnlinePlayers() => Settings.HidesOnlinePlayers;
    public bool SetHidesOnlinePlayers(bool hide, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update hides online players from {} to {}", HidesOnlinePlayers(), hide);
        Settings.SetHidesOnlinePlayers(hide);
        Settings.SaveCurrent();
        return HidesOnlinePlayers();
    }

    public bool RepliesToStatus() => Settings.RepliesToStatus;
    public bool SetRepliesToStatus(bool enable, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update replies to status from {} to {}", RepliesToStatus(), enable);
        Settings.SetRepliesToStatus(enable);
        Settings.SaveCurrent();
        return RepliesToStatus();
    }

    public int GetEntityBroadcastRangePercentage() => Settings.EntityBroadcastRangePercentage;
    public int SetEntityBroadcastRangePercentage(int percentage, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Update entity broadcast range percentage from {} to {}",
            GetEntityBroadcastRangePercentage(), percentage);
        Settings.SetEntityBroadcastRangePercentage(percentage);
        Settings.SaveCurrent();
        return GetEntityBroadcastRangePercentage();
    }
}
