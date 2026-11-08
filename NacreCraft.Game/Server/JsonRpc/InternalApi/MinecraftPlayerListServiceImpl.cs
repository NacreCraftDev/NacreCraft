using NacreCraft.Codec;
using NacreCraft.Game.Server.Notifications;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftPlayerListServiceImpl online player service over the server player list, maps to vanilla MinecraftPlayerListServiceImpl
//The name-to-id cache and the session service are not ported, so the auth-backed fetches return empty; cached lookups and online lookups work
public sealed class MinecraftPlayerListServiceImpl(NotificationManager notificationManager, JsonRpcLogger jsonRpcLogger)
    : MinecraftPlayerListService
{
    private NacreCraft.Game.Server.MinecraftServer Server()
        => notificationManager.Server() ?? throw new InvalidOperationException("Server is not initialized");

    public IReadOnlyList<NacreCraft.Game.Server.ServerPlayer> GetPlayers()
        => Server().PlayerList.Players;

    public NacreCraft.Game.Server.ServerPlayer? GetPlayer(Guid uuid)
        => Server().PlayerList.Players.FirstOrDefault(player => player.Profile.Id == uuid);

    public Optional<NameAndId> FetchUserByName(string name) => Optional<NameAndId>.Empty();

    public Optional<NameAndId> FetchUserById(Guid id) => Optional<NameAndId>.Empty();

    public Optional<NameAndId> GetCachedUserById(Guid id) => Optional<NameAndId>.Empty();

    public Optional<NacreCraft.Game.Server.ServerPlayer> GetPlayer(Optional<Guid> id, Optional<string> name)
    {
        if (id.IsPresent)
        {
            var player = GetPlayer(id.Get());
            return player is null ? Optional<NacreCraft.Game.Server.ServerPlayer>.Empty() : Optional<NacreCraft.Game.Server.ServerPlayer>.Of(player);
        }
        if (name.IsPresent)
        {
            var player = GetPlayerByName(name.Get());
            return player is null ? Optional<NacreCraft.Game.Server.ServerPlayer>.Empty() : Optional<NacreCraft.Game.Server.ServerPlayer>.Of(player);
        }
        return Optional<NacreCraft.Game.Server.ServerPlayer>.Empty();
    }

    public IReadOnlyList<NacreCraft.Game.Server.ServerPlayer> GetPlayersWithAddress(string ip)
        => Server().PlayerList.Players
            .Where(player => string.Equals(player.Connection.RemoteAddress, ip, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public NacreCraft.Game.Server.ServerPlayer? GetPlayerByName(string name)
        => Server().PlayerList.GetPlayerByName(name);

    public void Remove(NacreCraft.Game.Server.ServerPlayer player, ClientInfo clientInfo)
    {
        jsonRpcLogger.Log(clientInfo, "Remove player '{}'", player.Profile.Name);
        Server().PlayerList.RemovePlayer(player);
    }
}
