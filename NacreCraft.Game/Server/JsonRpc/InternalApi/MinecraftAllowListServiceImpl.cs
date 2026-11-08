using NacreCraft.Game.Server.Notifications;
using NacreCraft.Game.Server.Players;
using NacreCraft.Network.Chat;
using NacreCraft.Network.Protocol.Login;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftAllowListServiceImpl allowlist service over the server's WhiteList, maps to vanilla MinecraftAllowListServiceImpl
//The game-side whitelist persists WhiteListEntry; this service converts at the boundary so RPC payloads keep the vanilla entry shape
public sealed class MinecraftAllowListServiceImpl(NotificationManager notificationManager, JsonRpcLogger jsonRpcLogger)
    : MinecraftAllowListService
{
    private NacreCraft.Game.Server.MinecraftServer Server()
        => notificationManager.Server() ?? throw new InvalidOperationException("Server is not initialized");

    public IReadOnlyCollection<UserWhiteListEntry> GetEntries()
        => Server().WhiteList.Entries
            .Select(entry => new UserWhiteListEntry(new NameAndId { Id = entry.Id.ToString(), Name = entry.Name }))
            .ToList();

    public bool Add(UserWhiteListEntry infos, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Add player '{}' to allowlist", infos.User.Name);
        return Server().WhiteList.Add(ToProfile(infos.User));
    }

    public void Clear(ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Clear allowlist");
        Server().WhiteList.Clear();
    }

    public void Remove(NameAndId nameAndId, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Remove player '{}' from allowlist", nameAndId.Name);
        Server().WhiteList.Remove(ToProfile(nameAndId));
    }

    public void KickUnlistedPlayers(ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Kick unlisted players");
        var server = Server();
        foreach (var player in server.PlayerList.Players.ToList())
        {
            var profile = player.Profile;
            if (server.WhiteList.IsAllowed(profile) || server.OpList.IsOp(profile)) continue;
            player.Disconnect(Component.Translatable("multiplayer.disconnect.not_whitelisted"));
        }
    }

    private static GameProfile ToProfile(NameAndId nameAndId)
        => new(Guid.TryParse(nameAndId.Id, out var id) ? id : Guid.Empty, nameAndId.Name);
}
