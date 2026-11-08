using NacreCraft.Game.Server.JsonRpc.Methods;
using NacreCraft.Game.Server.Notifications;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftGameRuleService gamerule reads and writes behind the RPC, maps to vanilla MinecraftGameRuleService
public interface MinecraftGameRuleService
{
    GameRulesService.GameRuleUpdate UpdateGameRule(GameRulesService.GameRuleUpdate update, ClientInfo clientInfo);

    object GetRuleValue(GameRule<object> gameRule);

    GameRulesService.GameRuleUpdate GetTypedRule(GameRule<object> gameRule, object value);

    IEnumerable<GameRule<object>> GetAvailableGameRules();
}

//MinecraftGameRuleServiceImpl gamerule service over the server's GameRuleMap
//The map stores boxed bool/int values, so a rule read branches on the rule type
public sealed class MinecraftGameRuleServiceImpl(NotificationManager notificationManager, JsonRpcLogger jsonRpcLogger)
    : MinecraftGameRuleService
{
    private NacreCraft.Game.Server.MinecraftServer Server()
        => notificationManager.Server() ?? throw new InvalidOperationException("Server is not initialized");

    public GameRulesService.GameRuleUpdate UpdateGameRule(GameRulesService.GameRuleUpdate update,
        ClientInfo clientInfo)
    {
        var server = Server();
        var rules = server.GameRules ?? throw new InvalidOperationException("Game rules are not available");
        var oldValue = GetRuleValue(update.GameRule);
        rules.SetRule(update.GameRule, update.Value);
        jsonRpcLogger.Log(clientInfo, "Game rule '{}' updated from '{}' to '{}'", update.GameRule.Id.ToShortString(),
            Format(update.GameRule, oldValue), Format(update.GameRule, update.Value));
        return update;
    }

    public object GetRuleValue(GameRule<object> gameRule)
    {
        var rules = Server().GameRules ?? throw new InvalidOperationException("Game rules are not available");
        return gameRule.Type == GameRuleType.Bool ? rules.GetBool(gameRule) : rules.GetInt(gameRule);
    }

    public GameRulesService.GameRuleUpdate GetTypedRule(GameRule<object> gameRule, object value)
        => new(gameRule, value);

    public IEnumerable<GameRule<object>> GetAvailableGameRules()
        => BuiltInRegistries.GAME_RULE.EntrySet.Select(entry => entry.Value);

    private static object Format(GameRule<object> rule, object value)
        => rule.Type == GameRuleType.Bool ? value : value;
}
