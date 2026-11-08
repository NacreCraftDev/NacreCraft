using NacreCraft.Game.Server.Notifications;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftServerStateService server lifecycle and messaging behind the RPC, maps to vanilla MinecraftServerStateService
public interface MinecraftServerStateService
{
    bool IsReady();

    bool SaveEverything(bool suppressLogs, bool flush, bool force, ClientInfo clientInfo);

    void Halt(bool waitForShutdown, ClientInfo clientInfo);

    void SendSystemMessage(Component message, ClientInfo clientInfo);

    void SendSystemMessage(Component message, bool overlay,
        IReadOnlyCollection<NacreCraft.Game.Server.ServerPlayer> players, ClientInfo clientInfo);

    void BroadcastSystemMessage(Component message, bool overlay, ClientInfo clientInfo);
}

//MinecraftServerStateServiceImpl state service over MinecraftServer
//Vanilla's saveEverything distinguishes suppressLogs/flush/force; NC's SaveAllNow flushes everything, so the flags only reach the log
public sealed class MinecraftServerStateServiceImpl(NotificationManager notificationManager, JsonRpcLogger jsonRpcLogger)
    : MinecraftServerStateService
{
    private NacreCraft.Game.Server.MinecraftServer Server()
        => notificationManager.Server() ?? throw new InvalidOperationException("Server is not initialized");

    public bool IsReady() => notificationManager.Server() is not null;

    public bool SaveEverything(bool suppressLogs, bool flush, bool force, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Save everything. SuppressLogs: {}, flush: {}, force: {}", suppressLogs, flush,
            force);
        Server().SaveAllNow();
        return true;
    }

    public void Halt(bool waitForShutdown, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Halt server. WaitForShutdown: {}", waitForShutdown);
        Server().Stop();
    }

    public void SendSystemMessage(Component message, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Send system message: '{}'", message.GetString());
        Server().PlayerList.BroadcastSystemMessage(message, false);
    }

    public void SendSystemMessage(Component message, bool overlay,
        IReadOnlyCollection<NacreCraft.Game.Server.ServerPlayer> players, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Send system message to '{}' players (overlay: {}): '{}'", players.Count, overlay,
            message.GetString());
        foreach (var player in players) player.SendSystemMessage(message, overlay);
    }

    public void BroadcastSystemMessage(Component message, bool overlay, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Broadcast system message (overlay: {}): '{}'", overlay, message.GetString());
        Server().PlayerList.BroadcastSystemMessage(message, overlay);
    }
}

//MinecraftExecutorService runs work on the server thread, maps to vanilla MinecraftExecutorService
public interface MinecraftExecutorService
{
    Task<T> Submit<T>(Func<T> supplier);

    Task Submit(System.Action action);
}

//MinecraftExecutorServiceImpl executor service, degraded to the thread pool because the server task queue is not exposed yet
public sealed class MinecraftExecutorServiceImpl(NotificationManager notificationManager) : MinecraftExecutorService
{
    public Task<T> Submit<T>(Func<T> supplier) => Task.Run(supplier);

    public Task Submit(System.Action action) => Task.Run(action);
}
