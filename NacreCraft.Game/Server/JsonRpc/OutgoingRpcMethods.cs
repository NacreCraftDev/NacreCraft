using NacreCraft.Game.Server.JsonRpc.Methods;
using NacreCraft.Registry;
using RegistryMethod = NacreCraft.Registry.OutgoingRpcMethod<object, object>;

namespace NacreCraft.Game.Server.JsonRpc;

//OutgoingRpcMethods the server-to-client notification table, maps to vanilla net.minecraft.server.jsonrpc.OutgoingRpcMethods
//Every notification is a registered outgoing method; the notification service fills them with payloads
public static class OutgoingRpcMethods
{
    //An explicit static constructor removes beforefieldinit so calling Bootstrap runs the field initializers
    static OutgoingRpcMethods()
    {
    }

    public static readonly Reference<RegistryMethod> SERVER_STARTED = Register("server/started",
        OutgoingRpcMethodBuilder<object, object?>.Notification().Description("Server started"));
    public static readonly Reference<RegistryMethod> SERVER_SHUTTING_DOWN = Register("server/stopping",
        OutgoingRpcMethodBuilder<object, object?>.Notification().Description("Server shutting down"));
    public static readonly Reference<RegistryMethod> SERVER_SAVE_STARTED = Register("server/saving",
        OutgoingRpcMethodBuilder<object, object?>.Notification().Description("Server save started"));
    public static readonly Reference<RegistryMethod> SERVER_SAVE_COMPLETED = Register("server/saved",
        OutgoingRpcMethodBuilder<object, object?>.Notification().Description("Server save completed"));
    public static readonly Reference<RegistryMethod> SERVER_ACTIVITY_OCCURRED = Register("server/activity",
        OutgoingRpcMethodBuilder<object, object?>.Notification()
            .Description("Server activity occurred. Rate limited to 1 notification per 30 seconds"));

    public static readonly Reference<RegistryMethod> PLAYER_JOINED = Register("players/joined",
        OutgoingRpcMethodBuilder<PlayerDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.PLAYER_SCHEMA.AsRef()).Description("Player joined"));
    public static readonly Reference<RegistryMethod> PLAYER_LEFT = Register("players/left",
        OutgoingRpcMethodBuilder<PlayerDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.PLAYER_SCHEMA.AsRef()).Description("Player left"));
    public static readonly Reference<RegistryMethod> PLAYER_OPED = Register("operators/added",
        OutgoingRpcMethodBuilder<OperatorService.OperatorDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.OPERATOR_SCHEMA.AsRef()).Description("Player was oped"));
    public static readonly Reference<RegistryMethod> PLAYER_DEOPED = Register("operators/removed",
        OutgoingRpcMethodBuilder<OperatorService.OperatorDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.OPERATOR_SCHEMA.AsRef()).Description("Player was deoped"));
    public static readonly Reference<RegistryMethod> PLAYER_ADDED_TO_ALLOWLIST = Register("allowlist/added",
        OutgoingRpcMethodBuilder<PlayerDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.PLAYER_SCHEMA.AsRef()).Description("Player was added to allowlist"));
    public static readonly Reference<RegistryMethod> PLAYER_REMOVED_FROM_ALLOWLIST = Register("allowlist/removed",
        OutgoingRpcMethodBuilder<PlayerDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.PLAYER_SCHEMA.AsRef()).Description("Player was removed from allowlist"));
    public static readonly Reference<RegistryMethod> IP_BANNED = Register("ip_bans/added",
        OutgoingRpcMethodBuilder<IpBanlistService.IpBanDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.IP_BAN_SCHEMA.AsRef()).Description("Ip was added to ip ban list"));
    public static readonly Reference<RegistryMethod> IP_UNBANNED = Register("ip_bans/removed",
        OutgoingRpcMethodBuilder<string, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.STRING_SCHEMA).Description("Ip was removed from ip ban list"));
    public static readonly Reference<RegistryMethod> PLAYER_BANNED = Register("bans/added",
        OutgoingRpcMethodBuilder<BanlistService.UserBanDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.PLAYER_BAN_SCHEMA.AsRef()).Description("Player was added to ban list"));
    public static readonly Reference<RegistryMethod> PLAYER_UNBANNED = Register("bans/removed",
        OutgoingRpcMethodBuilder<PlayerDto, object?>.NotificationWithParams()
            .Param("player", SchemaPresets.PLAYER_SCHEMA.AsRef()).Description("Player was removed from ban list"));
    public static readonly Reference<RegistryMethod> GAMERULE_CHANGED = Register("gamerules/updated",
        OutgoingRpcMethodBuilder<GameRulesService.GameRuleUpdate, object?>.NotificationWithParams()
            .Param("gamerule", SchemaPresets.TYPED_GAME_RULE_SCHEMA.AsRef()).Description("Gamerule was changed"));
    public static readonly Reference<RegistryMethod> STATUS_HEARTBEAT = Register("server/status",
        OutgoingRpcMethodBuilder<ServerStateService.ServerState, object?>.NotificationWithParams()
            .AllowPreServerInit().Param("status", SchemaPresets.SERVER_STATE_SCHEMA.AsRef())
            .Description("Server status heartbeat, including before the server has spun up"));

    //Bootstrap forces the field initializers so every notification is registered before the management server starts
    public static void Bootstrap()
    {
        SchemaPresets.EnsureInitialized();
    }

    private static Reference<RegistryMethod> Register<Params>(string key,
        OutgoingRpcMethodBuilder<Params, object?> builder)
        => builder.RegisterForHolder(BuiltInRegistries.OUTGOING_RPC_METHOD, key);
}
