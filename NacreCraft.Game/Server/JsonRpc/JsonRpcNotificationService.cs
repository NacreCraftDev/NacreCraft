using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.JsonRpc.Methods;
using NacreCraft.Game.Server.Notifications;
using NacreCraft.Game.Server.Players;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;
using NacreCraft.Registry;
using RegistryMethod = NacreCraft.Registry.OutgoingRpcMethod<object, object>;

namespace NacreCraft.Game.Server.JsonRpc;

//JsonRpcNotificationService forwards server notifications to the management connections, maps to vanilla JsonRpcNotificationService
//The game-side notification service uses GameProfile/NameAndId/BanEntry shapes, so the payloads are converted here
public sealed class JsonRpcNotificationService(MinecraftApi minecraftApi, IRpcConnectionSet connections)
    : NotificationService
{
    public void PlayerJoined(NacreCraft.Game.Server.ServerPlayer player)
        => Broadcast(OutgoingRpcMethods.PLAYER_JOINED, PlayerDto.From(player));

    public void PlayerLeft(NacreCraft.Game.Server.ServerPlayer player)
        => Broadcast(OutgoingRpcMethods.PLAYER_LEFT, PlayerDto.From(player));

    public void ServerStarted() => Broadcast(OutgoingRpcMethods.SERVER_STARTED);
    public void ServerShuttingDown() => Broadcast(OutgoingRpcMethods.SERVER_SHUTTING_DOWN);
    public void ServerSaveStarted() => Broadcast(OutgoingRpcMethods.SERVER_SAVE_STARTED);
    public void ServerSaveCompleted() => Broadcast(OutgoingRpcMethods.SERVER_SAVE_COMPLETED);
    public void ServerActivityOccured() => Broadcast(OutgoingRpcMethods.SERVER_ACTIVITY_OCCURRED);

    public void PlayerOped(GameProfile @operator)
        => Broadcast(OutgoingRpcMethods.PLAYER_OPED, new OperatorService.OperatorDto(PlayerDto.From(@operator),
            Optional<PermissionLevel>.Empty(), Optional<bool>.Empty()));

    public void PlayerDeoped(GameProfile @operator)
        => Broadcast(OutgoingRpcMethods.PLAYER_DEOPED, new OperatorService.OperatorDto(PlayerDto.From(@operator),
            Optional<PermissionLevel>.Empty(), Optional<bool>.Empty()));

    public void PlayerAddedToAllowlist(NameAndId player)
        => Broadcast(OutgoingRpcMethods.PLAYER_ADDED_TO_ALLOWLIST, PlayerDto.From(player));

    public void PlayerRemovedFromAllowlist(NameAndId player)
        => Broadcast(OutgoingRpcMethods.PLAYER_REMOVED_FROM_ALLOWLIST, PlayerDto.From(player));

    public void IpBanned(IpBanEntry ban)
        => Broadcast(OutgoingRpcMethods.IP_BANNED,
            IpBanlistService.IpBanDto.From(new IpBanListEntry(ban.Ip, ban.Reason, ban.Source, ban.Expires)));

    public void IpUnbanned(string ip) => Broadcast(OutgoingRpcMethods.IP_UNBANNED, ip);

    public void PlayerBanned(BanEntry ban)
        => Broadcast(OutgoingRpcMethods.PLAYER_BANNED, BanlistService.UserBanDto.From(new UserBanListEntry(
            new NameAndId { Id = ban.Id.ToString(), Name = ban.Name }, null, ban.Reason, ban.Source, ban.Expires)));

    public void PlayerUnbanned(NameAndId player)
        => Broadcast(OutgoingRpcMethods.PLAYER_UNBANNED, PlayerDto.From(player));

    public void OnGameRuleChanged<T>(GameRule<T> gameRule, T value)
    {
        if (gameRule is not GameRule<object> rule) return;
        Broadcast(OutgoingRpcMethods.GAMERULE_CHANGED,
            GameRulesService.GetTypedRule(minecraftApi, rule, value!));
    }

    public void StatusHeartbeat() => Broadcast(OutgoingRpcMethods.STATUS_HEARTBEAT,
        ServerStateService.Status(minecraftApi));

    private void Broadcast(Reference<RegistryMethod> holder)
        => Broadcast<object?>(holder, null);

    private void Broadcast<Params>(Reference<RegistryMethod> holder, Params parameters)
    {
        var name = holder.UnwrapKey()?.Identifier
                   ?? throw new InvalidOperationException("RPC notification is not registered");
        var method = (OutgoingRpcMethod<Params, object?>)holder.Value;
        connections.ForEachConnection(connection => connection.SendNotification(name, method, parameters));
    }
}
