using NacreCraft.Game.World.Level;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Notifications;

//EmptyNotificationService implements every notification as a no-op, maps to vanilla net.minecraft.server.notifications.EmptyNotificationService
public sealed class EmptyNotificationService : NotificationService
{
    public void PlayerJoined(ServerPlayer player)
    {
    }

    public void PlayerLeft(ServerPlayer player)
    {
    }

    public void ServerStarted()
    {
    }

    public void ServerShuttingDown()
    {
    }

    public void ServerSaveStarted()
    {
    }

    public void ServerSaveCompleted()
    {
    }

    public void ServerActivityOccured()
    {
    }

    public void PlayerOped(GameProfile @operator)
    {
    }

    public void PlayerDeoped(GameProfile @operator)
    {
    }

    public void PlayerAddedToAllowlist(NameAndId player)
    {
    }

    public void PlayerRemovedFromAllowlist(NameAndId player)
    {
    }

    public void IpBanned(IpBanEntry ban)
    {
    }

    public void IpUnbanned(string ip)
    {
    }

    public void PlayerBanned(BanEntry ban)
    {
    }

    public void PlayerUnbanned(NameAndId player)
    {
    }

    public void OnGameRuleChanged<T>(GameRule<T> gameRule, T value)
    {
    }

    public void StatusHeartbeat()
    {
    }
}
