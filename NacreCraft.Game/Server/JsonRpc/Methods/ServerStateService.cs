using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Network.Protocol.Status;

namespace NacreCraft.Game.Server.JsonRpc.Methods;

//ServerStateService server state RPC methods, maps to vanilla net.minecraft.server.jsonrpc.methods.ServerStateService
public static class ServerStateService
{
    //ServerState one server status payload, maps to vanilla ServerStateService.ServerState
    public sealed record ServerState(bool Started, IReadOnlyList<PlayerDto> Players, ServerStatus.VersionData Version)
    {
        public static readonly ServerState NotStarted = new(false, Array.Empty<PlayerDto>(), ServerStatus.VersionData.Current());
    }

    public static ServerState Status(MinecraftApi minecraftApi)
        => minecraftApi.ServerStateService.IsReady()
            ? new ServerState(true, PlayerService.Get(minecraftApi), ServerStatus.VersionData.Current())
            : ServerState.NotStarted;

    public static bool Save(MinecraftApi minecraftApi, bool flush, ClientInfo clientInfo)
        => minecraftApi.ServerStateService.SaveEverything(true, flush, true, clientInfo);

    public static bool Stop(MinecraftApi minecraftApi, ClientInfo clientInfo)
    {
        _ = minecraftApi.Submit(() => minecraftApi.ServerStateService.Halt(false, clientInfo));
        return true;
    }

    //SystemMessage one system message payload, maps to vanilla ServerStateService.SystemMessage
    public sealed record SystemMessage(Message Message, bool Overlay, Optional<IReadOnlyList<PlayerDto>> ReceivingPlayers)
    {
        public static readonly Codec<SystemMessage> CODEC = RecordCodecBuilder.Of3(
            Codecs.AsCodec(NacreCraft.Game.Server.JsonRpc.Message.CODEC).FieldOf("message")
                .ForGetter((SystemMessage message) => message.Message),
            Codecs.Bool.FieldOf("overlay").ForGetter((SystemMessage message) => message.Overlay),
            Codecs.AsCodec(PlayerDto.CODEC).ListOf().LenientOptionalFieldOf("receivingPlayers")
                .ForGetter((SystemMessage message) => message.ReceivingPlayers),
            (message, overlay, receivingPlayers) => new SystemMessage(message, overlay, receivingPlayers));
    }

    public static bool SendSystemMessage(MinecraftApi minecraftApi, SystemMessage systemMessage, ClientInfo clientInfo)
    {
        var optionalComponent = systemMessage.Message.AsComponent();
        if (!optionalComponent.IsPresent) return false;
        var component = optionalComponent.Get();
        if (systemMessage.ReceivingPlayers.IsPresent)
        {
            var targets = systemMessage.ReceivingPlayers.Get();
            if (targets.Count == 0) return false;
            var players = new List<NacreCraft.Game.Server.ServerPlayer>();
            foreach (var dto in targets)
            {
                NacreCraft.Game.Server.ServerPlayer? player = null;
                if (dto.Id.IsPresent) player = minecraftApi.PlayerListService.GetPlayer(dto.Id.Get());
                else if (dto.Name.IsPresent) player = minecraftApi.PlayerListService.GetPlayerByName(dto.Name.Get());
                if (player is not null) players.Add(player);
            }
            minecraftApi.ServerStateService.SendSystemMessage(component, systemMessage.Overlay, players, clientInfo);
            return true;
        }
        minecraftApi.ServerStateService.BroadcastSystemMessage(component, systemMessage.Overlay, clientInfo);
        return true;
    }
}
