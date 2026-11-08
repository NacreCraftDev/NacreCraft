using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.Players;
using NacreCraft.Network.Chat;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.JsonRpc.Methods;

//BanlistService player ban RPC operations, maps to vanilla net.minecraft.server.jsonrpc.methods.BanlistService
//The game-side BanList persists string timestamps, so expiry travels as an ISO string instead of an Instant
public static class BanlistService
{
    private const string BanSource = "Management server";

    //UserBanDto one player ban payload, maps to vanilla BanlistService.UserBanDto
    public sealed record UserBanDto(PlayerDto Player, Optional<string> Reason, Optional<string> Source,
        Optional<string> Expires)
    {
        public static readonly MapCodec<UserBanDto> CODEC = RecordCodecBuilder.Of4(
            Codecs.AsCodec(PlayerDto.CODEC).FieldOf("player").ForGetter((UserBanDto dto) => dto.Player),
            Codecs.String.OptionalFieldOf("reason").ForGetter((UserBanDto dto) => dto.Reason),
            Codecs.String.OptionalFieldOf("source").ForGetter((UserBanDto dto) => dto.Source),
            Codecs.String.OptionalFieldOf("expires").ForGetter((UserBanDto dto) => dto.Expires),
            (player, reason, source, expires) => new UserBanDto(player, reason, source, expires));

        public static UserBanDto From(UserBanListEntry entry)
            => new(PlayerDto.From(entry.GetUser()), Optional<string>.Of(entry.Reason),
                Optional<string>.Of(entry.Source), Optional<string>.Of(entry.Expires));
    }

    private sealed record UserBan(NameAndId Player, string Reason, string Source, string Expires);

    public static List<UserBanDto> Get(MinecraftApi minecraftApi)
        => minecraftApi.BanListService.GetUserBanEntries()
            .Where(entry => entry.GetUser() is not null)
            .Select(UserBanDto.From)
            .ToList();

    public static async Task<List<UserBanDto>> Add(MinecraftApi minecraftApi, List<UserBanDto> bans,
        ClientInfo clientInfo)
    {
        var fetch = bans.Select(async ban =>
        {
            var user = await minecraftApi.PlayerListService.GetUser(ban.Player.Id, ban.Player.Name);
            return user.IsPresent ? Optional<UserBan>.Of(ToUserBan(ban, user.Get())) : Optional<UserBan>.Empty();
        }).ToList();
        foreach (var ban in await Task.WhenAll(fetch))
        {
            if (!ban.IsPresent) continue;
            var value = ban.Get();
            minecraftApi.BanListService.AddUserBan(ToEntry(value), clientInfo);
            var player = minecraftApi.PlayerListService.GetPlayer(Guid.TryParse(value.Player.Id, out var id) ? id : Guid.Empty);
            player?.Disconnect(Component.Translatable("multiplayer.disconnect.banned"));
        }
        return Get(minecraftApi);
    }

    public static List<UserBanDto> Clear(MinecraftApi minecraftApi, ClientInfo clientInfo)
    {
        minecraftApi.BanListService.ClearUserBans(clientInfo);
        return Get(minecraftApi);
    }

    public static async Task<List<UserBanDto>> Remove(MinecraftApi minecraftApi, List<PlayerDto> remove,
        ClientInfo clientInfo)
    {
        var fetch = remove.Select(player => minecraftApi.PlayerListService.GetUser(player.Id, player.Name)).ToList();
        foreach (var user in await Task.WhenAll(fetch))
        {
            if (user.IsPresent) minecraftApi.BanListService.RemoveUserBan(user.Get(), clientInfo);
        }
        return Get(minecraftApi);
    }

    public static async Task<List<UserBanDto>> Set(MinecraftApi minecraftApi, List<UserBanDto> bans,
        ClientInfo clientInfo)
    {
        var fetch = bans.Select(async ban =>
        {
            var user = await minecraftApi.PlayerListService.GetUser(ban.Player.Id, ban.Player.Name);
            return user.IsPresent ? Optional<UserBan>.Of(ToUserBan(ban, user.Get())) : Optional<UserBan>.Empty();
        }).ToList();
        var finalBans = new HashSet<UserBan>();
        foreach (var ban in await Task.WhenAll(fetch))
        {
            if (ban.IsPresent) finalBans.Add(ban.Get());
        }
        var currentBans = minecraftApi.BanListService.GetUserBanEntries()
            .Where(entry => entry.GetUser() is not null)
            .Select(entry => new UserBan(entry.GetUser(), entry.Reason, entry.Source, entry.Expires))
            .ToHashSet();
        foreach (var ban in currentBans.Where(ban => !finalBans.Contains(ban)))
            minecraftApi.BanListService.RemoveUserBan(ban.Player, clientInfo);
        foreach (var ban in finalBans.Where(ban => !currentBans.Contains(ban)))
        {
            minecraftApi.BanListService.AddUserBan(ToEntry(ban), clientInfo);
            var player = minecraftApi.PlayerListService.GetPlayer(Guid.TryParse(ban.Player.Id, out var id) ? id : Guid.Empty);
            player?.Disconnect(Component.Translatable("multiplayer.disconnect.banned"));
        }
        return Get(minecraftApi);
    }

    private static UserBan ToUserBan(UserBanDto dto, NameAndId user)
        => new(user, dto.Reason.OrElse("Banned by an operator."), dto.Source.OrElse(BanSource),
            dto.Expires.OrElse("forever"));

    private static UserBanListEntry ToEntry(UserBan ban)
        => new(ban.Player, null, ban.Reason, ban.Source, ban.Expires);
}
