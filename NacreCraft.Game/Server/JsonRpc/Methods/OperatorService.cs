using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.Players;
using NacreCraft.Network.Protocol.Status;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc.Methods;

//OperatorService operator list RPC operations, maps to vanilla net.minecraft.server.jsonrpc.methods.OperatorService
public static class OperatorService
{
    //OperatorDto one operator payload, maps to vanilla OperatorService.OperatorDto
    public sealed record OperatorDto(PlayerDto Player, Optional<PermissionLevel> PermissionLevel,
        Optional<bool> BypassesPlayerLimit)
    {
        public static readonly MapCodec<OperatorDto> CODEC = RecordCodecBuilder.Of3(
            Codecs.AsCodec(PlayerDto.CODEC).FieldOf("player").ForGetter((OperatorDto dto) => dto.Player),
            PermissionLevels.IntCodec.OptionalFieldOf("permissionLevel")
                .ForGetter((OperatorDto dto) => dto.PermissionLevel),
            Codecs.Bool.OptionalFieldOf("bypassesPlayerLimit").ForGetter((OperatorDto dto) => dto.BypassesPlayerLimit),
            (player, permissionLevel, bypassesPlayerLimit)
                => new OperatorDto(player, permissionLevel, bypassesPlayerLimit));

        public static OperatorDto From(ServerOpListEntry entry)
            => new(PlayerDto.From(entry.GetUser()), Optional<PermissionLevel>.Of(entry.Permissions),
                Optional<bool>.Of(entry.BypassesPlayerLimit));
    }

    private sealed record Op(NameAndId User, Optional<PermissionLevel> PermissionLevel,
        Optional<bool> BypassesPlayerLimit);

    public static List<OperatorDto> Get(MinecraftApi minecraftApi)
        => minecraftApi.OperatorListService.GetEntries()
            .Where(entry => entry.GetUser() is not null)
            .Select(OperatorDto.From)
            .ToList();

    public static List<OperatorDto> Clear(MinecraftApi minecraftApi, ClientInfo clientInfo)
    {
        minecraftApi.OperatorListService.Clear(clientInfo);
        return Get(minecraftApi);
    }

    public static async Task<List<OperatorDto>> Remove(MinecraftApi minecraftApi, List<PlayerDto> players,
        ClientInfo clientInfo)
    {
        var fetch = players.Select(player => minecraftApi.PlayerListService.GetUser(player.Id, player.Name)).ToList();
        foreach (var user in await Task.WhenAll(fetch))
        {
            if (user.IsPresent) minecraftApi.OperatorListService.Deop(user.Get(), clientInfo);
        }
        return Get(minecraftApi);
    }

    public static async Task<List<OperatorDto>> Add(MinecraftApi minecraftApi, List<OperatorDto> operators,
        ClientInfo clientInfo)
    {
        var fetch = operators.Select(async dto =>
        {
            var user = await minecraftApi.PlayerListService.GetUser(dto.Player.Id, dto.Player.Name);
            return user.IsPresent
                ? Optional<Op>.Of(new Op(user.Get(), dto.PermissionLevel, dto.BypassesPlayerLimit))
                : Optional<Op>.Empty();
        }).ToList();
        foreach (var op in await Task.WhenAll(fetch))
        {
            if (!op.IsPresent) continue;
            var value = op.Get();
            minecraftApi.OperatorListService.Op(value.User, value.PermissionLevel, value.BypassesPlayerLimit,
                clientInfo);
        }
        return Get(minecraftApi);
    }

    public static async Task<List<OperatorDto>> Set(MinecraftApi minecraftApi, List<OperatorDto> operators,
        ClientInfo clientInfo)
    {
        var fetch = operators.Select(async dto =>
        {
            var user = await minecraftApi.PlayerListService.GetUser(dto.Player.Id, dto.Player.Name);
            return user.IsPresent
                ? Optional<Op>.Of(new Op(user.Get(), dto.PermissionLevel, dto.BypassesPlayerLimit))
                : Optional<Op>.Empty();
        }).ToList();
        var finalOperators = new HashSet<Op>();
        foreach (var op in await Task.WhenAll(fetch))
        {
            if (op.IsPresent) finalOperators.Add(op.Get());
        }
        var currentOperators = minecraftApi.OperatorListService.GetEntries()
            .Where(entry => entry.GetUser() is not null)
            .Select(entry => new Op(entry.GetUser(), Optional<PermissionLevel>.Of(entry.Permissions),
                Optional<bool>.Of(entry.BypassesPlayerLimit)))
            .ToHashSet();
        foreach (var op in currentOperators.Where(op => !finalOperators.Contains(op)))
            minecraftApi.OperatorListService.Deop(op.User, clientInfo);
        foreach (var op in finalOperators.Where(op => !currentOperators.Contains(op)))
            minecraftApi.OperatorListService.Op(op.User, op.PermissionLevel, op.BypassesPlayerLimit, clientInfo);
        return Get(minecraftApi);
    }
}
