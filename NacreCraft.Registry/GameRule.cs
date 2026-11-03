namespace NacreCraft.Registry;

//GameRuleType rule value type, maps to vanilla GameRuleType
//SerializedName is what the save file and the network use, matching the vanilla string form
public enum GameRuleType
{
    Int,
    Bool,
}

//GameRule game rule definition, maps to vanilla net.minecraft.world.level.gamerules.GameRule
//Values are stored by name in GameRuleMap; this class only carries the metadata needed by commands and runtime
//T is fixed to object so all rules share the single BuiltInRegistries.GAME_RULE table
public sealed class GameRule<T>
{
    public GameRule(GameRuleCategory category, Identifier id, GameRuleType type, T defaultValue,
        int min = 0, int max = int.MaxValue)
    {
        Category = category;
        Id = id;
        Type = type;
        DefaultValue = defaultValue;
        Min = min;
        Max = max;
    }

    //Category the group this rule is listed under, maps to vanilla category()
    public GameRuleCategory Category { get; }

    //Id rule identifier, in short-name form minecraft:max_block_modifications
    public Identifier Id { get; }

    public GameRuleType Type { get; }

    //DefaultValue taken when unset; boolean rules box a bool and integer rules box an int
    public T DefaultValue { get; }

    //Min/Max apply only to integer rules; maps to vanilla IntegerArgumentType.integer(min, max)
    public int Min { get; }

    public int Max { get; }

    //SerializedName the type name used on the wire and in the save file, maps to vanilla GameRuleType.getSerializedName
    public static string SerializedName(GameRuleType type) => type == GameRuleType.Bool ? "boolean" : "integer";

    //CallVisitor dispatches to the matching visitor method, maps to vanilla callVisitor
    public void CallVisitor(GameRuleTypeVisitor visitor)
    {
        visitor.Visit(this);
        if (Type == GameRuleType.Bool) visitor.VisitBoolean(this);
        else visitor.VisitInteger(this);
    }

    public override string ToString() => Id.ToShortString();
}
