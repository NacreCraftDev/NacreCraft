using NacreCraft.Game.World.Level;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.Notifications;

//NotificationService receives server lifecycle and administrative events, maps to vanilla net.minecraft.server.notifications.NotificationService
//Vanilla entry types IpBanListEntry/UserBanListEntry/ServerOpListEntry collapse to the entry types NC actually has: IpBanEntry,
//BanEntry and GameProfile
public interface NotificationService
{
    void PlayerJoined(ServerPlayer player);

    void PlayerLeft(ServerPlayer player);

    void ServerStarted();

    void ServerShuttingDown();

    void ServerSaveStarted();

    void ServerSaveCompleted();

    void ServerActivityOccured();

    void PlayerOped(GameProfile @operator);

    void PlayerDeoped(GameProfile @operator);

    void PlayerAddedToAllowlist(NameAndId player);

    void PlayerRemovedFromAllowlist(NameAndId player);

    void IpBanned(IpBanEntry ban);

    void IpUnbanned(string ip);

    void PlayerBanned(BanEntry ban);

    void PlayerUnbanned(NameAndId player);

    void OnGameRuleChanged<T>(GameRule<T> gameRule, T value);

    void StatusHeartbeat();
}
