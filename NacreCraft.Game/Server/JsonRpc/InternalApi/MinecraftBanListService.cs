using NacreCraft.Codec;
using NacreCraft.Game.Server.Notifications;
using NacreCraft.Game.Server.Players;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftBanListService ban list operations behind the RPC, maps to vanilla MinecraftBanListService
public interface MinecraftBanListService
{
    void AddUserBan(UserBanListEntry ban, ClientInfo clientInfo);

    void RemoveUserBan(NameAndId nameAndId, ClientInfo clientInfo);

    IReadOnlyCollection<UserBanListEntry> GetUserBanEntries();

    IReadOnlyCollection<IpBanListEntry> GetIpBanEntries();

    void AddIpBan(IpBanListEntry ipBanEntry, ClientInfo clientInfo);

    void ClearIpBans(ClientInfo clientInfo);

    void RemoveIpBan(string ip, ClientInfo clientInfo);

    void ClearUserBans(ClientInfo clientInfo);
}

//MinecraftOperatorListService operator list operations behind the RPC, maps to vanilla MinecraftOperatorListService
public interface MinecraftOperatorListService
{
    IReadOnlyCollection<ServerOpListEntry> GetEntries();

    void Op(NameAndId nameAndId, Optional<PermissionLevel> permissionLevel, Optional<bool> canBypassPlayerLimit,
        ClientInfo clientInfo);

    void Op(NameAndId nameAndId, ClientInfo clientInfo);

    void Deop(NameAndId nameAndId, ClientInfo clientInfo);

    void Clear(ClientInfo clientInfo);
}

//MinecraftBanListServiceImpl ban list service over the server's BanList and IpBanList
public sealed class MinecraftBanListServiceImpl(NotificationManager notificationManager, JsonRpcLogger jsonRpcLogger)
    : MinecraftBanListService
{
    private NacreCraft.Game.Server.MinecraftServer Server()
        => notificationManager.Server() ?? throw new InvalidOperationException("Server is not initialized");

    public void AddUserBan(UserBanListEntry ban, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Add player '{}' to banlist. Reason: '{}'", ban.User.Name, ban.Reason);
        Server().BanList.Add(ToProfile(ban.User), ban.Source, ban.Reason, ban.Expires);
    }

    public void RemoveUserBan(NameAndId nameAndId, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Remove player '{}' from banlist", nameAndId.Name);
        Server().BanList.Remove(ToProfile(nameAndId));
    }

    public IReadOnlyCollection<UserBanListEntry> GetUserBanEntries()
        => Server().BanList.Entries.Select(entry => new UserBanListEntry(
            new NameAndId { Id = entry.Id.ToString(), Name = entry.Name }, null, entry.Reason, entry.Source,
            entry.Expires)).ToList();

    public IReadOnlyCollection<IpBanListEntry> GetIpBanEntries()
        => Server().IpBanList.Entries.Select(entry => new IpBanListEntry(entry.Ip, entry.Reason, entry.Source,
            entry.Expires)).ToList();

    public void AddIpBan(IpBanListEntry ipBanEntry, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Add ip '{}' to ban list", ipBanEntry.Ip);
        Server().IpBanList.Add(ipBanEntry.Ip, ipBanEntry.Source, ipBanEntry.Reason, ipBanEntry.Expires);
    }

    public void ClearIpBans(ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Clear ip ban list");
        Server().IpBanList.Clear();
    }

    public void RemoveIpBan(string ip, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Remove ip '{}' from ban list", ip);
        Server().IpBanList.Remove(ip);
    }

    public void ClearUserBans(ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Clear ban list");
        Server().BanList.Clear();
    }

    private static GameProfile ToProfile(NameAndId nameAndId)
        => new(Guid.TryParse(nameAndId.Id, out var id) ? id : Guid.Empty, nameAndId.Name);
}

//MinecraftOperatorListServiceImpl operator list service over the server's OpList
public sealed class MinecraftOperatorListServiceImpl(NotificationManager notificationManager, JsonRpcLogger jsonRpcLogger)
    : MinecraftOperatorListService
{
    private NacreCraft.Game.Server.MinecraftServer Server()
        => notificationManager.Server() ?? throw new InvalidOperationException("Server is not initialized");

    public IReadOnlyCollection<ServerOpListEntry> GetEntries()
        => Server().OpList.Entries.Select(entry => new ServerOpListEntry(
            new NameAndId { Id = entry.Id.ToString(), Name = entry.Name },
            (PermissionLevel)entry.Level, entry.BypassesPlayerLimit)).ToList();

    public void Op(NameAndId nameAndId, Optional<PermissionLevel> permissionLevel, Optional<bool> canBypassPlayerLimit,
        ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Op '{}'", nameAndId.Name);
        var level = permissionLevel.IsPresent ? (int)permissionLevel.Get() : 4;
        Server().OpList.Add(ToProfile(nameAndId), level, canBypassPlayerLimit.IsPresent && canBypassPlayerLimit.Get());
    }

    public void Op(NameAndId nameAndId, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Op '{}'", nameAndId.Name);
        Server().OpList.Add(ToProfile(nameAndId), 4);
    }

    public void Deop(NameAndId nameAndId, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Deop '{}'", nameAndId.Name);
        Server().OpList.Remove(ToProfile(nameAndId));
    }

    public void Clear(ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Clear operator list");
        Server().OpList.Clear();
    }

    private static GameProfile ToProfile(NameAndId nameAndId)
        => new(Guid.TryParse(nameAndId.Id, out var id) ? id : Guid.Empty, nameAndId.Name);
}
