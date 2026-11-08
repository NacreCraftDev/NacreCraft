using System.Text.Json.Nodes;
using NacreCraft.Codec;
using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.JsonRpc.Methods;
using NacreCraft.Game.World.Level;
using NacreCraft.Network.Protocol.Status;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc;

//SchemaCodecs JSON codecs used by the schema presets that have no codec of their own
//The RPC surface is JSON only, so these codecs read and write JsonOps objects directly
public sealed class DifficultyCodec : ScalarCodec<Difficulty>
{
    public override DataResult<Difficulty> Parse<U>(DynamicOps<U> ops, U input)
    {
        if (ops is not JsonOps || input is not JsonValue value || !value.TryGetValue<string>(out var name))
            return DataResult<Difficulty>.Error(() => "difficulty must be a string");
        var difficulty = Difficulty.ByName(name);
        return difficulty is null
            ? DataResult<Difficulty>.Error(() => $"unknown difficulty: {name}")
            : DataResult<Difficulty>.Success(difficulty);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, Difficulty value)
        => ops is JsonOps
            ? (DataResult<U>)(object)DataResult<JsonNode?>.Success(JsonValue.Create(value.Name))
            : base.EncodeStart(ops, value);
}

//GameRuleTypeCodec serializes the rule type by its serialized name
public sealed class GameRuleTypeCodec : ScalarCodec<GameRuleType>
{
    public override DataResult<GameRuleType> Parse<U>(DynamicOps<U> ops, U input)
    {
        if (ops is not JsonOps || input is not JsonValue value || !value.TryGetValue<string>(out var name))
            return DataResult<GameRuleType>.Error(() => "rule type must be a string");
        return name switch
        {
            "boolean" => DataResult<GameRuleType>.Success(GameRuleType.Bool),
            "integer" => DataResult<GameRuleType>.Success(GameRuleType.Int),
            _ => DataResult<GameRuleType>.Error(() => $"unknown rule type: {name}"),
        };
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, GameRuleType value)
        => ops is JsonOps
            ? (DataResult<U>)(object)DataResult<JsonNode?>.Success(JsonValue.Create(GameRule<object>.SerializedName(value)))
            : base.EncodeStart(ops, value);
}

//GameRuleUpdateCodec the {key, value} pair used by the gamerule RPC methods, maps to vanilla GameRuleUpdate.CODEC
//The value width comes from the rule definition, so a bool rule reads a JSON boolean and an integer rule a JSON number
public sealed class GameRuleUpdateCodec : ScalarCodec<GameRulesService.GameRuleUpdate>
{
    public override DataResult<GameRulesService.GameRuleUpdate> Parse<U>(DynamicOps<U> ops, U input)
    {
        if (ops is not JsonOps || input is not JsonObject obj)
            return DataResult<GameRulesService.GameRuleUpdate>.Error(() => "gamerule update must be an object");
        if (!obj.TryGetPropertyValue("key", out var keyNode) || keyNode is not JsonValue keyValue
            || !keyValue.TryGetValue<string>(out var key))
            return DataResult<GameRulesService.GameRuleUpdate>.Error(() => "gamerule update is missing the key");
        var rule = BuiltInRegistries.GAME_RULE.GetValue(Identifier.Parse(key));
        if (rule is null)
            return DataResult<GameRulesService.GameRuleUpdate>.Error(() => $"unknown gamerule: {key}");
        var valueNode = obj["value"];
        if (valueNode is not JsonValue value)
            return DataResult<GameRulesService.GameRuleUpdate>.Error(() => "gamerule update is missing the value");
        object boxed;
        if (rule.Type == GameRuleType.Bool)
        {
            if (!value.TryGetValue<bool>(out var boolValue))
                return DataResult<GameRulesService.GameRuleUpdate>.Error(() => $"{key} expects a boolean");
            boxed = boolValue;
        }
        else
        {
            if (!value.TryGetValue<int>(out var intValue))
                return DataResult<GameRulesService.GameRuleUpdate>.Error(() => $"{key} expects an integer");
            boxed = intValue;
        }
        return DataResult<GameRulesService.GameRuleUpdate>.Success(
            new GameRulesService.GameRuleUpdate(rule, boxed));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, GameRulesService.GameRuleUpdate value)
    {
        if (ops is not JsonOps) return base.EncodeStart(ops, value);
        var obj = new JsonObject
        {
            ["key"] = value.GameRule.Id.ToString(),
            ["value"] = value.Value is bool flag ? JsonValue.Create(flag) : JsonValue.Create((int)value.Value),
        };
        return (DataResult<U>)(object)DataResult<JsonNode?>.Success(obj);
    }
}

//ServerStateCodec the status payload, maps to vanilla ServerStateService.ServerState.CODEC
public sealed class ServerStateCodec : ScalarCodec<ServerStateService.ServerState>
{
    public override DataResult<ServerStateService.ServerState> Parse<U>(DynamicOps<U> ops, U input)
    {
        if (ops is not JsonOps || input is not JsonObject obj)
            return DataResult<ServerStateService.ServerState>.Error(() => "server state must be an object");
        var started = obj["started"] is JsonValue startedNode && startedNode.TryGetValue<bool>(out var startedValue)
                      && startedValue;
        var players = new List<PlayerDto>();
        if (obj["players"] is JsonArray array)
        {
            foreach (var entry in array)
            {
                var parsed = Codecs.AsCodec(PlayerDto.CODEC).Parse(JsonOps.Instance, entry);
                if (!parsed.Result().IsPresent)
                    return DataResult<ServerStateService.ServerState>.Error(() => "invalid player entry");
                players.Add(parsed.GetOrThrow());
            }
        }
        var version = ServerStatus.VersionData.Current();
        if (obj["version"] is JsonObject versionObj)
        {
            var name = versionObj["name"] is JsonValue nameNode && nameNode.TryGetValue<string>(out var nameText)
                ? nameText : version.Name;
            var protocol = versionObj["protocol"] is JsonValue protocolNode
                           && protocolNode.TryGetValue<int>(out var protocolValue) ? protocolValue : version.Protocol;
            version = new ServerStatus.VersionData { Name = name, Protocol = protocol };
        }
        return DataResult<ServerStateService.ServerState>.Success(
            new ServerStateService.ServerState(started, players, version));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, ServerStateService.ServerState value)
    {
        if (ops is not JsonOps) return base.EncodeStart(ops, value);
        var players = new JsonArray();
        foreach (var player in value.Players)
            players.Add(Codecs.AsCodec(PlayerDto.CODEC).EncodeStart(JsonOps.Instance, player).GetOrThrow());
        var obj = new JsonObject
        {
            ["started"] = value.Started,
            ["players"] = players,
            ["version"] = new JsonObject { ["name"] = value.Version.Name, ["protocol"] = value.Version.Protocol },
        };
        return (DataResult<U>)(object)DataResult<JsonNode?>.Success(obj);
    }
}
