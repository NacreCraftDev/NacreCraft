using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.Methods;
using NacreCraft.Game.World.Level;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc;

//SchemaPresets the built-in OpenRPC schema components, maps to the static schema fields of vanilla Schema
//Only the components whose value types are already ported are registered; touching this type initializes them once
public static class SchemaPresets
{
    //An explicit static constructor removes beforefieldinit, so EnsureInitialized really runs the field initializers
    static SchemaPresets()
    {
    }

    private static readonly Codec<Guid> UuidCodec = Codecs.String.ComapFlatMap(
        text => Guid.TryParse(text, out var id)
            ? DataResult<Guid>.Success(id)
            : DataResult<Guid>.Error(() => $"Invalid UUID: {text}"),
        id => id.ToString());

    public static readonly Schema<bool> BOOL_SCHEMA = Schema<bool>.OfType("boolean", Codecs.Bool);
    public static readonly Schema<int> INT_SCHEMA = Schema<int>.OfType("integer", Codecs.Int);
    public static readonly Schema<float> NUMBER_SCHEMA = Schema<float>.OfType("number", Codecs.Float);
    public static readonly Schema<string> STRING_SCHEMA = Schema<string>.OfType("string", Codecs.String);
    public static readonly Schema<Guid> UUID_SCHEMA = Schema<Guid>.OfType("string", UuidCodec);

    public static readonly TypedSchemaComponent<PlayerDto> PLAYER_SCHEMA = Schema<PlayerDto>.RegisterTypedSchema("player",
        Schema<PlayerDto>.Record(Codecs.AsCodec(PlayerDto.CODEC))
            .WithField("id", UUID_SCHEMA)
            .WithField("name", STRING_SCHEMA));

    public static readonly TypedSchemaComponent<DiscoveryService.DiscoverInfo> VERSION_SCHEMA = Schema<DiscoveryService.DiscoverInfo>.RegisterTypedSchema("version",
        Schema<DiscoveryService.DiscoverInfo>.Record(DiscoveryService.DiscoverInfo.Codec)
            .WithField("name", STRING_SCHEMA)
            .WithField("protocol", INT_SCHEMA));

    public static readonly TypedSchemaComponent<Message> MESSAGE_SCHEMA = Schema<Message>.RegisterTypedSchema("message",
        Schema<Message>.Record(Message.CODEC)
            .WithField("literal", STRING_SCHEMA)
            .WithField("translatable", STRING_SCHEMA)
            .WithField("translatableParams", STRING_SCHEMA.AsArray()));

    //--- schemas used by the RPC method registries ---

    public static readonly TypedSchemaComponent<OperatorService.OperatorDto> OPERATOR_SCHEMA =
        Schema<OperatorService.OperatorDto>.RegisterTypedSchema("operator",
            Schema<OperatorService.OperatorDto>.Record(Codecs.AsCodec(OperatorService.OperatorDto.CODEC))
                .WithField("player", PLAYER_SCHEMA.AsRef())
                .WithField("bypassesPlayerLimit", BOOL_SCHEMA)
                .WithField("permissionLevel", INT_SCHEMA));

    public static readonly TypedSchemaComponent<IpBanlistService.IncomingIpBanDto> INCOMING_IP_BAN_SCHEMA =
        Schema<IpBanlistService.IncomingIpBanDto>.RegisterTypedSchema("incoming_ip_ban",
            Schema<IpBanlistService.IncomingIpBanDto>.Record(
                    Codecs.AsCodec(IpBanlistService.IncomingIpBanDto.CODEC))
                .WithField("player", PLAYER_SCHEMA.AsRef())
                .WithField("ip", STRING_SCHEMA)
                .WithField("reason", STRING_SCHEMA)
                .WithField("source", STRING_SCHEMA)
                .WithField("expires", STRING_SCHEMA));

    public static readonly TypedSchemaComponent<IpBanlistService.IpBanDto> IP_BAN_SCHEMA =
        Schema<IpBanlistService.IpBanDto>.RegisterTypedSchema("ip_ban",
            Schema<IpBanlistService.IpBanDto>.Record(Codecs.AsCodec(IpBanlistService.IpBanDto.CODEC))
                .WithField("ip", STRING_SCHEMA)
                .WithField("reason", STRING_SCHEMA)
                .WithField("source", STRING_SCHEMA)
                .WithField("expires", STRING_SCHEMA));

