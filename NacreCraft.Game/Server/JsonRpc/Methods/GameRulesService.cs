using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.JsonRpc.Methods;

//GameRulesService game rule RPC operations, maps to vanilla net.minecraft.server.jsonrpc.methods.GameRulesService
//NC's GameRule is always boxed as object, so GameRuleUpdate carries the object value and the type comes from the rule definition
public static class GameRulesService
{
    //GameRuleUpdate one rule plus its value, maps to vanilla GameRulesService.GameRuleUpdate
    public sealed record GameRuleUpdate(GameRule<object> GameRule, object Value)
    {
        //GetValueAndTypeCodec the wire form with an explicit type field is described by the rule metadata; the schema is built by the
        //method registry from the rule type, so only the data shape is kept here
        public string TypeName => NacreCraft.Registry.GameRule<object>.SerializedName(GameRule.Type);
    }

    public static List<GameRuleUpdate> Get(MinecraftApi minecraftApi)
    {
        var rules = new List<GameRuleUpdate>();
        foreach (var rule in minecraftApi.GameRuleService.GetAvailableGameRules())
            rules.Add(GetTypedRule(minecraftApi, rule, minecraftApi.GameRuleService.GetRuleValue(rule)));
        return rules;
    }

    public static GameRuleUpdate GetTypedRule(MinecraftApi minecraftApi, GameRule<object> gameRule, object value)
        => minecraftApi.GameRuleService.GetTypedRule(gameRule, value);

    public static GameRuleUpdate Update(MinecraftApi minecraftApi, GameRuleUpdate update, ClientInfo clientInfo)
        => minecraftApi.GameRuleService.UpdateGameRule(update, clientInfo);
}
