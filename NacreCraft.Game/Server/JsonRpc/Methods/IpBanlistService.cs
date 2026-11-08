using System.Net;
using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.Players;
using NacreCraft.Network.Chat;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.JsonRpc.Methods;

//IpBanlistService IP ban RPC operations, maps to vanilla net.minecraft.server.jsonrpc.methods.IpBanlistService
//An incoming ban may carry an IP literal or a player reference; a player reference bans that player's current address
public static class IpBanlistService
{
    private const string BanSource = "Management server";

    //IncomingIpBanDto an incoming IP ban payload, maps to vanilla IncomingIpBanDto
    public sealed record IncomingIpBanDto(Optional<PlayerDto> Player, Optional<string> Ip, Optional<string> Reason,
        Optional<string> Source, Optional<string> Expires)
    {
        public static readonly MapCodec<IncomingIpBanDto> CODEC = RecordCodecBuilder.Of5(
            Codecs.AsCodec(PlayerDto.CODEC).OptionalFieldOf("player").ForGetter((IncomingIpBanDto dto) => dto.Player),
            Codecs.String.OptionalFieldOf("ip").ForGetter((IncomingIpBanDto dto) => dto.Ip),
            Codecs.String.OptionalFieldOf("reason").ForGetter((IncomingIpBanDto dto) => dto.Reason),
            Codecs.String.OptionalFieldOf("source").ForGetter((IncomingIpBanDto dto) => dto.Source),
            Codecs.String.OptionalFieldOf("expires").ForGetter((IncomingIpBanDto dto) => dto.Expires),
            (player, ip, reason, source, expires) => new IncomingIpBanDto(player, ip, reason, source, expires));
    }

    //IpBanDto an applied IP ban payload, maps to vanilla IpBanlistService.IpBanDto
    public sealed record IpBanDto(string Ip, Optional<string> Reason, Optional<string> Source,
        Optional<string> Expires)
    {
        public static readonly MapCodec<IpBanDto> CODEC = RecordCodecBuilder.Of4(
            Codecs.String.FieldOf("ip").ForGetter((IpBanDto dto) => dto.Ip),
            Codecs.String.OptionalFieldOf("reason").ForGetter((IpBanDto dto) => dto.Reason),
            Codecs.String.OptionalFieldOf("source").ForGetter((IpBanDto dto) => dto.Source),
            Codecs.String.OptionalFieldOf("expires").ForGetter((IpBanDto dto) => dto.Expires),
            (ip, reason, source, expires) => new IpBanDto(ip, reason, source, expires));

        public static IpBanDto From(IpBanListEntry entry)
            => new(entry.Ip, Optional<string>.Of(entry.Reason), Optional<string>.Of(entry.Source),
                Optional<string>.Of(entry.Expires));
    }

    private sealed record IpBan(string Ip, string Reason, string Source, string Expires);

    public static List<IpBanDto> Get(MinecraftApi minecraftApi)
        => minecraftApi.BanListService.GetIpBanEntries().Select(IpBanDto.From).ToList();

    public static List<IpBanDto> Add(MinecraftApi minecraftApi, List<IncomingIpBanDto> bans, ClientInfo clientInfo)
    {
        foreach (var ban in bans)
        {
            foreach (var player in BanIp(minecraftApi, ban, clientInfo))
                player.Disconnect(Component.Translatable("multiplayer.disconnect.ip_banned"));
        }
        return Get(minecraftApi);
    }

    //BanIp applies one incoming ban and returns the players that were connected from that address
    public static List<NacreCraft.Game.Server.ServerPlayer> BanIp(MinecraftApi minecraftApi, IncomingIpBanDto ban,
        ClientInfo clientInfo)
    {
        var ip = ResolveIp(minecraftApi, ban);
        if (ip is null) return new List<NacreCraft.Game.Server.ServerPlayer>();
        var value = new IpBan(ip, ban.Reason.OrElse("Banned by an operator."), ban.Source.OrElse(BanSource),
            ban.Expires.OrElse("forever"));
        minecraftApi.BanListService.AddIpBan(ToEntry(value), clientInfo);
        return minecraftApi.PlayerListService.GetPlayersWithAddress(value.Ip).ToList();
    }

    public static List<IpBanDto> Clear(MinecraftApi minecraftApi, ClientInfo clientInfo)
    {
        minecraftApi.BanListService.ClearIpBans(clientInfo);
        return Get(minecraftApi);
    }

    public static List<IpBanDto> Remove(MinecraftApi minecraftApi, List<string> bans, ClientInfo clientInfo)
    {
        foreach (var ip in bans) minecraftApi.BanListService.RemoveIpBan(ip, clientInfo);
        return Get(minecraftApi);
    }

    public static List<IpBanDto> Set(MinecraftApi minecraftApi, List<IpBanDto> ips, ClientInfo clientInfo)
    {
        var finalBans = ips.Where(ban => IPAddress.TryParse(ban.Ip, out _))
            .Select(ban => new IpBan(ban.Ip, ban.Reason.OrElse("Banned by an operator."),
                ban.Source.OrElse(BanSource), ban.Expires.OrElse("forever")))
            .ToHashSet();
        var currentBans = minecraftApi.BanListService.GetIpBanEntries()
            .Select(entry => new IpBan(entry.Ip, entry.Reason, entry.Source, entry.Expires))
            .ToHashSet();
        foreach (var ban in currentBans.Where(ban => !finalBans.Contains(ban)))
            minecraftApi.BanListService.RemoveIpBan(ban.Ip, clientInfo);
        foreach (var ban in finalBans.Where(ban => !currentBans.Contains(ban)))
            minecraftApi.BanListService.AddIpBan(ToEntry(ban), clientInfo);
        return Get(minecraftApi);
    }

    private static string? ResolveIp(MinecraftApi minecraftApi, IncomingIpBanDto ban)
    {
        if (ban.Ip.IsPresent) return ban.Ip.Get();
        if (!ban.Player.IsPresent) return null;
        var dto = ban.Player.Get();
        var player = minecraftApi.PlayerListService.GetPlayer(dto.Id, dto.Name);
        return player.IsPresent ? player.Get().Connection.RemoteAddress : null;
    }

    private static IpBanListEntry ToEntry(IpBan ban) => new(ban.Ip, ban.Reason, ban.Source, ban.Expires);
}
