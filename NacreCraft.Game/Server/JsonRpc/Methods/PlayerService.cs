using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Server.JsonRpc.Methods;

//PlayerService online player RPC operations, maps to vanilla net.minecraft.server.jsonrpc.methods.PlayerService
public static class PlayerService
{
    private static readonly Component DefaultKickMessage = Component.Translatable("multiplayer.disconnect.kicked");

    public static List<PlayerDto> Get(MinecraftApi minecraftApi)
        => minecraftApi.PlayerListService.GetPlayers().Select(PlayerDto.From).ToList();

    //KickDto the kick request payload, maps to vanilla PlayerService.KickDto
    public sealed record KickDto(PlayerDto Player, Optional<Message> Message)
    {
        public static readonly MapCodec<KickDto> CODEC = RecordCodecBuilder.Of2(
            Codecs.AsCodec(PlayerDto.CODEC).FieldOf("player").ForGetter((KickDto dto) => dto.Player),
            Codecs.AsCodec(NacreCraft.Game.Server.JsonRpc.Message.CODEC).OptionalFieldOf("message").ForGetter((KickDto dto) => dto.Message),
            (player, message) => new KickDto(player, message));
    }

    public static List<PlayerDto> Kick(MinecraftApi minecraftApi, List<KickDto> kicks, ClientInfo clientInfo)
    {
        var kicked = new List<PlayerDto>();
        foreach (var kick in kicks)
        {
            var player = GetServerPlayer(minecraftApi, kick.Player);
            if (player is null) continue;
            minecraftApi.PlayerListService.Remove(player, clientInfo);
            var message = kick.Message.IsPresent
                ? kick.Message.Get().AsComponent().OrElse(DefaultKickMessage)
                : DefaultKickMessage;
            player.Disconnect(message);
            kicked.Add(kick.Player);
        }
        return kicked;
    }

    private static NacreCraft.Game.Server.ServerPlayer? GetServerPlayer(MinecraftApi minecraftApi, PlayerDto playerDto)
    {
        if (playerDto.Id.IsPresent) return minecraftApi.PlayerListService.GetPlayer(playerDto.Id.Get());
        if (playerDto.Name.IsPresent) return minecraftApi.PlayerListService.GetPlayerByName(playerDto.Name.Get());
        return null;
    }
}