    public static readonly TypedSchemaComponent<BanlistService.UserBanDto> PLAYER_BAN_SCHEMA =
        Schema<BanlistService.UserBanDto>.RegisterTypedSchema("user_ban",
            Schema<BanlistService.UserBanDto>.Record(Codecs.AsCodec(BanlistService.UserBanDto.CODEC))
                .WithField("player", PLAYER_SCHEMA.AsRef())
                .WithField("reason", STRING_SCHEMA)
                .WithField("source", STRING_SCHEMA)
                .WithField("expires", STRING_SCHEMA));

    public static readonly TypedSchemaComponent<ServerStateService.ServerState> SERVER_STATE_SCHEMA =
        Schema<ServerStateService.ServerState>.RegisterTypedSchema("server_state",
            Schema<ServerStateService.ServerState>.Record(new ServerStateCodec())
                .WithField("started", BOOL_SCHEMA)
                .WithField("players", PLAYER_SCHEMA.AsArray())
                .WithField("version", VERSION_SCHEMA.AsRef()));

    public static readonly TypedSchemaComponent<ServerStateService.SystemMessage> SYSTEM_MESSAGE_SCHEMA =
        Schema<ServerStateService.SystemMessage>.RegisterTypedSchema("system_message",
            Schema<ServerStateService.SystemMessage>.Record(ServerStateService.SystemMessage.CODEC)
                .WithField("message", MESSAGE_SCHEMA.AsRef())
                .WithField("overlay", BOOL_SCHEMA)
                .WithField("receivingPlayers", PLAYER_SCHEMA.AsArray()));

    public static readonly TypedSchemaComponent<PlayerService.KickDto> KICK_PLAYER_SCHEMA =
        Schema<PlayerService.KickDto>.RegisterTypedSchema("kick_player",
            Schema<PlayerService.KickDto>.Record(Codecs.AsCodec(PlayerService.KickDto.CODEC))
                .WithField("message", MESSAGE_SCHEMA.AsRef())
                .WithField("player", PLAYER_SCHEMA.AsRef()));

    public static readonly TypedSchemaComponent<GameRulesService.GameRuleUpdate> TYPED_GAME_RULE_SCHEMA =
        Schema<GameRulesService.GameRuleUpdate>.RegisterTypedSchema("typed_game_rule",
            Schema<GameRulesService.GameRuleUpdate>.Record(new GameRuleUpdateCodec())
                .WithField("key", STRING_SCHEMA)
                .WithField("value", Schema<int>.OfTypes(new[] { "boolean", "integer" }, Codecs.Int))
                .WithField("type", Schema<GameRuleType>.OfEnum(new[] { "boolean", "integer" }, new GameRuleTypeCodec())));

    public static readonly TypedSchemaComponent<GameRulesService.GameRuleUpdate> UNTYPED_GAME_RULE_SCHEMA =
        Schema<GameRulesService.GameRuleUpdate>.RegisterTypedSchema("untyped_game_rule",
            Schema<GameRulesService.GameRuleUpdate>.Record(new GameRuleUpdateCodec())
                .WithField("key", STRING_SCHEMA)
                .WithField("value", Schema<int>.OfTypes(new[] { "boolean", "integer" }, Codecs.Int)));

    public static readonly TypedSchemaComponent<Difficulty> DIFFICULTY_SCHEMA =
        Schema<Difficulty>.RegisterTypedSchema("difficulty",
            Schema<Difficulty>.OfEnum(new[] { "peaceful", "easy", "normal", "hard" }, new DifficultyCodec()));

    public static readonly TypedSchemaComponent<GameType> GAME_TYPE_SCHEMA =
        Schema<GameType>.RegisterTypedSchema("game_type",
            Schema<GameType>.OfEnum(new[] { "survival", "creative", "adventure", "spectator" }, GameType.Codec));

    public static readonly Schema<PermissionLevel> PERMISSION_LEVEL_SCHEMA =
        Schema<PermissionLevel>.OfType("integer", PermissionLevels.IntCodec);

    public static readonly TypedSchemaComponent<GameRuleType> RULE_TYPE_SCHEMA =
        Schema<GameRuleType>.RegisterTypedSchema("rule_type",
            Schema<GameRuleType>.OfEnum(new[] { "boolean", "integer" }, new GameRuleTypeCodec()));

    //EnsureInitialized forces the static field initializers to run before the discovery document is built
    public static void EnsureInitialized()
    {
    }
}
