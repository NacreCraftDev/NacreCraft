using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.Players;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.JsonRpc.Methods;

//AllowlistService allowlist RPC operations, maps to vanilla net.minecraft.server.jsonrpc.methods.AllowlistService
public static class AllowlistService
{
    public static List<PlayerDto> Get(MinecraftApi minecraftApi)
        => minecraftApi.AllowListService.GetEntries()
            .Where(entry => entry.GetUser() is not null)
            .Select(entry => PlayerDto.From(entry.GetUser()))
            .ToList();

    public static async Task<List<PlayerDto>> Add(MinecraftApi minecraftApi, List<PlayerDto> playerDtos,
        ClientInfo clientInfo)
    {
        var fetch = playerDtos.Select(dto => minecraftApi.PlayerListService.GetUser(dto.Id, dto.Name)).ToList();
        foreach (var user in await Task.WhenAll(fetch))
        {
            if (user.IsPresent) minecraftApi.AllowListService.Add(new UserWhiteListEntry(user.Get()), clientInfo);
        }
        return Get(minecraftApi);
    }

    public static List<PlayerDto> Clear(MinecraftApi minecraftApi, ClientInfo clientInfo)
    {
        minecraftApi.AllowListService.Clear(clientInfo);
        return Get(minecraftApi);
    }

    public static async Task<List<PlayerDto>> Remove(MinecraftApi minecraftApi, List<PlayerDto> playerDtos,
        ClientInfo clientInfo)
    {
        var fetch = playerDtos.Select(dto => minecraftApi.PlayerListService.GetUser(dto.Id, dto.Name)).ToList();
        foreach (var user in await Task.WhenAll(fetch))
        {
            if (user.IsPresent) minecraftApi.AllowListService.Remove(user.Get(), clientInfo);
        }
        minecraftApi.AllowListService.KickUnlistedPlayers(clientInfo);
        return Get(minecraftApi);
    }

    public static async Task<List<PlayerDto>> Set(MinecraftApi minecraftApi, List<PlayerDto> playerDtos,
        ClientInfo clientInfo)
    {
        var fetch = playerDtos.Select(dto => minecraftApi.PlayerListService.GetUser(dto.Id, dto.Name)).ToList();
        var finalAllowList = new HashSet<NameAndId>();
        foreach (var user in await Task.WhenAll(fetch))
        {
            if (user.IsPresent) finalAllowList.Add(user.Get());
        }
        var currentAllowList = minecraftApi.AllowListService.GetEntries()
            .Select(entry => entry.GetUser())
            .ToHashSet();
        foreach (var user in currentAllowList.Where(user => !finalAllowList.Contains(user)))
            minecraftApi.AllowListService.Remove(user, clientInfo);
        foreach (var user in finalAllowList.Where(user => !currentAllowList.Contains(user)))
            minecraftApi.AllowListService.Add(new UserWhiteListEntry(user), clientInfo);
        minecraftApi.AllowListService.KickUnlistedPlayers(clientInfo);
        return Get(minecraftApi);
    }
}
