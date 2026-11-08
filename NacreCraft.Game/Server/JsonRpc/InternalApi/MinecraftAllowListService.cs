using NacreCraft.Codec;
using NacreCraft.Game.Server.Players;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftAllowListService allowlist operations behind the RPC, maps to vanilla MinecraftAllowListService
public interface MinecraftAllowListService
{
    IReadOnlyCollection<UserWhiteListEntry> GetEntries();

    bool Add(UserWhiteListEntry infos, ClientInfo clientInfo);

    void Clear(ClientInfo clientInfo);

    void Remove(NameAndId nameAndId, ClientInfo clientInfo);

    void KickUnlistedPlayers(ClientInfo clientInfo);
}

//MinecraftPlayerListService online player and profile lookups behind the RPC, maps to vanilla MinecraftPlayerListService
public interface MinecraftPlayerListService
{
    IReadOnlyList<NacreCraft.Game.Server.ServerPlayer> GetPlayers();

    NacreCraft.Game.Server.ServerPlayer? GetPlayer(Guid uuid);

    Optional<NameAndId> FetchUserByName(string name);

    Optional<NameAndId> FetchUserById(Guid id);

    Optional<NameAndId> GetCachedUserById(Guid id);

    Optional<NacreCraft.Game.Server.ServerPlayer> GetPlayer(Optional<Guid> id, Optional<string> name);

    IReadOnlyList<NacreCraft.Game.Server.ServerPlayer> GetPlayersWithAddress(string ip);

    NacreCraft.Game.Server.ServerPlayer? GetPlayerByName(string name);

    void Remove(NacreCraft.Game.Server.ServerPlayer player, ClientInfo clientInfo);

    //GetUser resolves a name/id pair, preferring the cache and falling back to the auth fetch
    //Vanilla runs the fetch on the non-critical IO pool; the same shape is kept here
    Task<Optional<NameAndId>> GetUser(Optional<Guid> id, Optional<string> name)
    {
        if (id.IsPresent)
        {
            var cached = GetCachedUserById(id.Get());
            if (cached.IsPresent) return Task.FromResult(cached);
            return Task.Run(() => FetchUserById(id.Get()));
        }
        if (name.IsPresent) return Task.Run(() => FetchUserByName(name.Get()));
        return Task.FromResult(Optional<NameAndId>.Empty());
    }
}
