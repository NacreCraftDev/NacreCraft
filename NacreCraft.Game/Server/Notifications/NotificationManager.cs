using NacreCraft.Game.World.Level;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Notifications;

//NotificationManager fans every notification out to the registered services, maps to vanilla net.minecraft.server.notifications.NotificationManager
//Vanilla holds a DedicatedServer; NC keeps the game-layer MinecraftServer so Game does not depend on the Server project
public sealed class NotificationManager : NotificationService
{
    private readonly List<NotificationService> _notificationServices = new();
    private MinecraftServer? _server;

    public void RegisterService(NotificationService notificationService) => _notificationServices.Add(notificationService);

    public void SetServer(MinecraftServer server)
    {
        if (_server is not null) throw new InvalidOperationException("Server already set");
        _server = server;
    }

    public MinecraftServer? Server() => _server;

    public void PlayerJoined(ServerPlayer player)
    {
        foreach (var service in _notificationServices) service.PlayerJoined(player);
    }

    public void PlayerLeft(ServerPlayer player)
    {
        foreach (var service in _notificationServices) service.PlayerLeft(player);
    }

    public void ServerStarted()
    {
        foreach (var service in _notificationServices) service.ServerStarted();
    }

    public void ServerShuttingDown()
    {
        foreach (var service in _notificationServices) service.ServerShuttingDown();
    }

    public void ServerSaveStarted()
    {
        foreach (var service in _notificationServices) service.ServerSaveStarted();
    }

    public void ServerSaveCompleted()
    {
        foreach (var service in _notificationServices) service.ServerSaveCompleted();
    }

    public void ServerActivityOccured()
    {
        foreach (var service in _notificationServices) service.ServerActivityOccured();
    }

    public void PlayerOped(GameProfile @operator)
    {
        foreach (var service in _notificationServices) service.PlayerOped(@operator);
    }

    public void PlayerDeoped(GameProfile @operator)
    {
        foreach (var service in _notificationServices) service.PlayerDeoped(@operator);
    }

    public void PlayerAddedToAllowlist(NameAndId player)
    {
        foreach (var service in _notificationServices) service.PlayerAddedToAllowlist(player);
    }

    public void PlayerRemovedFromAllowlist(NameAndId player)
    {
        foreach (var service in _notificationServices) service.PlayerRemovedFromAllowlist(player);
    }

    public void IpBanned(IpBanEntry ban)
    {
        foreach (var service in _notificationServices) service.IpBanned(ban);
    }

    public void IpUnbanned(string ip)
    {
        foreach (var service in _notificationServices) service.IpUnbanned(ip);
    }

    public void PlayerBanned(BanEntry ban)
    {
        foreach (var service in _notificationServices) service.PlayerBanned(ban);
    }

    public void PlayerUnbanned(NameAndId player)
    {
        foreach (var service in _notificationServices) service.PlayerUnbanned(player);
    }

    public void OnGameRuleChanged<T>(GameRule<T> gameRule, T value)
    {
        foreach (var service in _notificationServices) service.OnGameRuleChanged(gameRule, value);
    }

    public void StatusHeartbeat()
    {
        foreach (var service in _notificationServices) service.StatusHeartbeat();
    }
}
