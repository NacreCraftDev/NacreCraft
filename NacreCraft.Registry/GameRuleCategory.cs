namespace NacreCraft.Registry;

//GameRuleCategory the group a game rule belongs to, maps to vanilla net.minecraft.world.level.gamerules.GameRuleCategory
//Registration order is the display sort order; the category itself holds no values
public sealed record GameRuleCategory(Identifier Id)
{
    private static readonly List<GameRuleCategory> SortOrder = new();

    public static readonly GameRuleCategory Player = Register("player");
    public static readonly GameRuleCategory Mobs = Register("mobs");
    public static readonly GameRuleCategory Spawning = Register("spawning");
    public static readonly GameRuleCategory Drops = Register("drops");
    public static readonly GameRuleCategory Updates = Register("updates");
    public static readonly GameRuleCategory Chat = Register("chat");
    public static readonly GameRuleCategory Misc = Register("misc");

    //DescriptionId the category identifier used as the translation key stem, maps to vanilla getDescriptionId
    public Identifier DescriptionId => Id;

    //LabelKey the translation key of the category label, maps to the component vanilla builds in label()
    //The registry layer cannot name components, so the key is exposed and the caller decides how to wrap it
    public string LabelKey => Id.ToLanguageKey("gamerule.category");

    //Registered every category in display order
    public static IReadOnlyList<GameRuleCategory> Registered => SortOrder;

    public static GameRuleCategory Register(string name) => Register(Identifier.WithDefaultNamespace(name));

    //Register adds a category, maps to vanilla register; a duplicate id is a programming error and throws
    public static GameRuleCategory Register(Identifier id)
    {
        var category = new GameRuleCategory(id);
        if (SortOrder.Contains(category)) throw new ArgumentException($"Category '{id}' is already registered.", nameof(id));
        SortOrder.Add(category);
        return category;
    }
}
